using TMPro;
using System;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class DailyFreeBoosterManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Button collectFreeRewardButton;
    [SerializeField] TMP_Text giftGrantedPanelText;
    [SerializeField] GameObject giftGrantedPanel;

    private DateTime lastDateToGetReward = DateTime.MinValue;

    #region Unity Functions

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += FetchLastDateToGetReward;
    }
    #endregion

    #region Playfab Codes

    public void CalculateIntractableStatus()
    {
        Debug.Log(lastDateToGetReward);
        Debug.Log(DateTime.UtcNow.Date);
        collectFreeRewardButton.interactable = lastDateToGetReward != DateTime.UtcNow.Date;
    }

    private void FetchLastDateToGetReward()
    {
        var getLastDateToGetRewardReq = new GetUserDataRequest { Keys = new() { PlayfabDataKeys.PlayerFreeRewardLastCollectedRewardDate } };
        PlayFabClientAPI.GetUserData(getLastDateToGetRewardReq, (res) =>
        {
            if (res.Data.ContainsKey(PlayfabDataKeys.PlayerFreeRewardLastCollectedRewardDate))
                if (DateTime.TryParse(res.Data[PlayfabDataKeys.PlayerFreeRewardLastCollectedRewardDate].Value, out DateTime date))
                    lastDateToGetReward = date;
        },
        err => Debug.LogError("Playfab Error Fetch Last Date on which Free Booster Reward was collected. \nError Message : " + err.ErrorMessage + "\nError Report : " + err.GenerateErrorReport()));
    }

    private void SetLastDateToGetRewardData()
    {
        var setLastDateToGetRewardReq = new UpdateUserDataRequest { Data = new() { { PlayfabDataKeys.PlayerFreeRewardLastCollectedRewardDate, DateTime.UtcNow.Date.ToString() } } };
        PlayFabClientAPI.UpdateUserData(setLastDateToGetRewardReq, (res) =>
        {
            Debug.Log("Successfully updated playfab last date to collect reward data!");
        },
        err => Debug.LogError("Playfab error while setting last date on which free booster reward was collected. \nError Message : " + err.ErrorMessage + "\nError Report : " + err.GenerateErrorReport()));
    }

    #endregion

    #region Button Clicks

    public void OnClick_CollectFreeBooster()
    {
        if(lastDateToGetReward != DateTime.UtcNow.Date)
        {
            lastDateToGetReward = DateTime.UtcNow.Date;
            SetLastDateToGetRewardData();
            var randBooster = BoosterAndPerksData.data.GetRandomItemFromBoosters();

            InventoryManager.manager.GrantItem(randBooster.itemId, (success) =>
            {
                if (success)
                    giftGrantedPanelText.text = "You recieved " + randBooster.itemName + " booster!";
                else
                    giftGrantedPanelText.text = "Failed to grant a free reward, Please try again later!";

                UIAnimationManager.manager.PopUpPanel(giftGrantedPanel);
            });
        }
    }

    public void OnClick_CloseGiftGrantedPanel()
    {
        UIAnimationManager.manager.PopDownPanel(giftGrantedPanel);
    }

    #endregion
}
