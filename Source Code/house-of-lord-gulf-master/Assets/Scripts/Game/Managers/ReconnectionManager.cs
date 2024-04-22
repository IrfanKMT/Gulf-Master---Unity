using UnityEngine;
using kcp2k;
using Newtonsoft.Json;
using System;
using Mirror;

public class ReconnectionManager : MonoBehaviour
{
    public static ReconnectionManager manager;
    private const string playerPrefSavedKey = "ReconnectionManager_SavedData";

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        GameNetworkManager.manager.OnClientConnected += OnClientConnected;

        AuthenticationManager.manager.OnPlayerLoggedIn += OnUserLoggedIn;

        GameMatchEventManager.OnWonGame += OnGameEnded;
        GameMatchEventManager.OnLostGame += OnGameEnded;
    }

    private void OnClientConnected()
    {
        ReconnectionData data = new()
        {
            networkAddr = GameNetworkManager.manager.networkAddress,
            port = GameNetworkManager.manager.GetComponent<KcpTransport>().port,
            startTime = DateTime.UtcNow.ToString(),
            matchType = MatchMakingManager.manager.matchType.ToString()
            //serverUniqueID = GameNetworkManager.manager.serverID
        };

        string jsonData = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(playerPrefSavedKey, jsonData);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            GameNetworkManager.manager.StopClient();
            GameNetworkManager.manager.mainMenu.SetActive(true);
            UIManager.manager.matchmakingPanel.SetActive(false);
            UIManager.manager.gameOverPanel.SetActive(false);
        }
        else if (AuthenticationManager.manager.LoggedIn && !NetworkClient.isConnected)
        {
            Reconnect();
        }
    }

    private void OnGameEnded()
    {
        PlayerPrefs.DeleteKey(playerPrefSavedKey);
    }

    private void OnUserLoggedIn()
    {
        Reconnect();
        PlayerPrefs.DeleteKey(playerPrefSavedKey);
    }

    public void Reconnect()
    {
        if (PlayerPrefs.HasKey(playerPrefSavedKey))
        {
            Debug.Log("Found Reconnection Data");
            string jsonData = PlayerPrefs.GetString(playerPrefSavedKey);
            ReconnectionData data = JsonConvert.DeserializeObject<ReconnectionData>(jsonData);

            DateTime startTime = DateTime.Parse(data.startTime);
            TimeSpan timePassed = startTime - DateTime.UtcNow;
            Debug.Log("MatchType"+data.matchType);
            if(data.matchType=="FourPlayer")
            {
                MatchMakingManager.manager.matchType = MatchType.FourPlayer;
            }
            else
            {
                MatchMakingManager.manager.matchType = MatchType.TwoPlayer;
            }
            //MatchMakingManager.manager.matchType = MatchType.FourPlayer;
            Debug.Log("Time Passed Since Game Left : " + Mathf.Round(Mathf.Abs((float)timePassed.TotalSeconds)) + " sec");
            Debug.Log(timePassed.TotalMinutes < 10 ? "Reconnecting" : "Not Reconnecting");

            if (timePassed.TotalMinutes < 10)
            {
                GameNetworkManager.manager.networkAddress = data.networkAddr;
                GameNetworkManager.manager.GetComponent<KcpTransport>().port = data.port;

                GameNetworkManager.manager.StartClient();
            }
            else
            {
                PlayerPrefs.DeleteKey(playerPrefSavedKey);
            }
        }
    }
}

struct ReconnectionData
{
    public string networkAddr;
    public ushort port;
    public string startTime;
    public string serverUniqueID;
    public string matchType;
         
}