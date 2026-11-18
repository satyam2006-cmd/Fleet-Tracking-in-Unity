using TMPro;
using UnityEngine;

public class VehicleLabels : MonoBehaviour
{
    public float labelHeight = 3f;   // how far above the car
    public float fontSize = 10f;     // increase if the labels look too small

    Transform[] cars = new Transform[3];
    TextMeshPro[] labels = new TextMeshPro[3];

    static readonly Color[] colors =
    {
        new Color(1f, 0.85f, 0.2f),   // V1 yellow
        new Color(0.3f, 0.9f, 1f),    // V2 cyan
        new Color(1f, 0.4f, 0.9f)     // V3 magenta
    };

    void Start()
    {
        VehicleMovement vm = FindFirstObjectByType<VehicleMovement>();
        if (vm == null)
        {
            Debug.LogError("VehicleLabels: no VehicleMovement found.");
            return;
        }

        cars[0] = vm.vehicleV1;
        cars[1] = vm.vehicleV2;
        cars[2] = vm.vehicleV3;

        for (int i = 0; i < 3; i++)
        {
            GameObject go = new GameObject("Label_V" + (i + 1));
            TextMeshPro t = go.AddComponent<TextMeshPro>();
            t.text = "V" + (i + 1);
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = colors[i];
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(0, 0, 0, 255);
            labels[i] = t;
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (cars[i] == null || labels[i] == null) continue;

            labels[i].transform.position =
                cars[i].position + Vector3.up * labelHeight;
            labels[i].transform.rotation = cam.transform.rotation;
        }
    }
}