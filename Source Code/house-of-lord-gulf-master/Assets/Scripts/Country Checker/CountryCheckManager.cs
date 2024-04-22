using UnityEngine;

public class CountryCheckManager : MonoBehaviour
{
    [SerializeField] string[] blockCountryCodes;

    private async void Start()
    {
        LoadingManager.manager.ShowLoadingBar("Checking Country Availability...");

        Debug.Log("Fetching User Location");
        var loc = await LocationManager.GetCurrentLocation();
        Debug.Log("User Location Fetched: " + loc.countryCode);

        bool blocked = false;

        foreach (var item in blockCountryCodes)
            if (loc.countryCode.Equals(item))
                blocked = true;

        if (!blocked)
        {
            Debug.Log("User Location Check Completed: Logging you in!");
            AuthenticationManager.manager.StartLoginProcess();
        }
        else
            LoadingManager.manager.UpdateLoadingText("Country Not Available...");
    }
}
