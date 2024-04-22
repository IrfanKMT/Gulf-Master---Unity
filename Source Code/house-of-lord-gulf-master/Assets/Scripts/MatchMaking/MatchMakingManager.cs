using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using System;
using Mirror;
using Unity.RemoteConfig;

public class MatchMakingManager : MonoBehaviour
{
    public static MatchMakingManager manager;
    private Coroutine checkMatchmakingCoroutine;

    [Tooltip("Its the ID from the playfab of the build server")]
    public string buildServerID;

    public event Action OnMatchMakingStarted;

    public string curruntmatchid;
    [Space]
    public MatchType matchType;
    public TeamType MyteamType;

    public struct userAttributes { }
    public struct appAttributes { }

    [HideInInspector]
    public string qName;

    #region Unity Functions

    [Obsolete]
    private void Awake()
    {
        manager = this;

        ConfigManager.FetchCompleted += ConfigManager_FetchCompleted;
        ConfigManager.FetchConfigs(new userAttributes(), new appAttributes());
    }


    [Obsolete]
    private void ConfigManager_FetchCompleted(ConfigResponse obj)
    {
        //buildServerID = ConfigManager.appConfig.GetString("buildID");
        Debug.Log("build server id" + ConfigManager.appConfig.GetString("buildID"));
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += RemovePlayerFromMatchmakingQueue;
    }

    private void OnApplicationQuit()
    {
        RemovePlayerFromMatchmakingQueue();
    }

#if !UNITY_EDITOR

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
            RemovePlayerFromMatchmakingQueue();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            RemovePlayerFromMatchmakingQueue();
    }

#endif

    #endregion

    #region MatchMaking

    public void StartMatchMaking()
    {
        if (!AuthenticationManager.manager.LoggedIn)
        {
            Debug.LogError("MatchMakingManager Error: You can only enter a match when you are logged in!");
            return;
        }

        if (NetworkClient.isConnected)
        {
            Debug.LogError("MatchMakingManager Error: NetworkClient is already connected...\nDisconnecting it.");
            NetworkClient.Disconnect();
        }


        switch (matchType)
        {
            case MatchType.TwoPlayer:
                MatchMakingUIManager.manager.mode2PlayersPanel.SetActive(true);
                MatchMakingUIManager.manager.mode4PlayersPanel.SetActive(false);
                qName = "MatchMaking";
                break;
            case MatchType.FourPlayer:
                MatchMakingUIManager.manager.mode2PlayersPanel.SetActive(false);
                MatchMakingUIManager.manager.mode4PlayersPanel.SetActive(true);
                qName = "MatchMaking_1";
                break;
        }

        var createMatchmakingTicketRequest = new CreateMatchmakingTicketRequest
        {
            Creator = new MatchmakingPlayer
            {
                Entity = new EntityKey { Id = AuthenticationManager.manager.entityID, Type = "title_player_account" },
                Attributes = new MatchmakingPlayerAttributes { DataObject = new { Country = (Countries)PlayerData.Country } }
            },
            QueueName = qName,
            GiveUpAfterSeconds = 300
        };

        Debug.Log(createMatchmakingTicketRequest.Creator.Attributes.DataObject);
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(createMatchmakingTicketRequest, OnCreateMatchmakingTicketSuccess, PlayFabErrorLog);

        OnMatchMakingStarted?.Invoke();
    }

    private void OnCreateMatchmakingTicketSuccess(CreateMatchmakingTicketResult result)
    {
        Debug.Log("Matchmaking ticket created. TicketId: " + result.TicketId);
        //Debug.Log("Matchmaking ticket created. TicketId: " + result.CustomData.ToString());
        checkMatchmakingCoroutine = StartCoroutine(CheckMatchmakingTicketStatusEveryTenSeconds(result.TicketId));
    }

    private IEnumerator CheckMatchmakingTicketStatusEveryTenSeconds(string ticketId)
    {
        while (true)
        {
            CheckMatchmakingTicketStatus(ticketId);
            yield return new WaitForSeconds(6);
        }
    }

    private void CheckMatchmakingTicketStatus(string ticketId)
    {

        switch (matchType)
        {
            case MatchType.TwoPlayer:
                qName = "MatchMaking";
                break;
            case MatchType.FourPlayer:
                qName = "MatchMaking_1";
                break;
        }

        var getMatchmakingTicketRequest = new GetMatchmakingTicketRequest
        {
            TicketId = ticketId,
            QueueName = qName
        };

        //Debug.Log(getMatchmakingTicketRequest.TicketId);
        //Debug.Log(getMatchmakingTicketRequest.QueueName);
        PlayFabMultiplayerAPI.GetMatchmakingTicket(getMatchmakingTicketRequest, OnGetMatchmakingTicketSuccess, PlayFabErrorLog);
    }


    private void OnGetMatchmakingTicketSuccess(GetMatchmakingTicketResult result)
    {
        Debug.Log("OnGetMatchmakingTicketSuccess " + result.Status);

        //Debug.Log("Four Player Found");
        if (result.Status == "Matched")
        {
            GetMatchRequest matchRequest = new GetMatchRequest
            {
                MatchId = result.MatchId,
                QueueName = qName
            };

            PlayFabMultiplayerAPI.GetMatch(matchRequest, CheckTeamAssignment, PlayFabErrorLog);

            //Debug.Log("Match found. MatchId: " + result.MatchId);
            //Debug.Log("Number of players in the match: " + result.); // Log the number of players
            // Now you can enter the match
            EnterMatch(result.MatchId);
            curruntmatchid = result.MatchId;
            if (checkMatchmakingCoroutine != null)
            {
                StopCoroutine(checkMatchmakingCoroutine);
                checkMatchmakingCoroutine = null;
            }
        }
    }

    public string GetTeamID()
    {
        Debug.Log("<color=red> Getting Team ID from Here Do Some Magic and Get Team");
        Debug.Log("<color=R=red> Entity ID will Help Here");
        return null;
    }

    private void CheckTeamAssignment(GetMatchResult result)
    {
        if (result == null)
        {
            Debug.Log("Result is null");
            return;
        }

        foreach (MatchmakingPlayerWithTeamAssignment item in result.Members)
        {
            //Debug.Log(item.Entity.Id);
            //Debug.Log(AuthenticationManager.manager.entityID);
            if(AuthenticationManager.manager.entityID == item.Entity.Id)
            {
                GameNetworkManager.manager.myTeam = item.TeamId;
            }
        }
    }

    private void EnterMatch(string SessionId)
    {
        //Debug.Log("Build ID" + buildServerID);
        RequestMultiplayerServerRequest requestData = new()
        {
            BuildId = buildServerID,
            SessionId = SessionId,
            PreferredRegions = new List<string> { "UAENorth" }
        };

        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, (res) => OnRequestMultiplayerServer(res), PlayFabErrorLog);
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        if (response == null)
        {
            Debug.Log("responce is null");
            return;
        }
        //Wait here for Four Players
        Debug.Log(response.BuildId);
        Debug.Log(response.ServerId);

        Debug.Log($"<color=green>Matchmaking Successful, Connecting To Server : " + response.ServerId + "</color>");

        Debug.Log("Port Now " + response.Ports[0].Num);

        GameNetworkManager.manager.networkAddress = response.IPV4Address;
        GameNetworkManager.manager.GetComponent<kcp2k.KcpTransport>().Port = (ushort)response.Ports[0].Num;
        GameNetworkManager.manager.StartClient();
        RemovePlayerFromMatchmakingQueue();
    }

    #endregion

    #region Removing Player From MatchMaking

    public void RemovePlayerFromMatchmakingQueue()
    {

        if (checkMatchmakingCoroutine != null)
        {
            StopCoroutine(checkMatchmakingCoroutine);
            checkMatchmakingCoroutine = null;
        }

        Debug.Log("RemovePlayerFromMatchmakingQueue");
        switch (matchType)
        {
            case MatchType.TwoPlayer:
                qName = "MatchMaking";
                break;
            case MatchType.FourPlayer:
                qName = "MatchMaking_1";
                break;
        }

        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // Remove the player from all queues
            var cancelRequest = new CancelAllMatchmakingTicketsForPlayerRequest
            {
                Entity = new EntityKey
                {
                    Id = AuthenticationManager.manager.entityID,
                    Type = "title_player_account"
                },
                QueueName = qName
            };

            PlayFabMultiplayerAPI.CancelAllMatchmakingTicketsForPlayer(cancelRequest, OnCancelSuccess, PlayFabErrorLog);
        }
    }

    private void OnCancelSuccess(CancelAllMatchmakingTicketsForPlayerResult result)
    {
        Debug.Log("MatchMakingManager: All matchmaking tickets for player cancelled successfully");
    }

    private void PlayFabErrorLog(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
        Debug.LogError("MatchMakingManager Error : " + error.ErrorMessage + "\nReport: " + error.GenerateErrorReport());
    }

    #endregion

    #region GetMatchType

    public MatchType GetMatchType()
    {
        return matchType;
    }
    #endregion
}
