using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FleetDashboardAuto : MonoBehaviour
{
    public string vehiclesUrl = "http://127.0.0.1:8000/vehicles";
    public float refreshSeconds = 2f;
    public float uiUpdateSeconds = 0.2f;

    TMP_Text summaryText;
    TMP_Text[] cards = new TMP_Text[3];

    VehicleMovement mover;
    VehicleInfo[] latest;
    float uiTimer;
    bool backendOnline = false;

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [System.Serializable]
    public class VehicleInfo
    {
        public string vehicle_id;
        public float latitude;
        public float longitude;
        public float distance_km;
    }

    [System.Serializable]
    public class VehicleList
    {
        public VehicleInfo[] items;
    }

    void Start()
    {
        mover = FindFirstObjectByType<VehicleMovement>();
        if (mover == null)
            Debug.LogError("Dashboard: no VehicleMovement found in the scene.");

        BuildUI();
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
        GameObject canvasGo = new GameObject("DashboardCanvas",
            typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("DashboardPanel",
            typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0, 1);
        pr.anchoredPosition = new Vector2(20, -20);
        pr.sizeDelta = new Vector2(480, 640);
        panel.GetComponent<Image>().color = new Color32(15, 23, 42, 220);

        MakeText(panel.transform, "TitleText", -15, 50, 32, FontStyles.Bold,
                 Color.white, TextAlignmentOptions.Center).text = "FLEET DASHBOARD";

        summaryText = MakeText(panel.transform, "SummaryText", -75, 90, 24,
                 FontStyles.Normal, new Color(1f, 0.95f, 0.6f), TextAlignmentOptions.Left);

        for (int i = 0; i < 3; i++)
        {
            cards[i] = MakeText(panel.transform, "V" + (i + 1) + "Text",
                     -180 - i * 145, 135, 22, FontStyles.Normal,
                     Color.white, TextAlignmentOptions.TopLeft);
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
        return t;
    }

    // ---------- BACKEND DATA (distance totals) ----------

    IEnumerator RefreshLoop()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(vehiclesUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    backendOnline = false;
                    summaryText.text = "Backend offline";
                    Debug.LogError("Dashboard API error: " + request.error);
                }
                else
                {
                    string json = "{\"items\":" + request.downloadHandler.text + "}";
                    VehicleList list = JsonUtility.FromJson<VehicleList>(json);
                    if (list != null && list.items != null)
                    {
                        latest = list.items;
                        backendOnline = true;
                    }
                }
            }

            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    // ---------- LIVE DISPLAY ----------

    int IndexOf(string id)
    {
        if (id == "V1") return 0;
        if (id == "V2") return 1;
        if (id == "V3") return 2;
        return -1;
    }

    void UpdateUI()
    {
        if (!backendOnline) return;

        float fleetTotal = 0f;
        float fleetLive = 0f;
        string longest = "-";
        float longestKm = -1f;

        foreach (var v in latest)
        {
            fleetTotal += v.distance_km;
            if (v.distance_km > longestKm)
            {
                longestKm = v.distance_km;
                longest = v.vehicle_id;
            }
        }

        foreach (var v in latest)
        {
            int idx = IndexOf(v.vehicle_id);
            if (idx < 0) continue;

            bool live = mover != null && mover.liveReady[idx];

            float lat = live ? mover.liveLat[idx] : v.latitude;
            float lon = live ? mover.liveLon[idx] : v.longitude;
            float progress = live ? mover.liveProgress[idx] : 0f;
            float liveKm = v.distance_km * progress;
            string status = !live ? "Starting"
                          : (mover.liveMoving[idx] ? "Moving" : "Waiting");

            fleetLive += liveKm;

            var sb = new StringBuilder();
            sb.AppendLine("<b>VEHICLE " + v.vehicle_id + "</b>");
            sb.AppendLine("Latitude: " + lat.ToString("F4", Inv));
            sb.AppendLine("Longitude: " + lon.ToString("F4", Inv));
            sb.AppendLine("Distance: " + liveKm.ToString("F3", Inv) +
                          " / " + v.distance_km.ToString("F3", Inv) + " KM");
            sb.Append("Status: " + status +
                      "   Progress: " + (progress * 100f).ToString("F0", Inv) + "%");
            cards[idx].text = sb.ToString();
        }

        summaryText.text =
            "Total Vehicles: " + latest.Length +
            "\nFleet Distance: " + fleetLive.ToString("F3", Inv) +
            " / " + fleetTotal.ToString("F3", Inv) + " KM" +
            "\nLongest Route: " + longest + " (" + longestKm.ToString("F3", Inv) + " KM)";
    }
}