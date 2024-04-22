using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class BattlePassManager : MonoBehaviour
{
    public static BattlePassManager manager;
    [SerializeField] BattlePassUIManager uiManager;

    public event Action OnPlayerDataUpdated;

    internal BattlePassUserData battlePassUserData = new();
    internal BattlePassData battlePassData;
    internal BattlePassUserData tempUserData;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += GetBattlePassData;
        GameMatchEventManager.OnWonGame += IncreasePlayerBattlePassLevel;
    }

    #endregion

    #region Get Battle Pass Data From PlayFab

    private void GetBattlePassData()
    {
        var getBattlePassDataRequest = new GetTitleDataRequest{Keys = new List<string> { PlayfabDataKeys.TitleData_BattlePass }};
        PlayFabClientAPI.GetTitleData(getBattlePassDataRequest, OnBattlePassDataRecieved, OnRecievedBattlePassDataFailed);
    }

    private void OnBattlePassDataRecieved(GetTitleDataResult data)
    {
        if (data.Data.ContainsKey(PlayfabDataKeys.TitleData_BattlePass))
        {
            string jsonData = data.Data[PlayfabDataKeys.TitleData_BattlePass];
            BattlePassData battlepass = JsonConvert.DeserializeObject<BattlePassData>(jsonData);

            if(battlepass != null)
            {
                //Debug.Log(JsonUtility.ToJson(battlepass));
                battlePassData = battlepass;
                GetBattlePassUserData();
            }
            else
            {
                Debug.LogError($"Battle Pass Object cant be made with the json title data.\nJson Data : \n{jsonData}");
            }
        }
        else
        {
            Debug.LogError($"OnBattlePassDataRecieved Error\nData not found in the recieved battle pass data result. Key : {PlayfabDataKeys.TitleData_BattlePass}");
        }
    }

    private void OnRecievedBattlePassDataFailed(PlayFabError error)
    {
        Debug.LogError($"OnRecievedBattlePassDataFailed Error\nRecieveing Remote Battle Pass Data Failed.\nTitle Data Key : {PlayfabDataKeys.TitleData_BattlePass}\nError Message : {error.ErrorMessage}\nDetailed Error Report : {error.GenerateErrorReport()}");
    }

    #endregion

    #region Get Player Battle Pass Data

    private void GetBattlePassUserData()
    {
        var getBattlePassDataRequest = new GetUserDataRequest { Keys = new List<string> { PlayfabDataKeys.PlayerBattlePassData } };
        PlayFabClientAPI.GetUserData(getBattlePassDataRequest, OnBattlePassUserDataRecieved, OnRecievingBattlePassUserDataFailed);
    }

    private void OnBattlePassUserDataRecieved(GetUserDataResult result)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerBattlePassData))
        {
            string jsonUserData = result.Data[PlayfabDataKeys.PlayerBattlePassData].Value;
            BattlePassUserData userData = JsonUtility.FromJson<BattlePassUserData>(jsonUserData);

            if (userData != null)
            {
                battlePassUserData = userData;
                tempUserData = userData;
                OnPlayerDataUpdated?.Invoke();
            }
            else
            {
                Debug.LogError("Error while getting Battle Pass User Data, setting default battle pass user data.\nJson Data That Failed To Convert : \n" + jsonUserData);
                SetDefaultBattlePassUserData();
            }
        }
        else
        {
            SetDefaultBattlePassUserData();
        }
    }

    private void OnRecievingBattlePassUserDataFailed(PlayFabError error)
    {
        Debug.LogError($"OnRecievingBattlePassDataFailed Error :\nError Message : {error.ErrorMessage}\nError Report : {error.GenerateErrorReport()}");
    }

    private void SetDefaultBattlePassUserData()
    {
        string jsonData = JsonUtility.ToJson(battlePassUserData);
        var setBattlePassUserDataRequest = new UpdateUserDataRequest { Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerBattlePassData, jsonData } } };
        PlayFabClientAPI.UpdateUserData(setBattlePassUserDataRequest, (result) => { }, OnUpdatingBattlePassUserDataFailed);
    }

    private void OnUpdatingBattlePassUserDataFailed(PlayFabError error)
    {
        Debug.LogError($"OnUpdatingBattlePassDataFailed Error :\nError Message : {error.ErrorMessage}\nError Report : {error.GenerateErrorReport()}");
    }

    #endregion

    #region Update Player Battle Pass Data

    private void IncreasePlayerBattlePassLevel()
    {
        tempUserData = battlePassUserData;
        tempUserData.playerLevel++;
        UpdateBattlePassUserData(tempUserData);
    }

    public void UpdateCollectedRewardsList(int levelCollected)
    {
        tempUserData = battlePassUserData;
        tempUserData.collectedLevels.Add(levelCollected);

        UpdateBattlePassUserData(tempUserData);
    }

    private void UpdateBattlePassUserData(BattlePassUserData data)
    {
        string json = JsonUtility.ToJson(data);
        var updateBattlePassUserDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { PlayfabDataKeys.PlayerBattlePassData, json }
            }
        };
        PlayFabClientAPI.UpdateUserData(updateBattlePassUserDataRequest, OnBattlePassUserDataUpdated, OnBattlePassUserDataUpdateFailed);
    }

    private void OnBattlePassUserDataUpdated(UpdateUserDataResult result)
    {
        battlePassUserData = tempUserData;
        OnPlayerDataUpdated?.Invoke();
        Debug.Log("Player Battle Pass Player Level Data Successfully Updated");
    }

    private void OnBattlePassUserDataUpdateFailed(PlayFabError error)
    {
        Debug.LogError($"OnBattlePassUserDataUpdateFailed Error : \nError Message : {error.ErrorMessage}\nError Details Report : {error.GenerateErrorReport()}");
    }

    #endregion

    #region Helper Functions

    public void CheckAndRenewBattlePass()
    {
        List<Reward> rewardItems = null;

        if (uiManager.season.endYear == battlePassUserData.endYear && uiManager.season.endMonth == battlePassUserData.endMonth && uiManager.season.endDate == battlePassUserData.endDate)
            return;

        foreach(var season in battlePassData.seasons)
        {
            if(season.endDate == battlePassUserData.endDate && season.endMonth == battlePassUserData.endMonth && season.endYear == battlePassUserData.endYear)
            {
                rewardItems = season.rewards;
                break;
            }
        }

        if (rewardItems != null)
        {
            // Collect All Uncollected Items
            foreach (var reward in rewardItems)
            {
                if (!battlePassUserData.collectedLevels.Contains(reward.level) && battlePassUserData.playerLevel >= reward.level)
                {
                    InventoryManager.manager.GrantItems(reward.rewardID.ToArray(), (success) =>
                     {
                         if (success)
                         {
                             Debug.Log("Uncollected BattlePass Rewards Granted");
                         }
                         else
                         {
                             string rewardIDs = "";
                             foreach (var id in reward.rewardID)
                                 rewardIDs += id + "\n";

                             Debug.LogError("Error granting uncollected reward items. Reward IDs : " + rewardIDs);
                         }
                     });
                }
            }
        }
        else
        {
            Debug.LogError("No Season found with date : " + battlePassUserData.endYear + "/" + battlePassUserData.endMonth + "/" + battlePassUserData.endDate);
        }

        tempUserData = battlePassUserData;
        tempUserData.playerLevel = 0;
        tempUserData.collectedLevels = new();
        tempUserData.endDate = uiManager.season.endDate;
        tempUserData.endMonth = uiManager.season.endMonth;
        tempUserData.endYear = uiManager.season.endYear;
        UpdateBattlePassUserData(tempUserData);
    }

    #endregion
}

[Serializable]
public class BattlePassUserData
{
    public int playerLevel;
    public List<int> collectedLevels;

    public int endYear;
    public int endMonth;
    public int endDate;

    public BattlePassUserData()
    {
        playerLevel = 0;
        collectedLevels = new();
        endDate = 0;
        endMonth = 0;
        endYear = 0;
    }
}