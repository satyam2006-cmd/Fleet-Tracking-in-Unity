using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FleetAnalyticsAuto : MonoBehaviour
{
    public string vehiclesUrl = "http://127.0.0.1:8000/vehicles";
    public string routesUrl = "http://127.0.0.1:8000/routes";
    public float refreshSeconds = 2f;
    public float uiUpdateSeconds = 0.2f;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly Color[] barColors =
    {
        new Color(1f, 0.85f, 0.2f),
        new Color(0.3f, 0.9f, 1f),
        new Color(1f, 0.4f, 0.9f)
    };

    TMP_Text summaryText;
    TMP_Text[] labels = new TMP_Text[3];
    TMP_Text[] infos = new TMP_Text[3];
    RectTransform[] fills = new RectTransform[3];

    VehicleMovement mover;
    VehicleInfo[] latest;
    int[] pointCount = new int[3];
    float[] durationMin = new float[3];
    float uiTimer;

    [Serializable]
    public class VehicleInfo
    {
        public string vehicle_id;
        public float latitude;
        public float longitude;
        public float distance_km;
    }

    [Serializable]
    public class VehicleList { public VehicleInfo[] items; }

    [Serializable]
    public class RoutePoint
    {
        public float latitude;
        public float longitude;
        public string timestamp;
    }

    [Serializable]
    public class RoutesData
    {
        public RoutePoint[] V1;
        public RoutePoint[] V2;
        public RoutePoint[] V3;
    }

    void Start()
    {
        mover = FindFirstObjectByType<VehicleMovement>();
        BuildUI();
        StartCoroutine(LoadRoutesOnce());
        StartCoroutine(RefreshLoop());
    }

    void Update()
    {
        uiTimer += Time.deltaTime;
        if (uiTimer < uiUpdateSeconds) return;
        uiTimer = 0f;
        if (latest != null) UpdateUI();
    }

    // ---------- UI BUILDING ----------

    void BuildUI()
    {
        GameObject canvasGo = new GameObject("AnalyticsCanvas",
            typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("AnalyticsPanel",
            typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1, 1);
        pr.anchoredPosition = new Vector2(-20, -20);
        pr.sizeDelta = new Vector2(480, 570);
        Image pi = panel.GetComponent<Image>();
        pi.color = new Color32(15, 23, 42, 220);
        pi.raycastTarget = false;

        MakeText(panel.transform, "AnalyticsTitle", -15, 50, 30, FontStyles.Bold,
                 Color.white, TextAlignmentOptions.Center).text = "ROUTE ANALYTICS";

        summaryText = MakeText(panel.transform, "AnalyticsSummary", -70, 100, 22,
                 FontStyles.Normal, new Color(1f, 0.95f, 0.6f), TextAlignmentOptions.TopLeft);

        for (int i = 0; i < 3; i++)
        {
            float top = -180 - i * 125;

            labels[i] = MakeText(panel.transform, "AnalyticsLabel" + i, top, 30, 22,
                FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);

            // bar background
            GameObject bg = new GameObject("AnalyticsBarBg" + i,
                typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(panel.transform, false);
            RectTransform bgr = bg.GetComponent<RectTransform>();
            bgr.anchorMin = new Vector2(0, 1);
            bgr.anchorMax = new Vector2(1, 1);
            bgr.pivot = new Vector2(0.5f, 1);
            bgr.anchoredPosition = new Vector2(0, top - 32);
            bgr.sizeDelta = new Vector2(-40, 22);
            Image bgi = bg.GetComponent<Image>();
            bgi.color = new Color32(51, 65, 85, 255);
            bgi.raycastTarget = false;

            // bar fill
            GameObject fill = new GameObject("AnalyticsBarFill" + i,
                typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            RectTransform fr = fill.GetComponent<RectTransform>();
            fr.anchorMin = new Vector2(0, 0);
            fr.anchorMax = new Vector2(0, 1);
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            Image fi = fill.GetComponent<Image>();
            fi.color = barColors[i];
            fi.raycastTarget = false;
            fills[i] = fr;

            infos[i] = MakeText(panel.transform, "AnalyticsInfo" + i, top - 60, 55, 18,
                FontStyles.Normal, new Color(0.8f, 0.85f, 0.95f), TextAlignmentOptions.TopLeft);
        }

        summaryText.text = "Connecting to backend...";
    }

    TMP_Text MakeText(Transform parent, string name, float posY, float height,
                      float fontSize, FontStyles style, Color color,
                      TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, posY);
        rt.sizeDelta = new Vector2(-40, height);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ---------- DATA ----------

    IEnumerator LoadRoutesOnce()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(routesUrl))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Analytics routes error: " + req.error);
                yield break;
            }

            RoutesData data = JsonUtility.FromJson<RoutesData>(req.downloadHandler.text);
            if (data == null) yield break;

            Measure(0, data.V1);
            Measure(1, data.V2);
            Measure(2, data.V3);
        }
    }

    void Measure(int idx, RoutePoint[] pts)
    {
        if (pts == null || pts.Length == 0) return;
        pointCount[idx] = pts.Length;

        DateTime first, last;
        if (DateTime.TryParse(pts[0].timestamp, Inv, DateTimeStyles.None, out first) &&
            DateTime.TryParse(pts[pts.Length - 1].timestamp, Inv, DateTimeStyles.None, out last))
        {
            durationMin[idx] = (float)(last - first).TotalMinutes;
        }
    }

    IEnumerator RefreshLoop()
    {
        while (true)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(vehiclesUrl))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    latest = null;
                    summaryText.text = "Backend offline";
                }
                else
                {
                    string json = "{\"items\":" + req.downloadHandler.text + "}";
                    VehicleList list = JsonUtility.FromJson<VehicleList>(json);
                    if (list != null && list.items != null) latest = list.items;
                }
            }
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    int IndexOf(string id)
    {
        if (id == "V1") return 0;
        if (id == "V2") return 1;
        if (id == "V3") return 2;
        return -1;
    }

    // ---------- DISPLAY ----------

    void UpdateUI()
    {
        float total = 0f, maxKm = 0.0001f, minKm = float.MaxValue;
        foreach (var v in latest)
        {
            total += v.distance_km;
            maxKm = Mathf.Max(maxKm, v.distance_km);
            minKm = Mathf.Min(minKm, v.distance_km);
        }
        float avg = latest.Length > 0 ? total / latest.Length : 0f;

        summaryText.text =
            "Fleet Total: " + total.ToString("F3", Inv) + " KM" +
            "\nAverage per Vehicle: " + avg.ToString("F3", Inv) + " KM" +
            "\nSpread (longest - shortest): " + (maxKm - minKm).ToString("F3", Inv) + " KM";

        foreach (var v in latest)
        {
            int idx = IndexOf(v.vehicle_id);
            if (idx < 0) continue;

            bool live = mover != null && mover.liveReady[idx];
            float progress = live ? mover.liveProgress[idx] : 0f;
            float liveKm = v.distance_km * progress;

            int rank = 1;
            int tiedCount = 0;
            foreach (var o in latest)
            {
                if (o.distance_km > v.distance_km + 0.0005f) rank++;
                else if (o != v && Mathf.Abs(o.distance_km - v.distance_km) <= 0.0005f) tiedCount++;
            }
            string rankText = tiedCount > 0 ? "Tied #" + rank : "#" + rank;

            float delta = avg > 0f ? (v.distance_km - avg) / avg * 100f : 0f;
            float speed = durationMin[idx] > 0f
                ? v.distance_km / (durationMin[idx] / 60f) : 0f;

            labels[idx].text = "<b>" + v.vehicle_id + "</b>   " +
                liveKm.ToString("F3", Inv) + " / " +
                v.distance_km.ToString("F3", Inv) + " KM   (Rank " + rankText + ")";

            fills[idx].anchorMax = new Vector2(
                Mathf.Clamp01(liveKm / maxKm), 1f);

            infos[idx].text =
                "Route points: " + pointCount[idx] +
                "   Duration: " + durationMin[idx].ToString("F0", Inv) + " min" +
                "\nAvg speed: " + speed.ToString("F1", Inv) + " km/h" +
                "   vs fleet avg: " + delta.ToString("+0.0;-0.0;0.0", Inv) + "%";
        }
    }
}