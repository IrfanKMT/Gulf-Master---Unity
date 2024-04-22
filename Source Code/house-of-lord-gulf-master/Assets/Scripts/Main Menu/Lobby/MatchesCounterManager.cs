using System;
using PlayFab;
using UnityEngine;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;

public class MatchesCounterManager : MonoBehaviour
{
    public static MatchesCounterManager manager;

    // used by leaderboard manager to increase leaderboard score of local player
    public event Action OnMatchWonDataAdded;

    [SerializeField] GameObject[] redBlocks;
    [SerializeField] GameObject[] greenBlocks;

    List<string> matchesInfo;

    private const int MaxMatches = 20;
    private const string WonValue = "WON";
    private const string LostValue = "LOST";

    bool initialized = false;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += () => Initialize();
        GameMatchEventManager.OnWonGame += ()=> AddMatchData(true);
        GameMatchEventManager.OnLostGame += () => AddMatchData(false);
    }

    private void Initialize(bool addWonData = false, bool won = false)
    {
        var getMatchCounterDataRequest = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.PlayerMatchCounterData }
        };
        PlayFabClientAPI.GetUserData(getMatchCounterDataRequest, res =>
        {
            if (res.Data.ContainsKey(PlayfabDataKeys.PlayerMatchCounterData))
            {
                string jsonData = res.Data[PlayfabDataKeys.PlayerMatchCounterData].Value;
                matchesInfo = JsonConvert.DeserializeObject<List<string>>(jsonData);
            }
            else
            {
                matchesInfo = new();
            }

            SetupMatchCounterUI();
            initialized = true;

            if (addWonData)
                AddMatchData(won);

        },
        err => Debug.LogError("MatchCounterManager : Error While Getting User Data From Playfab.\nError Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));
    }

    private void SetupMatchCounterUI()
    {
        for (int i = 0; i < MaxMatches; i++)
        {
            redBlocks[i].SetActive(false);
            greenBlocks[i].SetActive(false);
        }

        for (int i = 0; i < matchesInfo.Count; i++)
        {
            string item = matchesInfo[i];

            if (item.Equals(WonValue))
                greenBlocks[i].SetActive(true);
            else if (item.Equals(LostValue))
                redBlocks[i].SetActive(true);
        }
    }

    public void AddMatchData(bool won)
    {
        StartCoroutine(Coroutine_AddMatchData(won));
    }

    IEnumerator Coroutine_AddMatchData(bool won)
    {
        yield return new WaitWhile(()=>!initialized);
        if (matchesInfo.Count < MaxMatches)
        {
            if (won) OnMatchWonDataAdded?.Invoke();

            matchesInfo.Add(won ? WonValue : LostValue);
            UpdatePlayfabData();
            SetupMatchCounterUI();
        }
        else
            Initialize(true, won);
    }

    private void UpdatePlayfabData()
    {
        var userDataRequest = new UpdateUserDataRequest {Data = new Dictionary<string, string> {{PlayfabDataKeys.PlayerMatchCounterData, JsonConvert.SerializeObject(matchesInfo)}}};

        PlayFabClientAPI.UpdateUserData(userDataRequest, res => Debug.Log("User Match Counter Data Successfully Updated!"),
        err => Debug.LogError("MatchCounterManager : Error While Setting User Data To Playfab.\nError Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));
    }
}
