using System;
using PlayFab;
using System.Linq;
using UnityEngine;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class LeaderboardsManager : MonoBehaviour
{
    public static LeaderboardsManager manager;

    public int scoreIncrementPerWin = 5;
    public int maxScorePerDay = 100;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        MatchesCounterManager.manager.OnMatchWonDataAdded += IncreaseLocalPlayerLeaderboardScore;
    }

    #endregion

    #region Set Leaderboard Data

    /// <summary>
    /// Error Is Logged Already
    /// Call Only After Player's Logged In
    /// </summary>
    private void IncreaseLocalPlayerLeaderboardScore()
    {
        string leaderboardName = ((Countries)PlayerData.Country).ToString() + ((PlayerData.Gender==1) ? "_Male" : "_Female");

        var getLocalPlayerScoreRequest = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = leaderboardName,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(getLocalPlayerScoreRequest, res =>
        {
            int playerScore=0;

            if (res.Leaderboard.Count > 0)
                playerScore = res.Leaderboard.First().StatValue;

            if (playerScore < maxScorePerDay)
            {
                var setLeaderboardDataRequest = new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate
                        {
                            StatisticName = leaderboardName,
                            Value = scoreIncrementPerWin
                        }
                    }
                };
                PlayFabClientAPI.UpdatePlayerStatistics(setLeaderboardDataRequest, res => { Debug.Log("Score Added In Leaderboard"); },
                err => Debug.LogError("Error Setting Player Leaderboard Data : \nError : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));
            }
        },
        err => Debug.LogError("Error Setting Player Leaderboard Data : \nError : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));
    }

    #endregion

    #region Get Leaderboard Data

    /// <summary>
    /// Returns yesterday's leaderboard,
    /// Error Is Logged Already
    /// </summary>
    public static void GetLeaderboardData(Countries country, bool getMaleLeaderboard, int numberOfPlayersFromLearboard, Action<GetLeaderboardResult> OnSuccess, Action<PlayFabError> OnError)
    {
        string leaderboardName = country.ToString() + (getMaleLeaderboard ? "_Male" : "_Female");

        var getLeaderboardDataRequest = new GetLeaderboardRequest
        {
            StatisticName = leaderboardName,
            MaxResultsCount = 25
        };

        PlayFabClientAPI.GetLeaderboard(getLeaderboardDataRequest, res =>
        {
            if (res.Version > 0)
            {
                int oldVersion = res.Version - 1;
                var getLeaderboardDataRequest = new GetLeaderboardRequest
                {
                    StatisticName = leaderboardName,
                    MaxResultsCount = numberOfPlayersFromLearboard,
                    StartPosition = 0,
                    Version = oldVersion
                };

                PlayFabClientAPI.GetLeaderboard(getLeaderboardDataRequest, OnSuccess,
                err =>
                {
                    OnError(err);
                    Debug.LogError("Error Getting Player Leaderboard Data : \nError : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport());
                });
            }
            else
            {
                OnSuccess(res);
            }
        },
        err =>
        {
            OnError(err);
            Debug.LogError("Error Getting Player Leaderboard Data : \nError : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport());
        });
    }

    /// <summary>
    /// Call Only After Player's Logged In
    /// Returns null if the local player's leaderboard entry is not found.
    /// Returns data from current leaderboard, not previous one
    /// </summary>
    public static void GetLocalPlayerLeaderboardData(Action<PlayerLeaderboardEntry> OnSuccess)
    {
        string leaderboardName = ((Countries)PlayerData.Country).ToString() + ((PlayerData.Gender == 1) ? "_Male" : "_Female");

        var getLocalPlayerScoreRequest = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = leaderboardName,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(getLocalPlayerScoreRequest, res =>
        {
            if (res.Leaderboard.Count > 0)
                OnSuccess(res.Leaderboard.First());
            else
                OnSuccess(null);
        },
        err => Debug.LogError("Error Setting Player Leaderboard Data : \nError : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));
    }

    #endregion
}
