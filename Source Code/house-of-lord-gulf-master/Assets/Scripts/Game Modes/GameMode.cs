using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class GameMode : NetworkBehaviour
{
    public static GameMode gameMode;

    #region UI Variables

    [Header("Canvas")]
    [SerializeField] Canvas gameplayCanvas;
    [Header("Emijo Canvas")]
    [SerializeField] Canvas emojiCanvas;

    [Header("End Game Timer Panel")]
    public TMP_Text endGameTimerText;
    public Button endGameButton;
    public GameObject endGamePanel;

    #endregion

    #region Game Data Variables

    public GamePlayerData localPlayer1;
    public GamePlayerData otherPlayer2;
    public GamePlayerData otherPlayer3;
    public GamePlayerData otherPlayer4;

    [SyncVar] public GamePlayerData player1Data;
    [SyncVar] public GamePlayerData player2Data;
    [SyncVar] public GamePlayerData player3Data;
    [SyncVar] public GamePlayerData player4Data;

    #endregion

    #region Unity Functions

    private void Awake()
    {
        gameMode = this;
    }

    private IEnumerator Start()
    {

#if !UNITY_SERVER && !TEST
        //Debug.Log("Testing");
        GameObject.FindGameObjectWithTag("Main Menu").SetActive(false);
        UIAnimationManager.manager.ClosePanelWithoutAnimation(UIManager.manager.matchmakingPanel);
        UIAnimationManager.manager.ClosePanelWithoutAnimation(UIManager.manager.gameOverPanel);
        MatchMakingUIManager.manager.SetMatchmakingDataToDefault();
        GameOverUIManager.manager.InitializeEndGameTimerPanel(this);
        gameplayCanvas.worldCamera = Camera.main;
        emojiCanvas.worldCamera = Camera.main;
#endif
        InitializePlayers();
#if !UNITY_SERVER && !TEST
#endif
        yield return null;
        InitializeManagers();
    }

    #endregion

    #region Initialization

    private void InitializePlayers()
    {
        localPlayer1 = player1Data;
        otherPlayer2 = player2Data;
        otherPlayer3 = player3Data;
        otherPlayer4 = player4Data;


        if (isServer)
            return;

        GamePlayer[] gamePlayers = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);

        List<GamePlayerData> data = new List<GamePlayerData>();
        data.Add(player1Data);
        data.Add(player2Data);
        data.Add(player3Data);
        data.Add(player4Data);

        foreach (var item in data)
        {
            foreach (var p in gamePlayers)
            {
                if (item.playfabID == p.playfabID)
                {
                    p.team1p1 = item.team1p1;
                    p.team1p2 = item.team1p2;
                    p.team2p1 = item.team2p1;
                    p.team2p2 = item.team2p2;
                }
            }
        }

        if (gamePlayers.Length > 0)
        {
            switch (MatchMakingManager.manager.matchType)
            {
                case MatchType.TwoPlayer:
                    GamePlayer gamePlayer = gamePlayers[0];

                    if (gamePlayer.isLocalPlayer)
                    {
                        if (player1Data.playfabID == gamePlayer.playfabID)
                        {
                            localPlayer1 = player1Data;
                            otherPlayer2 = player2Data;
                        }
                        else
                        {
                            localPlayer1 = player2Data;
                            otherPlayer2 = player1Data;
                        }
                    }
                    else
                    {
                        if (player1Data.playfabID != gamePlayer.playfabID)
                        {
                            localPlayer1 = player1Data;
                            otherPlayer2 = player2Data;
                        }
                        else
                        {
                            localPlayer1 = player2Data;
                            otherPlayer2 = player1Data;
                        }
                    }
                    break;

                case MatchType.FourPlayer:

                    localPlayer1 = player1Data;
                    otherPlayer2 = player3Data;
                    otherPlayer3 = player2Data;
                    otherPlayer4 = player4Data;

                    GamePlayManager.manager.player1PlayfaID = localPlayer1.playfabID;
                    GamePlayManager.manager.player2PlayfaID = otherPlayer2.playfabID;
                    GamePlayManager.manager.player3PlayfaID = otherPlayer3.playfabID;
                    GamePlayManager.manager.player4PlayfaID = otherPlayer4.playfabID;
                    StartCoroutine(GameplayUIManager.manager.SwapPlayers());
                    break;
            }

        }
        else
        {
            Debug.LogError("GameMode: No Players Found When Initializing Game");
        }
    }

    private void InitializeManagers()
    {
        GameplayUIManager.manager.InitializePlayers(localPlayer1.playfabID, otherPlayer2.playfabID, otherPlayer3.playfabID, otherPlayer4.playfabID);
        PerksManager.manager.Initialize(localPlayer1.perk1ID, localPlayer1.perk2ID,
            otherPlayer2.perk1ID, otherPlayer2.perk2ID,
            otherPlayer3.perk1ID, otherPlayer3.perk2ID,
            otherPlayer4.perk1ID, otherPlayer4.perk2ID);
        BoosterManager.manager.Initialize(player1Data.boosterID, player2Data.boosterID, player3Data.boosterID, player4Data.boosterID);
    }
    #endregion


    public string GetMyPartnerID(string pid, TeamType t)
    {
        string s = "";

        if (t == TeamType.TeamA)
        {
            s = "team1";
        }
        else
        {
            s = "team2";
        }

        if (pid != player1Data.playfabID && player1Data.teamName == s)
        {
            return player1Data.playfabID;
        }

        if (pid != player2Data.playfabID && player2Data.teamName == s)
        {
            return player2Data.playfabID;
        }

        if (pid != player3Data.playfabID && player3Data.teamName == s)
        {
            return player3Data.playfabID;
        }

        if (pid != player4Data.playfabID && player4Data.teamName == s)
        {
            return player4Data.playfabID;
        }

        return null;
    }


    public TeamType GetMyTeam(string pid)
    {
        if (pid == player1Data.playfabID)
        {
            if (player1Data.teamName == "team1")
            {
                //Debug.Log("Returned Team A");
                return TeamType.TeamA;
            }
            else
            {
                //Debug.Log("Returned Team B");
                return TeamType.TeamB;
            }
        }

        if (pid == player2Data.playfabID)
        {
            if (player2Data.teamName == "team1")
            {
                //Debug.Log("Returned Team A");
                return TeamType.TeamA;
            }
            else
            {
                //Debug.Log("Returned Team B");
                return TeamType.TeamB;
            }
        }

        if (pid == player3Data.playfabID)
        {
            if (player3Data.teamName == "team1")
            {
                //Debug.Log("Returned Team A");
                return TeamType.TeamA;
            }
            else
            {
                //Debug.Log("Returned Team B");
                return TeamType.TeamB;
            }
        }

        if (pid == player4Data.playfabID)
        {
            if (player4Data.teamName == "team1")
            {
                //Debug.Log("Returned Team A");
                return TeamType.TeamA;
            }
            else
            {
                //Debug.Log("Returned Team B");
                return TeamType.TeamB;
            }
        }
        return TeamType.None;
    }
}

public struct GamePlayerData
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