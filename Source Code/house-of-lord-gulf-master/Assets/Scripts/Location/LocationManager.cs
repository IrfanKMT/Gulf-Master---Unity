using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class LocationManager : MonoBehaviour
{
    public static async Task<Country> GetCurrentLocation()
    {
        using UnityWebRequest req = UnityWebRequest.Get("https://extreme-ip-lookup.com/json/?key=G5xynkcOCaRFhQeGw7ZG");
        req.SendWebRequest();

        while (!req.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error In Getting Location Of The User: " + req.error);
            return null;
        }
        else
        {
            var jsonData = req.downloadHandler.text;
            try
            {
                Country location = JsonUtility.FromJson<Country>(jsonData);
                return location;
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing Location Web Request Data : Error Message : " + e.Message + "\nRequest Data : " + jsonData);
                return null;
            }
        }
    }
}

public class Country
{
    public string businessName;
    public string businessWebsite;
    public string city;
    public string continent;
    public string country;
    public string countryCode;
    public string ipName;
    public string ipType;
    public string isp;
    public string lat;
    public string lon;
    public string org;
    public string query;
    public string region;
    public string status;

}
