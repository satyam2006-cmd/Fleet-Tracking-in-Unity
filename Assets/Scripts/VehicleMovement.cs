using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class VehicleMovement : MonoBehaviour
{
    public Transform vehicleV1;
    public Transform vehicleV2;
    public Transform vehicleV3;

    public string apiUrl = "http://127.0.0.1:8000/routes";

    [Header("Ground / orientation")]
    public float groundY = 0f;
    public float vehicleHeight = 0f;
    public float modelYawOffset = 0f;
    public float turnSpeed = 8f;

    [Header("Speed and signals")]
    public float secondsPerGpsInterval = 3f;
    public float brakeDistance = 14f;        // start slowing this far before a red light
    public float yellowCommitDistance = 3f;  // closer than this, a car drives through yellow
    public bool loopRoutes = true;

    // Set by RoadPathBuilder before Start()
    [System.NonSerialized] public RoadPath[] paths = new RoadPath[3];

    // Live data read by the dashboard (index 0 = V1, 1 = V2, 2 = V3)
    [HideInInspector] public float[] liveLat = new float[3];
    [HideInInspector] public float[] liveLon = new float[3];
    [HideInInspector] public float[] liveProgress = new float[3];
    [HideInInspector] public bool[] liveMoving = new bool[3];
    [HideInInspector] public bool[] liveReady = new bool[3];

    [System.Serializable]
    public class GPSPoint
    {
        public float latitude;
        public float longitude;
        public string timestamp;
    }

    [System.Serializable]
    public class RoutesData
    {
        public GPSPoint[] V1;
        public GPSPoint[] V2;
        public GPSPoint[] V3;
    }

    void Start()
    {
        StartCoroutine(LoadRoutes());
    }

    IEnumerator LoadRoutes()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + request.error);
                yield break;
            }

            RoutesData routes =
                JsonUtility.FromJson<RoutesData>(request.downloadHandler.text);

            if (routes == null || routes.V1 == null ||
                routes.V2 == null || routes.V3 == null)
            {
                Debug.LogError("Invalid GPS route data!");
                yield break;
            }

            Debug.Log("ALL GPS ROUTES RECEIVED!");

            StartCoroutine(MoveVehicle(0, vehicleV1, routes.V1));
            StartCoroutine(MoveVehicle(1, vehicleV2, routes.V2));
            StartCoroutine(MoveVehicle(2, vehicleV3, routes.V3));
        }
    }

    float[] GpsFractions(GPSPoint[] g)
    {
        float[] cum = new float[g.Length];
        for (int i = 1; i < g.Length; i++)
        {
            float latRad = g[i].latitude * Mathf.Deg2Rad;
            float dx = (g[i].longitude - g[i - 1].longitude) * Mathf.Cos(latRad) * 111320f;
            float dz = (g[i].latitude - g[i - 1].latitude) * 111320f;
            cum[i] = cum[i - 1] + Mathf.Sqrt(dx * dx + dz * dz);
        }
        float total = Mathf.Max(cum[g.Length - 1], 0.0001f);
        for (int i = 0; i < cum.Length; i++) cum[i] /= total;
        return cum;
    }

    IEnumerator MoveVehicle(int idx, Transform vehicle, GPSPoint[] gps)
    {
        if (vehicle == null || gps.Length < 2) yield break;

        RoadPath road = paths[idx];
        if (road == null)
        {
            Debug.LogError("No road path for " + vehicle.name +
                           ". Is RoadPathBuilder on the Dashboard object?");
            yield break;
        }

        float[] frac = GpsFractions(gps);
        bool first = true;

        do
        {
            liveReady[idx] = true;

            for (int i = 1; i < gps.Length; i++)
            {
                float dStart = frac[i - 1] * road.Length;
                float dEnd = frac[i] * road.Length;
                float elapsed = 0f;

                while (elapsed < secondsPerGpsInterval)
                {
                    float k0 = Mathf.Clamp01(elapsed / secondsPerGpsInterval);
                    float dNow = Mathf.Lerp(dStart, dEnd, k0);

                    // slow down / stop for red and yellow signals
                    float factor = road.SpeedFactor(dNow, brakeDistance, yellowCommitDistance);
                    liveMoving[idx] = factor > 0.05f;
                    elapsed += Time.deltaTime * factor;

                    float k = Mathf.Clamp01(elapsed / secondsPerGpsInterval);
                    float d = Mathf.Lerp(dStart, dEnd, k);

                    Vector3 dir;
                    Vector3 pos = road.Evaluate(d, out dir);
                    pos.y = groundY + vehicleHeight;
                    vehicle.position = pos;

                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion target =
                            Quaternion.LookRotation(dir) *
                            Quaternion.Euler(0f, modelYawOffset, 0f);
                        vehicle.rotation = first
                            ? target
                            : Quaternion.Slerp(vehicle.rotation, target,
                                               Time.deltaTime * turnSpeed);
                        first = false;
                    }

                    liveLat[idx] = Mathf.Lerp(gps[i - 1].latitude, gps[i].latitude, k);
                    liveLon[idx] = Mathf.Lerp(gps[i - 1].longitude, gps[i].longitude, k);
                    liveProgress[idx] = Mathf.Lerp(frac[i - 1], frac[i], k);

                    yield return null;
                }
            }

            liveProgress[idx] = 1f;
            liveMoving[idx] = false;

            if (loopRoutes) yield return new WaitForSeconds(1f);

        } while (loopRoutes);
    }
}