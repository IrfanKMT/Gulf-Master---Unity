//using Photon.Pun;
using System.Linq;
using System.Collections;
using UnityEngine;
//using Photon.Realtime;
//using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections.Generic;
using PlayFab.ClientModels;

public class OldNetworkManager : MonoBehaviour
//public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static OldNetworkManager manager;

    //public event System.Action OnPlayerJoinRoom;
    //public event System.Action OnPlayerLeaveRoom;
    ////public event System.Action<DisconnectCause> OnPlayerDisconnected;

    //public event System.Action OnPlayerJoinedLobby;
    //public event System.Action<int> OnPlayerJoinRoomFailed; // return code of error

    //public event System.Action<Player, Player> OnOpponentFound; // Localplayer and Other Player

    //public event System.Action<Player> OnMasterClientChanged;


    //bool canStartGame = false;

    public const string Region = "C0";
    public const string VoiceMode = "C1";
    //public readonly TypedLobby sqlLobby = new("customSqlLobby", LobbyType.SqlLobby);

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += ConnectPlayer;
    }

    #endregion

    #region Matchmaking, Joining And Leaving Rooms

    public void ConnectPlayer()
    {
        //if (PhotonNetwork.IsConnectedAndReady) return;
        //if (!AuthenticationManager.manager.LoggedIn) return;

        //PhotonNetwork.AutomaticallySyncScene = true;
        //PhotonNetwork.NickName = PlayerData.PlayfabID;
        //Debug.Log("Photon Nickname : " + PhotonNetwork.NickName);

        //PhotonNetwork.ConnectUsingSettings();
    }

    public void JoinOrCreateRoom(string boosterID, string perk1ID, string perk2ID)
    {
        //if (!PhotonNetwork.IsConnectedAndReady) return;

        #region Player Properties

        Hashtable playerHashtable = new()
        {
            { "BoosterID", boosterID },
            { "Perk1ID", perk1ID },
            { "Perk2ID", perk2ID }
        };
        //PhotonNetwork.LocalPlayer.SetCustomProperties(playerHashtable);

        #endregion

        #region Room Properties

        int randomSeed = Random.Range(0, 99999);

        List<PlayerLeaderboardEntry> players = PlayerData.Gender == 1 ? LeaderboardsUIManager.manager.maleLeaderboards[(Countries)PlayerData.Country] : LeaderboardsUIManager.manager.femaleLeaderboards[(Countries)PlayerData.Country];
        if (players.Count > 10) players = players.GetRange(0, 10);

        int randomLeaderboardPlayer = Random.Range(0, players.Count);

        Hashtable roomCustomProperties = new()
        {
            { "SEED", randomSeed },
            { "IsGameStarted", false },
            { "IsMasterPlayerTurn", true },
            { "RandomPlayerGenderFromTop20Players", PlayerData.Gender },
            { "RandomPlayerIndexFromTop20Players", randomLeaderboardPlayer },
            { Region, PlayerData.Country },
            { VoiceMode, LobbyUIManager.manager.isVoiceOn }
        };

        //RoomOptions options = new()
        //{
        //    MaxPlayers = 2,
        //    BroadcastPropsChangeToAll = true,
        //    CustomRoomProperties = roomCustomProperties,
        //    CustomRoomPropertiesForLobby = new string[] { Region, VoiceMode },
        //    IsVisible = true,
        //    IsOpen = true,
        //    PlayerTtl = 900000,
        //    EmptyRoomTtl = 0,
        //    CleanupCacheOnLeave = false
        //};

        #endregion

        //string lobbyFilter = Region + " = " + PlayerData.Country + " AND " + VoiceMode + " = " + LobbyUIManager.manager.isVoiceOn + " ; " + VoiceMode + " = " + LobbyUIManager.manager.isVoiceOn;
        //PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: options, sqlLobbyFilter:lobbyFilter, typedLobby: sqlLobby);
    }

    /// <summary>
    /// Called From MatchmakingUIManager.cs, called after countdown timer has been finished.
    /// </summary>
    public void StartGame()
    {
        //if (canStartGame && !(bool)PhotonNetwork.CurrentRoom.CustomProperties["IsGameStarted"])
        //{
        //    print("Network Manager: Start Game");
        //    photonView.RPC(nameof(RPC_StartGame), RpcTarget.All, Random.Range(0, GameManager.manager.gameModes.Length));
        //}
    }

    public void LeaveRoom(string caller)
    {
        //Debug.Log("Leaving Photon Room: " + caller);
        //PhotonNetwork.LeaveRoom();
        //PhotonNetwork.SendAllOutgoingCommands();
        //GameManager.manager.InitializeMainMenuUI();
        //OnPlayerLeaveRoom?.Invoke();
        //canStartGame = false;
    }

    #endregion

    #region Helper Functions

    public bool IsInLobby()
    {
        return true;
        //return PhotonNetwork.InLobby;
    }

    #endregion

    #region Pun Callbacks

    //public override void OnConnectedToMaster()
    //{
    //    PhotonNetwork.JoinLobby(sqlLobby);
    //}

    //public override void OnJoinedLobby()
    //{
    //    print("Joined Lobby");
    //    OnPlayerJoinedLobby?.Invoke();
    //    canStartGame = false;
    //}

    //public override void OnJoinedRoom()
    //{
    //    canStartGame = PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers && PhotonNetwork.IsMasterClient;
    //    print("Joined Room:\nRoom Name : " + PhotonNetwork.CurrentRoom.Name);
    //    OnPlayerJoinRoom?.Invoke();

    //    if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
    //    {
    //        print("Room Full, Starting Match");
    //        Player otherPlayer = PhotonNetwork.PlayerListOthers.First();
    //        OnOpponentFound?.Invoke(PhotonNetwork.LocalPlayer, otherPlayer);
    //    }
    //}

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{
    //    Debug.Log("A new player joined the room : " + newPlayer.NickName);

    //    if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
    //    {
    //        OnOpponentFound?.Invoke(PhotonNetwork.LocalPlayer, newPlayer);
    //        canStartGame = PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers && PhotonNetwork.IsMasterClient;
    //    }
    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    Debug.Log("A player left the room : " + otherPlayer.NickName);
    //}

    //public override void OnJoinRoomFailed(short returnCode, string message)
    //{
    //    canStartGame = false;
    //    Debug.LogError("Joining Room Errored:\nError Message : " + message + "\nError Code : " + returnCode);
    //    OnPlayerJoinRoomFailed?.Invoke(returnCode);
    //}

    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    canStartGame = false;
    //    OnPlayerDisconnected?.Invoke(cause);
    //    Debug.Log("Disconnected From Photon");
    //}

    //public override void OnMasterClientSwitched(Player newMasterClient)
    //{
    //    OnMasterClientChanged?.Invoke(newMasterClient);
    //}

    #endregion

    #region RPCs

    //[PunRPC]
    private void RPC_StartGame(int randLevel)
    {
        print("Network Manager RPC: Start Game");
        //GameManager.manager.InitializeGameUI(randLevel);

        //if (PhotonNetwork.IsMasterClient)
        //{
        //    Hashtable roomProps = new()
        //    {
        //        { "IsGameStarted", true }
        //    };

        //    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        //}
    }

    #endregion
}
