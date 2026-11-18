using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class CameraController : MonoBehaviour
{
    [Header("Follow settings")]
    public float followDistance = 12f;
    public float followHeight = 7f;
    public float lookAtHeight = 1f;
    public float positionSmooth = 4f;
    public float rotationSmooth = 5f;

    int selected = -1;                   // -1 = overview, 0/1/2 = V1/V2/V3
    Transform[] cars = new Transform[3];
    VehicleMovement vm;

    Vector3 homePos;
    Quaternion homeRot;

    void Start()
    {
        homePos = transform.position;
        homeRot = transform.rotation;

        vm = FindFirstObjectByType<VehicleMovement>();
        if (vm == null)
        {
            Debug.LogError("CameraController: no VehicleMovement found.");
            return;
        }
        cars[0] = vm.vehicleV1;
        cars[1] = vm.vehicleV2;
        cars[2] = vm.vehicleV3;

        EnsureEventSystem();
        StartCoroutine(HookDashboard());
    }

    void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();

        if (es == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            es = go.GetComponent<EventSystem>();
            Debug.Log("CameraController: created an EventSystem.");
        }

#if ENABLE_INPUT_SYSTEM
        // New Input System: the old StandaloneInputModule cannot receive clicks
        StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Destroy(old);

        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("CameraController: added InputSystemUIInputModule.");
        }
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
            es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    IEnumerator HookDashboard()
    {
        // wait for FleetDashboardAuto to build its UI
        yield return null;
        yield return null;

        GameObject canvasGo = GameObject.Find("DashboardCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("CameraController: DashboardCanvas not found. " +
                           "Is FleetDashboardAuto on the Dashboard object?");
            yield break;
        }

        // THIS was the missing piece: without it the canvas ignores clicks
        if (canvasGo.GetComponent<GraphicRaycaster>() == null)
        {
            canvasGo.AddComponent<GraphicRaycaster>();
            Debug.Log("CameraController: added GraphicRaycaster to the dashboard.");
        }

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            MakeClickable("V" + (i + 1) + "Text", () => Select(index));
        }
        MakeClickable("TitleText", () => Select(-1));

        Debug.Log("CameraController: dashboard cards are clickable.");
    }

    void MakeClickable(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning("CameraController: could not find " + objectName);
            return;
        }

        Graphic g = go.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;

        Button b = go.GetComponent<Button>();
        if (b == null) b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(action);
    }

    public void Select(int index)
    {
        selected = index;
        Debug.Log(index < 0 ? "Camera: overview" : "Camera: following V" + (index + 1));
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return;
        if (k.digit1Key.wasPressedThisFrame) Select(0);
        if (k.digit2Key.wasPressedThisFrame) Select(1);
        if (k.digit3Key.wasPressedThisFrame) Select(2);
        if (k.digit0Key.wasPressedThisFrame) Select(-1);
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);
        if (Input.GetKeyDown(KeyCode.Alpha0)) Select(-1);
#endif
    }

    void LateUpdate()
    {
        Vector3 wantPos;
        Quaternion wantRot;

        if (selected < 0 || cars[selected] == null)
        {
            wantPos = homePos;
            wantRot = homeRot;
        }
        else
        {
            Transform car = cars[selected];

            Vector3 forward =
                Quaternion.Euler(0f, -vm.modelYawOffset, 0f) * car.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            wantPos = car.position - forward * followDistance
                      + Vector3.up * followHeight;

            Vector3 lookTarget = car.position + Vector3.up * lookAtHeight;
            wantRot = Quaternion.LookRotation(lookTarget - wantPos);
        }

        float pk = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        float rk = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, wantPos, pk);
        transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, rk);
    }
}