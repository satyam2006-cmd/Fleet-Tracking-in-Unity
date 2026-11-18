using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintBarAuto : MonoBehaviour
{
    static readonly Color[] colors =
    {
        new Color(1f, 0.85f, 0.2f),
        new Color(0.3f, 0.9f, 1f),
        new Color(1f, 0.4f, 0.9f)
    };

    void Start()
    {
        GameObject canvasGo = new GameObject("HintCanvas",
            typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;   // keeps it behind the other panels

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Bar at the bottom centre
        GameObject bar = new GameObject("HintBar",
            typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvasGo.transform, false);
        RectTransform br = bar.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0f);
        br.anchoredPosition = new Vector2(0, 20);
        br.sizeDelta = new Vector2(1000, 70);
        Image bi = bar.GetComponent<Image>();
        bi.color = new Color32(15, 23, 42, 220);
        bi.raycastTarget = false;

        // Legend swatches + names
        for (int i = 0; i < 3; i++)
        {
            float x = -440 + i * 110;

            GameObject sw = new GameObject("Swatch" + i,
                typeof(RectTransform), typeof(Image));
            sw.transform.SetParent(bar.transform, false);
            RectTransform sr = sw.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = new Vector2(x, 0);
            sr.sizeDelta = new Vector2(24, 24);
            Image si = sw.GetComponent<Image>();
            si.color = colors[i];
            si.raycastTarget = false;

            MakeText(bar.transform, "LegendText" + i, new Vector2(x + 55, 0),
                     new Vector2(80, 40), 26, FontStyles.Bold, Color.white,
                     TextAlignmentOptions.Left).text = "V" + (i + 1);
        }

        // Controls hint
        MakeText(bar.transform, "HintText", new Vector2(190, 0),
                 new Vector2(600, 60), 22, FontStyles.Normal,
                 new Color(0.8f, 0.85f, 0.95f), TextAlignmentOptions.Center).text =
            "Click a vehicle card or press 1 / 2 / 3 to follow\n" +
            "Click FLEET DASHBOARD or press 0 for overview";
    }

    TMP_Text MakeText(Transform parent, string name, Vector2 pos, Vector2 size,
                      float fontSize, FontStyles style, Color color,
                      TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}