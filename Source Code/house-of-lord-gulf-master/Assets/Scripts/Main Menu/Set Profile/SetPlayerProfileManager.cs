using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;

public class SetPlayerProfileManager : MonoBehaviour
{
    public static SetPlayerProfileManager manager;

    [SerializeField] TMP_Text age_text;
    [SerializeField] TMP_Text error_text;

    [Header("Gender Selection")]
    [SerializeField] Image maleGenderSelectionImage;
    [SerializeField] Image femaleGenderSelectionImage;
    [SerializeField] Sprite selectedMaleGenderSprite;
    [SerializeField] Sprite selectedFemaleGenderSprite;
    [SerializeField] Sprite deselectedMaleGenderSprite;
    [SerializeField] Sprite deselectedFemaleGenderSprite;

    [Header("Country Selection Material")]
    [SerializeField] Material selectedCountryBuildingMat;
    [SerializeField] Material selectedCountryFlagMat;
    [SerializeField] Material normalMat;

    [Header("Country Selection")]
    [SerializeField] Image[] countryBuildingImages;
    [SerializeField] Image[] countryFlagImages;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    #endregion

    #region Profile Selection Callbacks

    public void SetGender(int gender)
    {
        PlayerData.Gender = gender;

        if(gender == 1)
        {
            maleGenderSelectionImage.sprite = selectedMaleGenderSprite;
            femaleGenderSelectionImage.sprite = deselectedFemaleGenderSprite;
        }
        else
        {
            maleGenderSelectionImage.sprite = deselectedMaleGenderSprite;
            femaleGenderSelectionImage.sprite = selectedFemaleGenderSprite;
        }
    }

    public void SetCountry(int country)
    {
        PlayerData.Country = country;

        foreach (var item in countryBuildingImages)
            item.material = normalMat;

        foreach (var item in countryFlagImages)
            item.material = normalMat;

        countryBuildingImages[country-1].material = selectedCountryBuildingMat;
        countryFlagImages[country-1].material = selectedCountryFlagMat;
    }

    public void SetAge(float age)
    {
        age_text.text = ((int)age).ToString();
        PlayerData.Age = (int)age;
    }

    #endregion

    #region Button Callbacks

    public void OnClick_Submit()
    {
        ProfileData data = new(PlayerData.Username, PlayerData.Age, PlayerData.Gender, PlayerData.Country) ;
        string json = JsonUtility.ToJson(data);

        if (IsPlayerProfileCorrect(json))
            UpdatePlayerPlayfabData(json);
        else
            error_text.text = "Please enter valid details";
    }

    #endregion

    #region Player Profile Validation

    public bool IsPlayerProfileCorrect(string profileData)
    {
        ProfileData data = JsonUtility.FromJson<ProfileData>(profileData);
        return IsDataCorrect(data);
    }

    public bool IsDataCorrect(ProfileData data)
    {
        if (string.IsNullOrEmpty(data.UserName))
            return false;

        if (data.Age == 0)
            return false;

        if (data.Gender == 0)
            return false;

        if (data.Country == 0)
            return false;

        return true;
    }

    #endregion

    #region UpdatePlayerData

    void UpdatePlayerPlayfabData(string json)
    {
        LoadingManager.manager.ShowLoadingBar("Creating Your Profile...");

        var updateUserDataRequest = new UpdateUserDataRequest
        {
           Data = new Dictionary<string, string>
           {
               {PlayfabDataKeys.PlayerProfile, json }
           }
        };

        PlayFabClientAPI.UpdateUserData(updateUserDataRequest, OnUserProfileDataUpdated, OnUserProfileDataUpdateError);
    }

    private void OnUserProfileDataUpdated(UpdateUserDataResult result)
    {
        AuthenticationManager.manager.TriggerOnLoggedInEvent();
    }

    private void OnUserProfileDataUpdateError(PlayFabError error)
    {
        error_text.text = error.ErrorMessage;
        Debug.LogError("Set Player Profile Error :\n" + error.GenerateErrorReport());
        LoadingManager.manager.HideLoadingBar();
    }

    #endregion
}


