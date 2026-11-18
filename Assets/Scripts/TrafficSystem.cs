using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum SignalLight { Red, Yellow, Green }

public class SignalApproach
{
    public SignalLight State = SignalLight.Red;
    public Renderer redLamp, yellowLamp, greenLamp;

    static readonly Color redOn = new Color(1f, 0.1f, 0.1f);
    static readonly Color redOff = new Color(0.25f, 0.03f, 0.03f);
    static readonly Color yellowOn = new Color(1f, 0.85f, 0.1f);
    static readonly Color yellowOff = new Color(0.25f, 0.2f, 0.02f);
    static readonly Color greenOn = new Color(0.1f, 1f, 0.2f);
    static readonly Color greenOff = new Color(0.03f, 0.25f, 0.05f);

    public void Apply()
    {
        TrafficSystem.SetColor(redLamp, State == SignalLight.Red ? redOn : redOff);
        TrafficSystem.SetColor(yellowLamp, State == SignalLight.Yellow ? yellowOn : yellowOff);
        TrafficSystem.SetColor(greenLamp, State == SignalLight.Green ? greenOn : greenOff);
    }
}

public class SignalStop
{
    public float distance;            // distance along the path where the car must stop
    public SignalApproach approach;
}

public class RoadPath
{
    public Vector3[] pts;
    public float[] cum;
    public float Length;
    public List<SignalStop> stops = new List<SignalStop>();

    static Vector2 Left(Vector2 d) { return new Vector2(-d.y, d.x); }

    // Builds a closed loop in one lane with rounded corners.
    // lane > 0 = left of travel direction, lane < 0 = right.
    public static RoadPath FromCorners(Vector2[] c, float lane, float cut, float y)
    {
        int n = c.Length;
        List<Vector3> list = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            Vector2 prev = c[(i + n - 1) % n];
            Vector2 cur = c[i];
            Vector2 next = c[(i + 1) % n];

            Vector2 din = (cur - prev).normalized;
            Vector2 dout = (next - cur).normalized;

            Vector2 corner = cur + Left(din) * lane + Left(dout) * lane;
            Vector2 a = corner - din * cut;
            Vector2 b = corner + dout * cut;

            const int steps = 10;
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * corner + t * t * b;
                list.Add(new Vector3(p.x, y, p.y));
            }
        }

        RoadPath rp = new RoadPath();
        rp.pts = list.ToArray();
        rp.cum = new float[rp.pts.Length + 1];
        for (int i = 0; i < rp.pts.Length; i++)
            rp.cum[i + 1] = rp.cum[i] + Vector3.Distance(rp.pts[i], rp.pts[(i + 1) % rp.pts.Length]);
        rp.Length = rp.cum[rp.pts.Length];
        return rp;
    }

    public Vector3 Evaluate(float d, out Vector3 dir)
    {
        d = Mathf.Repeat(d, Length);

        int lo = 0, hi = pts.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (cum[mid] <= d) lo = mid; else hi = mid - 1;
        }

        Vector3 a = pts[lo];
        Vector3 b = pts[(lo + 1) % pts.Length];
        float seg = Mathf.Max(cum[lo + 1] - cum[lo], 0.0001f);
        float t = (d - cum[lo]) / seg;

        dir = b - a;
        return Vector3.Lerp(a, b, t);
    }

    // 1 = full speed, 0 = stopped
    public float SpeedFactor(float d, float brakeDist, float yellowCommit)
    {
        float f = 1f;
        foreach (SignalStop s in stops)
        {
            if (s.approach.State == SignalLight.Green) continue;

            float dist = s.distance - d;
            if (dist < 0f) dist += Length;
            if (dist > brakeDist) continue;

            if (s.approach.State == SignalLight.Yellow && dist < yellowCommit) continue;

            float sf = dist <= 0.15f ? 0f : Mathf.Clamp(dist / brakeDist, 0.08f, 1f);
            f = Mathf.Min(f, sf);
        }
        return f;
    }
}

public class TrafficSystem : MonoBehaviour
{
    [Header("Junction tiles (centre X, Z)")]
    public Vector2[] junctionCenters =
    {
        new Vector2(-7.5f, -67.5f),
        new Vector2(-7.5f, 22.5f),
        new Vector2(-7.5f, 67.5f),
        new Vector2(-52.5f, 22.5f)
    };
    public float tileHalf = 7.5f;

    [Header("Signal timing (seconds)")]
    public float greenTime = 6f;
    public float yellowTime = 2f;
    public float allRedTime = 1.5f;

    [Header("Stopping and signal pole")]
    public float stopMargin = 3.5f;   // how far before the junction the car stops
    public float poleSide = 3.5f;     // pole distance to the left of the lane

    class Junction
    {
        public List<SignalApproach> list = new List<SignalApproach>();
        public float offset;
    }

    readonly List<Junction> junctions = new List<Junction>();
    Transform root;

    static int DirKey(Vector3 d)
    {
        if (Mathf.Abs(d.x) > Mathf.Abs(d.z)) return d.x > 0 ? 0 : 1;
        return d.z > 0 ? 2 : 3;
    }

    bool Inside(Vector3 p, Vector2 c)
    {
        return Mathf.Abs(p.x - c.x) < tileHalf && Mathf.Abs(p.z - c.y) < tileHalf;
    }

    public void Register(RoadPath[] paths)
    {
        root = new GameObject("TrafficSignals").transform;

        for (int j = 0; j < junctionCenters.Length; j++)
        {
            Vector2 c = junctionCenters[j];
            Junction jn = new Junction();
            jn.offset = j * 2.3f;
            Dictionary<int, SignalApproach> map = new Dictionary<int, SignalApproach>();

            foreach (RoadPath p in paths)
            {
                if (p == null) continue;

                const float step = 0.5f;
                int samples = Mathf.CeilToInt(p.Length / step);
                Vector3 dir;
                bool prevInside = Inside(p.Evaluate(-step, out dir), c);

                for (int s = 0; s < samples; s++)
                {
                    float d = s * step;
                    Vector3 pos = p.Evaluate(d, out dir);
                    bool inside = Inside(pos, c);

                    if (inside && !prevInside)
                    {
                        int key = DirKey(dir);
                        SignalApproach ap;
                        if (!map.TryGetValue(key, out ap))
                        {
                            ap = new SignalApproach();
                            map[key] = ap;
                            jn.list.Add(ap);
                            BuildHead(p, d, ap, j, key);
                        }

                        SignalStop stop = new SignalStop();
                        stop.distance = Mathf.Repeat(d - stopMargin, p.Length);
                        stop.approach = ap;
                        p.stops.Add(stop);
                    }
                    prevInside = inside;
                }
            }

            if (jn.list.Count > 0) junctions.Add(jn);
        }

        foreach (Junction jn in junctions)
            foreach (SignalApproach ap in jn.list)
                ap.Apply();

        Debug.Log("TrafficSystem: " + junctions.Count + " signalled junctions created.");
    }

    void Update()
    {
        float phaseLen = greenTime + yellowTime + allRedTime;

        foreach (Junction jn in junctions)
        {
            int n = jn.list.Count;
            int phase = -1;
            float local = 0f;

            if (n > 1)
            {
                float t = Mathf.Repeat(Time.time + jn.offset, n * phaseLen);
                phase = Mathf.Min((int)(t / phaseLen), n - 1);
                local = t - phase * phaseLen;
            }

            for (int i = 0; i < n; i++)
            {
                SignalLight s = SignalLight.Red;
                if (n == 1) s = SignalLight.Green;
                else if (i == phase)
                {
                    if (local < greenTime) s = SignalLight.Green;
                    else if (local < greenTime + yellowTime) s = SignalLight.Yellow;
                }

                if (jn.list[i].State != s)
                {
                    jn.list[i].State = s;
                    jn.list[i].Apply();
                }
            }
        }
    }

    // ---------- visuals ----------

    public static Material MakeMaterial(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    public static void SetColor(Renderer r, Color c)
    {
        if (r == null) return;
        Material m = r.material;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }

    GameObject Prim(PrimitiveType type, string name, Transform parent, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(parent, false);
        Renderer r = go.GetComponent<Renderer>();
        r.material = MakeMaterial(color);
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    void BuildHead(RoadPath p, float entryD, SignalApproach ap, int j, int key)
    {
        Vector3 dir;
        Vector3 lanePos = p.Evaluate(entryD - 1f, out dir);
        dir.y = 0f;
        dir.Normalize();
        Vector3 left = new Vector3(-dir.z, 0f, dir.x);

        GameObject parent = new GameObject("Signal_J" + j + "_" + key);
        parent.transform.SetParent(root, false);

        // stop line across the lane
        Vector3 linePos = p.Evaluate(entryD - 1.2f, out dir);
        GameObject line = Prim(PrimitiveType.Cube, "StopLine", parent.transform, Color.white);
        line.transform.position = new Vector3(linePos.x, 0.16f, linePos.z);
        line.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        line.transform.localScale = new Vector3(4.5f, 0.04f, 0.5f);

        // pole
        Vector3 polePos = lanePos + left * poleSide;
        GameObject pole = Prim(PrimitiveType.Cylinder, "Pole", parent.transform,
                               new Color(0.25f, 0.25f, 0.28f));
        pole.transform.position = new Vector3(polePos.x, 2.3f, polePos.z);
        pole.transform.localScale = new Vector3(0.25f, 2.3f, 0.25f);

        // head facing the approaching cars
        GameObject head = new GameObject("Head");
        head.transform.SetParent(parent.transform, false);
        head.transform.position = new Vector3(polePos.x, 5.0f, polePos.z);
        head.transform.rotation = Quaternion.LookRotation(-dir);

        GameObject body = Prim(PrimitiveType.Cube, "Body", head.transform,
                               new Color(0.08f, 0.08f, 0.1f));
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.1f, 2.9f, 0.7f);

        ap.redLamp = MakeLamp(head.transform, "Red", 0.95f);
        ap.yellowLamp = MakeLamp(head.transform, "Yellow", 0f);
        ap.greenLamp = MakeLamp(head.transform, "Green", -0.95f);
    }

    Renderer MakeLamp(Transform head, string name, float y)
    {
        GameObject lamp = Prim(PrimitiveType.Sphere, name, head, Color.gray);
        lamp.transform.localPosition = new Vector3(0f, y, 0.4f);
        lamp.transform.localScale = new Vector3(0.8f, 0.8f, 0.5f);
        return lamp.GetComponent<Renderer>();
    }
}