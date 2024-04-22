using System;
using Mirror;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using UnityEngine.Events;
using PlayFab;

// This script on server will quit the app after 10min automatically if no players are connected
public class GameNetworkManager : NetworkManager
{
    public static GameNetworkManager manager;

    #region Events

    public event Action OnClientConnected;
    public event Action OnClientDisconnected;

    #endregion

    #region Variables

    [Header("Custom Variables")]
    [SerializeField] internal GameObject mainMenu;
    [SerializeField] private GameMode[] gameModes;
    [SerializeField] internal bool isLocal = false;

    internal List<GamePlayerMessage> gamePlayersData = new();
    [Space]

    internal int randomLeaderboardPlayerGender; //Enum Gender is of base type int
    internal int randomLeaderboardPlayerIndex;

    public string myTeam;

    #endregion

    #region Unity Functions

#if !UNITY_SERVER
    public override void Awake()
    {
        base.Awake();
        manager = this;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
 Debug.unityLogger.logEnabled = false;
#endif
    }
#endif

    #endregion


    #region Disconnecting

    public void LeaveGame(bool disconnectClient = true)
    {
        if (NetworkClient.isConnected && disconnectClient)
            StopClient();
#if ENABLE_VIVOX 
        VivoxManager.manager.LeaveChannel();
#endif
        mainMenu.SetActive(true);
        UIManager.manager.matchmakingPanel.SetActive(false);
        UIManager.manager.gameOverPanel.SetActive(false);
    }

    #endregion

    #region Client Callbacks

    public override void OnClientConnect()
    {
        Debug.Log("OnClientConnect Game NetworkManager");
        base.OnClientConnect();
        OnClientConnected?.Invoke();

        GamePlayerMessage characterMessage = new()
        {
            playfabID = PlayerData.PlayfabID,
            boosterID = PlayerData.BoosterID,
            perk1ID = PlayerData.Perk1ID,
            perk2ID = PlayerData.Perk2ID,
            teamName = myTeam
        };
        NetworkClient.Send(characterMessage);
    }


    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnClientDisconnected?.Invoke();
    }

    #endregion

    #region Server Callbacks

    public override void OnStartServer()
    {
        Debug.Log("OnStartServer RegisterHandler");
        base.OnStartServer();
        NetworkServer.RegisterHandler<GamePlayerMessage>(ServerMessage_OnCreateCharacter);
    }

    #endregion

    #region OnServer - Player Creation

    [Server]
    private void ServerMessage_OnCreateCharacter(NetworkConnectionToClient conn, GamePlayerMessage message)
    {
        Debug.Log("ServerMessage_OnCreateCharacter");

        if (gamePlayersData.Count == maxConnections)
        {
            if (gamePlayersData.Where(i => i.playfabID == message.playfabID).Count() == 1) //There is 1 user that has joined with this playfab ID
            {
                Debug.Log("A user is reconnecting : " + message.playfabID);
                GamePlayerMessage reconnectedUserPlayfabID = gamePlayersData.Where(i => i.playfabID == message.playfabID).First();
                SpawnPlayerGameObject(conn, reconnectedUserPlayfabID, gamePlayersData.IndexOf(reconnectedUserPlayfabID) == 0, true);
            }
            else
            {
                Debug.Log("Disconnected From Here");
                conn.Disconnect();
            }
            return;
        }

        SpawnPlayerGameObject(conn, message, gamePlayersData.Count == 0);

        if (gamePlayersData.Where(i => i.playfabID == message.playfabID).Count() == 0) //There is no user with this playfab ID
        {
            gamePlayersData.Add(message);

            Debug.Log("numPlayers  " + numPlayers);

            if (numPlayers == maxConnections)
            {
                StartCoroutine(SpawnGameMode());
            }
        }
    }

    IEnumerator SpawnGameMode()
    {
        Debug.Log("Spawn GameMode");
        //Get this from MatchMakingUIManager
        yield return new WaitForSeconds(5 + 1 + 2);
        GameObject spawnedGameMode = Instantiate(gameModes[UnityEngine.Random.Range(0, gameModes.Length)].gameObject);

        GameMode gameMode = spawnedGameMode.GetComponent<GameMode>();

        Debug.Log("GamePlayersData - " + gamePlayersData.Count);

        int count = gamePlayersData.Count;

        Debug.Log("Total Player Count" + count);

        if (count > 2)
        {
            List<GamePlayerMessage> team1 = new List<GamePlayerMessage>();
            List<GamePlayerMessage> team2 = new List<GamePlayerMessage>();
            List<string> addedteam1 = new List<string>();
            List<string> addedteam2 = new List<string>();

            foreach (GamePlayerMessage item in gamePlayersData)
            {
                if (item.teamName == "team1" && !addedteam1.Contains(item.playfabID))
                {
                    team1.Add(item);
                    addedteam1.Add(item.playfabID);
                }
                else if (item.teamName == "team2" && !addedteam2.Contains(item.playfabID))
                {
                    team2.Add(item);
                    addedteam2.Add(item.playfabID);
                }
            }

            Debug.Log("Team 1 count " + team1.Count);
            Debug.Log("Team 2 count " + team2.Count);

            gameMode.player1Data = new() { playfabID = team1[0].playfabID, boosterID = team1[0].boosterID, perk1ID = team1[0].perk1ID, perk2ID = team1[0].perk2ID, teamName = team1[0].teamName,team1p1=true };
            gameMode.player2Data = new() { playfabID = team2[0].playfabID, boosterID = team2[0].boosterID, perk1ID = team2[0].perk1ID, perk2ID = team2[0].perk2ID, teamName = team2[0].teamName ,team2p1= true };
            gameMode.player3Data = new() { playfabID = team1[1].playfabID, boosterID = team1[1].boosterID, perk1ID = team1[1].perk1ID, perk2ID = team1[1].perk2ID, teamName = team1[1].teamName,team1p2=true };
            gameMode.player4Data = new() { playfabID = team2[1].playfabID, boosterID = team2[1].boosterID, perk1ID = team2[1].perk1ID, perk2ID = team2[1].perk2ID, teamName = team2[1].teamName,team2p2= true };

        }
        else
        {
            gameMode.player1Data = new() { playfabID = gamePlayersData[0].playfabID, boosterID = gamePlayersData[0].boosterID, perk1ID = gamePlayersData[0].perk1ID, perk2ID = gamePlayersData[0].perk2ID };
            gameMode.player2Data = new() { playfabID = gamePlayersData[1].playfabID, boosterID = gamePlayersData[1].boosterID, perk1ID = gamePlayersData[1].perk1ID, perk2ID = gamePlayersData[1].perk2ID };
        }

        NetworkServer.Spawn(spawnedGameMode);
    }

    [Server]
    private void SpawnPlayerGameObject(NetworkConnectionToClient conn, GamePlayerMessage msg, bool isPlayer1, bool isReconnecting = false)
    {
        Debug.Log("Is this LocalPlayer of Player 1 " + isPlayer1);

        GameObject gameobject = Instantiate(playerPrefab);

        GamePlayer player = gameobject.GetComponent<GamePlayer>();
        player.playfabID = msg.playfabID;
        player.boosterID = msg.boosterID;
        player.perk1ID = msg.perk1ID;
        player.perk2ID = msg.perk2ID;
        player.isPlayer1 = isPlayer1;
        player.teamName = msg.teamName;
        player.isReconnecting = isReconnecting;
        //player.serverUniqueID = msg.serverUniqueID;   

        switch (player.playfabID)
        {
            case "29730D3131C9208C":
                player.gameObject.name = "mac";
                break;
            case "1C46F5B833D9C261":
                player.gameObject.name = "ksk";
                break;
            case "DD54F7BEF9466894":
                player.gameObject.name = "irfan";
                break;
            case "C023337627289AAD":
                player.gameObject.name = "milan";
                break;
        }
        NetworkServer.AddPlayerForConnection(conn, gameobject);
    }

    #endregion

    #region OnServer - Client Connections

#if UNITY_SERVER

    #region Variables

    public PlayerEvent OnPlayerAdded = new();
    public PlayerEvent OnPlayerRemoved = new();

    public int MaxConnections = 100;
    public int Port = 7777; // overwritten by the code in AgentListener.cs



    public List<UnityNetworkConnection> Connections
    {
        get { return _connections; }
        private set { _connections = value; }
    }

    private List<UnityNetworkConnection> _connections = new();

    public class PlayerEvent : UnityEvent<string> { }

    #endregion

    // Use this for initialization
    public override void Awake()
    {
        base.Awake();
        manager = this;
        Debug.Log("Instance Created");
        NetworkServer.RegisterHandler<ReceiveAuthenticateMessage>(OnReceiveAuthenticate);
    }

    public void StartListen()
    {
        Debug.Log(MaxConnections);

        Debug.Log("Mac COnnection " + MaxConnections + "    ");

        GetComponent<kcp2k.KcpTransport>().port = (ushort)Port;
        NetworkServer.Listen(MaxConnections);
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        NetworkServer.Shutdown();
    }

    IEnumerator QuitServerAfter10Minutes()
    {
        yield return new WaitForSeconds(600);

        if (numPlayers == 0)
        {
            Debug.Log("Server Quit After 10 minutes 1");
            Application.Quit();
        }
        else
        {
            while (true)
            {
                yield return new WaitForSeconds(5);
                Debug.Log("Server Quit After 10 minutes 2");
                Application.Quit();
            }
        }
    }

    private void OnReceiveAuthenticate(NetworkConnection nconn, ReceiveAuthenticateMessage message)
    {
        var conn = _connections.Find(c => c.ConnectionId == nconn.connectionId);
        if (conn != null)
        {
            Debug.Log("OnReceiveAuthenticate  " + conn.PlayFabId);
            conn.PlayFabId = message.PlayFabId;
            conn.IsAuthenticated = true;
            OnPlayerAdded.Invoke(message.PlayFabId);
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.LogWarning("Client Connected");
        var uconn = _connections.Find(c => c.ConnectionId == conn.connectionId);
        if (uconn == null)
        {
            _connections.Add(new UnityNetworkConnection()
            {
                Connection = conn,
                ConnectionId = conn.connectionId,
                LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId
            });
        }
    }

    public override void OnServerError(NetworkConnectionToClient conn, TransportError error, string reason)
    {
        //base.OnServerError(conn, ex);

        Debug.Log(string.Format("Unity Network Connection Status: exception - {0}", error));
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        Debug.Log("OnServerDisconnect");

        var uconn = _connections.Find(c => c.ConnectionId == conn.connectionId);
        if (uconn != null)
        {
            if (!string.IsNullOrEmpty(uconn.PlayFabId))
            {
                OnPlayerRemoved.Invoke(uconn.PlayFabId);
            }
            _connections.Remove(uconn);
        }
    }
#endif

    #endregion
}

public struct GamePlayerMessage : NetworkMessage
{
    public string playfabID;
    public string boosterID;
    public string perk1ID;
    public string perk2ID;
    public string teamName;
    public bool team1p1;
    public bool team1p2;
    public bool team2p1;
    public bool team2p2;
}

#region Extra Server Classes

[Serializable]
public class UnityNetworkConnection
{
    public bool IsAuthenticated;
    public string PlayFabId;
    public string LobbyId;
    public int ConnectionId;
    public NetworkConnection Connection;
}

public class CustomGameServerMessageTypes
{
    public const short ReceiveAuthenticate = 900;
    public const short ShutdownMessage = 901;
    public const short MaintenanceMessage = 902;
}

public struct ReceiveAuthenticateMessage : NetworkMessage
{
    public string PlayFabId;
}

public struct ShutdownMessage : NetworkMessage { }

[Serializable]
public struct MaintenanceMessage : NetworkMessage
{
    public DateTime ScheduledMaintenanceUTC;
}

#endregion