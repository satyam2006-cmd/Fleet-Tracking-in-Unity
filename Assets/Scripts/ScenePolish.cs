using UnityEngine;
using UnityEngine.Rendering;

public class ScenePolish : MonoBehaviour
{
    [Header("Lighting")]
    public float sunIntensity = 1.3f;
    public Color sunColor = new Color(1f, 0.96f, 0.88f);
    public Vector3 sunAngles = new Vector3(50f, -30f, 0f);
    public Color ambientColor = new Color(0.55f, 0.6f, 0.7f);

    [Header("Vehicle visibility")]
    public float trailTime = 6f;
    public float trailWidth = 0.6f;
    public float ringSize = 3.5f;

    static readonly Color[] colors =
    {
        new Color(1f, 0.85f, 0.2f),
        new Color(0.3f, 0.9f, 1f),
        new Color(1f, 0.4f, 0.9f)
    };

    void Start()
    {
        SetupLighting();
        SetupVehicles();
    }

    void SetupLighting()
    {
        Light sun = null;
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun != null)
        {
            sun.intensity = sunIntensity;
            sun.color = sunColor;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(sunAngles);
        }
        else
        {
            Debug.LogWarning("ScenePolish: no Directional Light found.");
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
    }

    Material MakeMaterial(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    void SetupVehicles()
    {
        VehicleMovement vm = FindFirstObjectByType<VehicleMovement>();
        if (vm == null)
        {
            Debug.LogError("ScenePolish: no VehicleMovement found.");
            return;
        }

        Transform[] cars = { vm.vehicleV1, vm.vehicleV2, vm.vehicleV3 };

        for (int i = 0; i < 3; i++)
        {
            if (cars[i] == null) continue;
            Color c = colors[i];

            // Trail
            GameObject trailObj = new GameObject("TrailPoint");
            trailObj.transform.SetParent(cars[i], false);
            trailObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            TrailRenderer tr = trailObj.AddComponent<TrailRenderer>();
            tr.time = trailTime;
            tr.startWidth = trailWidth;
            tr.endWidth = 0f;
            tr.material = MakeMaterial(c);
            tr.startColor = c;
            tr.endColor = new Color(c.r, c.g, c.b, 0f);
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.alignment = LineAlignment.TransformZ;
            trailObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Ring under the car
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring_V" + (i + 1);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(cars[i], false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(ringSize, 0.01f, ringSize);

            Renderer r = ring.GetComponent<Renderer>();
            r.material = MakeMaterial(new Color(c.r, c.g, c.b, 1f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}