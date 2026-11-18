using UnityEngine;

public class RoadPathBuilder : MonoBehaviour
{
    [Header("Road rules")]
    public bool leftHandTraffic = true;   // India = left-hand traffic
    public float laneOffset = 3.5f;       // distance from road centre to the driving lane
    public float cornerCut = 5f;          // how rounded the corners are

    void Awake()
    {
        VehicleMovement vm = FindFirstObjectByType<VehicleMovement>();
        if (vm == null)
        {
            Debug.LogError("RoadPathBuilder: no VehicleMovement found.");
            return;
        }

        Vector2[] v1 =   // right half
        {
            new Vector2(-7.5f, -67.5f), new Vector2(52.5f, -67.5f),
            new Vector2(52.5f, 67.5f),  new Vector2(-7.5f, 67.5f)
        };
        Vector2[] v2 =   // top-left block
        {
            new Vector2(-52.5f, 22.5f), new Vector2(-7.5f, 22.5f),
            new Vector2(-7.5f, 67.5f),  new Vector2(-52.5f, 67.5f)
        };
        Vector2[] v3 =   // bottom-left block
        {
            new Vector2(-52.5f, -67.5f), new Vector2(-7.5f, -67.5f),
            new Vector2(-7.5f, 22.5f),   new Vector2(-52.5f, 22.5f)
        };

        RoadPath[] paths =
        {
            Make(v1, vm), Make(v2, vm), Make(v3, vm)
        };
        vm.paths = paths;

        TrafficSystem ts = GetComponent<TrafficSystem>();
        if (ts == null) ts = gameObject.AddComponent<TrafficSystem>();
        ts.Register(paths);

        Debug.Log("RoadPathBuilder: built 3 lane paths.");
    }

    RoadPath Make(Vector2[] corners, VehicleMovement vm)
    {
        Vector2[] c = (Vector2[])corners.Clone();
        if (!leftHandTraffic) System.Array.Reverse(c);
        float lane = leftHandTraffic ? laneOffset : -laneOffset;
        return RoadPath.FromCorners(c, lane, cornerCut, vm.groundY + vm.vehicleHeight);
    }
}