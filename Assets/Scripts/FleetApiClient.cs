
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FleetApiClient : MonoBehaviour
{
    private string apiUrl = "http://127.0.0.1:8000/vehicles";

    [System.Serializable]
    public class Vehicle
    {
        public string vehicle_id;
        public float latitude;
        public float longitude;
        public float distance_km;
    }

    [System.Serializable]
    public class VehicleList
    {
        public Vehicle[] vehicles;
    }

    void Start()
    {
        StartCoroutine(GetVehicles());
    }

    IEnumerator GetVehicles()
    {
        using (UnityWebRequest request =
               UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                // Wrap JSON array for Unity JsonUtility
                string wrappedJson =
                    "{\"vehicles\":" + json + "}";

                VehicleList data =
                    JsonUtility.FromJson<VehicleList>(wrappedJson);

                Debug.Log("CONNECTED TO FLEET API!");

                foreach (Vehicle vehicle in data.vehicles)
                {
                    Debug.Log(
                        "Vehicle: " + vehicle.vehicle_id +
                        " | Latitude: " + vehicle.latitude +
                        " | Longitude: " + vehicle.longitude +
                        " | Distance: " + vehicle.distance_km + " KM"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    "API ERROR: " + request.error
                );
            }
        }
    }
}
