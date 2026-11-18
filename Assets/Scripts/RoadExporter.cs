using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class RoadExporter : MonoBehaviour
{
    // Any object whose name contains one of these words is exported
    public string[] keywords = { "road", "street", "cross", "junction", "curve", "turn", "lane", "parking" };

    void Start()
    {
        var sb = new StringBuilder();
        sb.AppendLine("name,centerX,centerZ,centerY,rotY,sizeX,sizeZ,sizeY");

        int count = 0;
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name.ToLower();
            bool match = false;
            foreach (string k in keywords)
                if (n.Contains(k)) { match = true; break; }
            if (!match) continue;

            Bounds b = r.bounds;
            sb.AppendLine(
                r.gameObject.name + "," +
                b.center.x.ToString("F2") + "," +
                b.center.z.ToString("F2") + "," +
                b.center.y.ToString("F2") + "," +
                r.transform.eulerAngles.y.ToString("F0") + "," +
                b.size.x.ToString("F2") + "," +
                b.size.z.ToString("F2") + "," +
                b.size.y.ToString("F2"));
            count++;
        }

        string path = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "road_export.txt"));
        File.WriteAllText(path, sb.ToString());

        Debug.Log("RoadExporter: wrote " + count + " objects to " + path);
    }
}