using System;
using Mirror;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

/// Most Of the Functions In This Script Run On Server
public class GamePlayManager : NetworkBehaviour
{
    public static GamePlayManager manager;

    #region Events

    //Used For UI Events
    public event Action OnGameStarted; //Used For UI
    public event Action<int, int> OnScoresChanged; //Used For UI
    public event Action<int, int> OnScoresMultiplierChanged; //Used For UI
    public event Action<int, int, int, int> OnMoveCounterChanged; //Used For UI
    public event Action<int, int, int, int> OnMoveCounterChangedFourPlayer; //Used For UI

    //Used in UI and Code Events
    public event Action<bool> OnTurnChanged; //Retuns true if is my turn on client | Always returns true on server
    public event Action<int> OnRoundChanged;
    public event Action OnWonGame;
    public event Action OnLostGame;
    public event Action OnGameEnd;
    #endregion

    #region Variables

    [Header("GamePlay Variables")]
    [SerializeField] private int maxMoves = 2;
    [SerializeField] internal int maxRounds = 5;


    public int currntround;

    //Sync Variables
    [SyncVar(hook = nameof(Hook_OnPlayerMovesChanged))] private int player1MovesLeft = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerMovesChanged))] private int player2MovesLeft = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerMovesChanged))] private int player3MovesLeft = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerMovesChanged))] private int player4MovesLeft = 0;

    [SyncVar(hook = nameof(Hook_OnScoresMultiplierChanged))] private int player1ScoreMultiplier = 0;
    [SyncVar(hook = nameof(Hook_OnScoresMultiplierChanged))] private int player2ScoreMultiplier = 0;

    [SyncVar(hook = nameof(Hook_OnRoundsChanged))] private int round = 0;



    [SyncVar(hook = nameof(Hook_OnScoresChanged))] internal int player1Score = 99;
    [SyncVar(hook = nameof(Hook_OnScoresChanged))] internal int player2Score = 99;

    [SyncVar] private bool extendedRound = false; // extended when players have equal score at last round
    [SyncVar] private int extendedRoundNumber = 0;

    [SyncVar(hook = nameof(Hook_InitializeGrid))] private int seed = 0;

    [SyncVar(hook = nameof(Hook_OnTurnChanged))] public string currentTurnPlayfabID; //playfab ID of the user whose turn it is

    [SyncVar(hook = nameof(CurruntPlayerTeamType))] public TeamType currentTurnPlayerteamtype; //playfab ID of the user whose turn it is



    //Private Variables
    public string player1PlayfaID;
    public string player2PlayfaID;
    public string player3PlayfaID;
    public string player4PlayfaID;

    public string p1team;
    public string p2team;
    public string p3team;
    public string p4team;

    public MatchType matchtype = MatchType.None;

    public TeamType serverteamtype;

    //public MatchType LocalmatchType;

    private Grid grid;

    internal GamePlayer LocalPlayer
    {
        get
        {
            if (localplayer == null)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                foreach (var item in players)
                {
                    GamePlayer gamePlayer = item.GetComponent<GamePlayer>();
                    if (gamePlayer.isLocalPlayer)
                    {
                        localplayer = gamePlayer;
                    }
                }
            }
            return localplayer;
        }
        private set { }
    }
    public GamePlayer localplayer;

    internal bool isGameOver = false;

    #endregion

    #region Sync Variables Hooks

    private void Hook_InitializeGrid(int oldSeed, int newSeed)
    {
        Debug.Log("Hook_InitializeGrid");
        grid = Grid.grid;
        grid.Initialize(newSeed, true, true);

        //OnRoundChanged?.Invoke(maxRounds);
        //OnTurnChanged?.Invoke();

        Debug.Log("OnGameStarted Invoke " + currentTurnPlayfabID);
        OnGameStarted?.Invoke();
    }

    private void Hook_OnPlayerMovesChanged(int oldMoves, int newMoves)
    {
        OnMoveCounterChanged?.Invoke(player1MovesLeft, player2MovesLeft, player3MovesLeft, player4MovesLeft);
    }

    private void Hook_OnRoundsChanged(int oldRounds, int newRounds)
    {
        // There can be more than 5 rounds...
        if (newRounds <= 0)
            newRounds = 1;
        //Debug.Log("Round changed invoked " + oldRounds + "  " + newRounds);
        OnRoundChanged?.Invoke(newRounds);
    }

    private void Hook_OnTurnChanged(string oldTurnPlayfabID, string newTurnPlayfabID)
    {
        //Debug.Log("Turn changed - " + newTurnPlayfabID);

        OnTurnChanged?.Invoke(newTurnPlayfabID.Equals(PlayerData.PlayfabID));
    }

    private void CurruntPlayerTeamType(TeamType oldValue, TeamType newValue)
    {
        newValue = GetTeam();
        currentTurnPlayerteamtype = newValue;
    }

    TeamType GetTeam()
    {
        if (localplayer.teamName == "team1")
        {
            return TeamType.TeamA;
        }
        else
        {
            return TeamType.TeamB;
        }
    }


    private void Hook_OnScoresChanged(int oldScore, int newScore)
    {
        OnScoresChanged?.Invoke(player1Score, player2Score);
    }

    private void Hook_OnScoresMultiplierChanged(int oldScore, int newScore)
    {
        OnScoresMultiplierChanged?.Invoke(player1ScoreMultiplier, player2ScoreMultiplier);
    }

    #endregion

    #region Unity Functions

    private void Awake()
    {
        manager = this;
#if ENABLE_VIVOX
        OnGameStarted += FindObjectOfType<VivoxManager>().EventCallback_JoinChannel;
#endif
    }
    #endregion

    #region Callbacks

    public override void OnStartServer()
    {
        Server_Initialize();
    }

    #endregion

    #region OnServer Initialze Game

    [Server]
    private void Server_Initialize()
    {
        print("Initializing GamePlay Manager - " + GameNetworkManager.manager.gamePlayersData.Count);

        int count = GameNetworkManager.manager.gamePlayersData.Count;

        if (count != 2)
        {
            if (count != 4)
            {
                Debug.LogError("GamePlayManager : OnServer_Initialize failed because the number of players are invalid.");
                return;
            }
            serverteamtype = TeamType.TeamA;
            matchtype = MatchType.FourPlayer;
            Debug.Log("Match Type is Set to Four Player");
        }
        else
        {
            matchtype = MatchType.TwoPlayer;
            Debug.Log("Match Type is Set to Two Player");
        }


        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                //player1PlayfaID = GameNetworkManager.manager.gamePlayersData[0].playfabID;
                //player2PlayfaID = GameNetworkManager.manager.gamePlayersData[1].playfabID;


                player1PlayfaID = GameNetworkManager.manager.gamePlayersData[0].playfabID;
                player2PlayfaID = GameNetworkManager.manager.gamePlayersData[1].playfabID;

                player1MovesLeft = maxMoves;
                player2MovesLeft = maxMoves;

                player3MovesLeft = 0;
                player4MovesLeft = 0;
                break;

            case MatchType.FourPlayer:

                player1PlayfaID = GameMode.gameMode.player1Data.playfabID;
                player2PlayfaID = GameMode.gameMode.player3Data.playfabID;
                player3PlayfaID = GameMode.gameMode.player2Data.playfabID;
                player4PlayfaID = GameMode.gameMode.player4Data.playfabID;

                Debug.Log(" BLUE 1 " + player1PlayfaID + " BLUE 2  " + player2PlayfaID + " RED 1  " + player3PlayfaID + " RED 2  " + player4PlayfaID);
                player1MovesLeft = maxMoves;
                player2MovesLeft = maxMoves;
                player3MovesLeft = maxMoves;
                player4MovesLeft = maxMoves;
                break;
        }

        Debug.Log("First Palyer Turn " + player1PlayfaID);

        currentTurnPlayfabID = player1PlayfaID;
        currentTurnPlayerteamtype = TeamType.TeamA;

        player1Score = 0;
        player2Score = 0;

        player1ScoreMultiplier = 1;
        player2ScoreMultiplier = 1;

        round = maxRounds;
        currntround = round;
        seed = UnityEngine.Random.Range(0, 99999);
        grid = Grid.grid;

        //Debug.Log("OnGameStarted Invoke First turn will be   " + currentTurnPlayfabID);
        OnRoundChanged?.Invoke(maxRounds);
        OnTurnChanged?.Invoke(true);
        OnGameStarted?.Invoke();
        //Debug.Log("Gameplay Initilized grid.Initialize  " + currentTurnPlayfabID);
        grid.Initialize(seed, true, true);
    }

    #endregion

    #region OnServer Moves Counter

    [ServerCallback]
    public void Server_DecreaseMovesCounter()
    {

        Debug.Log("Which Player Turn  " + Server_CheckWhichPlayerTurn());

        if (Server_CheckWhichPlayerTurn() == player1PlayfaID)
        {
            Debug.Log("player1MovesLeft  " + player1MovesLeft);
            player1MovesLeft--;
            if (player1MovesLeft <= 0)
                StartCoroutine(Server_Coroutine_InitializeNewRound());
        }
        if (Server_CheckWhichPlayerTurn() == player2PlayfaID)
        {
            Debug.Log("player2MovesLeft  " + player2MovesLeft);
            player2MovesLeft--;
            if (player2MovesLeft <= 0)
                StartCoroutine(Server_Coroutine_InitializeNewRound());
        }
        if (Server_CheckWhichPlayerTurn() == player3PlayfaID)
        {
            Debug.Log("player3MovesLeft  " + player3MovesLeft);
            player3MovesLeft--;
            if (player3MovesLeft <= 0)
                StartCoroutine(Server_Coroutine_InitializeNewRound());
        }
        if (Server_CheckWhichPlayerTurn() == player4PlayfaID)
        {
            Debug.Log("player4MovesLeft  " + player4MovesLeft);
            player4MovesLeft--;
            if (player4MovesLeft <= 0)
                StartCoroutine(Server_Coroutine_InitializeNewRound());
        }

        OnMoveCounterChanged?.Invoke(player1MovesLeft, player2MovesLeft, player3MovesLeft, player4MovesLeft);
    }

    [ServerCallback]
    public void Server_IncreaseMovesCounter()
    {

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                if (Server_IsPlayer1Turn())
                {
                    player1MovesLeft++;
                }
                else
                {
                    player2MovesLeft++;
                }

                OnMoveCounterChanged?.Invoke(player1MovesLeft, player2MovesLeft, player3MovesLeft, player4MovesLeft);
                break;
            case MatchType.FourPlayer:

                if (Server_CheckWhichPlayerTurn() == player1PlayfaID)
                {
                    Debug.Log("Server_IncreaseMovesCounter " + player1PlayfaID);
                    player1MovesLeft++;
                }
                else if (Server_CheckWhichPlayerTurn() == player2PlayfaID)
                {
                    Debug.Log("Server_IncreaseMovesCounter " + player2PlayfaID);
                    player2MovesLeft++;
                }
                else if (Server_CheckWhichPlayerTurn() == player3PlayfaID)
                {
                    Debug.Log("Server_IncreaseMovesCounter " + player3PlayfaID);
                    player3MovesLeft++;
                }
                else if (Server_CheckWhichPlayerTurn() == player4PlayfaID)
                {
                    Debug.Log("Server_IncreaseMovesCounter " + player4PlayfaID);
                    player4MovesLeft++;
                }
                OnMoveCounterChanged?.Invoke(player1MovesLeft, player2MovesLeft, player3MovesLeft, player4MovesLeft);
                break;
        }
    }

    #endregion

    #region Timer

    [ServerCallback]
    public void Server_OnTimerCompleted()
    {
        Debug.Log("Server_OnTimerCompleted start" + Server_CheckWhichPlayerTurn());
        TurnTimer.timer.ResetTimer();
        Rpc_Reset_OnTimerCompleted();

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                if (Server_IsPlayer1Turn())
                {
                    player1MovesLeft = 0;
                }
                else
                {
                    player2MovesLeft = 0;
                }
                break;

            case MatchType.FourPlayer:
                if (Server_CheckWhichPlayerTurn() == player1PlayfaID)
                {
                    Debug.Log("Server_OnTimerCompleted " + player1PlayfaID);
                    player1MovesLeft = 0;
                }
                else if (Server_CheckWhichPlayerTurn() == player2PlayfaID)
                {
                    Debug.Log("Server_OnTimerCompleted " + player2PlayfaID);
                    player2MovesLeft = 0;
                }
                else if (Server_CheckWhichPlayerTurn() == player3PlayfaID)
                {
                    Debug.Log("Server_OnTimerCompleted " + player3PlayfaID);
                    player3MovesLeft = 0;
                }
                else if (Server_CheckWhichPlayerTurn() == player4PlayfaID)
                {
                    Debug.Log("Server_OnTimerCompleted " + player4PlayfaID);
                    player4MovesLeft = 0;
                }
                break;
        }

        StartCoroutine(Server_Coroutine_InitializeNewRound());
    }

    [ClientRpc]
    private void Rpc_Reset_OnTimerCompleted()
    {
        //!IsMyTurn() because the turn is not updated when this RPC is called
        TurnTimer.timer.ResetTimer();
    }

    #endregion

    #region OnServer Initialize New Round

    [Server]
    IEnumerator Server_Coroutine_InitializeNewRound()
    {
        yield return new WaitWhile(() => grid.isFilling);

        switch (matchtype)
        {
            case MatchType.TwoPlayer:

                if (currentTurnPlayfabID == player1PlayfaID)
                {
                    currentTurnPlayfabID = player2PlayfaID;
                }
                else
                {
                    currentTurnPlayfabID = player1PlayfaID;
                }
                /*if (Server_IsPlayer1Turn())
                {
                    player2MovesLeft = 0;
                }
                else
                {
                    player1MovesLeft = 0;
                }*/

                break;

            case MatchType.FourPlayer:

                if (currentTurnPlayfabID == player1PlayfaID)
                {
                    Debug.Log("Turn Changed to Player2");
                    currentTurnPlayfabID = player2PlayfaID;
                    currentTurnPlayerteamtype = TeamType.TeamB;
                }
                else if (currentTurnPlayfabID == player2PlayfaID)
                {
                    Debug.Log("Turn Changed to Player3");
                    currentTurnPlayfabID = player3PlayfaID;
                    currentTurnPlayerteamtype = TeamType.TeamA;
                }
                else if (currentTurnPlayfabID == player3PlayfaID)
                {
                    Debug.Log("Turn Changed to Player4");
                    currentTurnPlayfabID = player4PlayfaID;
                    currentTurnPlayerteamtype = TeamType.TeamB;
                }
                else if (currentTurnPlayfabID == player4PlayfaID)
                {
                    Debug.Log("Turn Changed to Player1");
                    currentTurnPlayfabID = player1PlayfaID;
                    currentTurnPlayerteamtype = TeamType.TeamA;
                }
                break;
        }

        Debug.Log("currentTurnPlayfabID  " + currentTurnPlayfabID);

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                if (player1MovesLeft <= 0 && player2MovesLeft <= 0)
                {
                    extendedRound = false;
                    round--;
                    currntround = round;
                    if (player1Score == player2Score && round <= 0)
                    {
                        extendedRound = true;
                        extendedRoundNumber++;
                    }

                    if (round > 0)
                    {
                        Debug.Log("Round changed invoked " + round);
                        OnRoundChanged?.Invoke(round);
                        player1MovesLeft = maxMoves;
                        player2MovesLeft = maxMoves;

                    }
                    else if (extendedRound && round <= 0)
                    {
                        OnRoundChanged?.Invoke(round + extendedRoundNumber);
                        Debug.Log("Round changed invoked " + round);
                        player1MovesLeft = maxMoves;
                        player2MovesLeft = maxMoves;
                    }
                }
                break;
            case MatchType.FourPlayer:

                if (player1MovesLeft <= 0 && player2MovesLeft <= 0 && player3MovesLeft <= 0 && player4MovesLeft <= 0)
                {
                    extendedRound = false;
                    round--;
                    currntround = round;

                    if (player1Score == player2Score && round <= 0)
                    {
                        extendedRound = true;
                        extendedRoundNumber++;
                    }
                    if (round > 0)
                    {
                        Debug.Log("Round changed invoked " + round);
                        OnRoundChanged?.Invoke(round);
                        player1MovesLeft = maxMoves;
                        player2MovesLeft = maxMoves;
                        player3MovesLeft = maxMoves;
                        player4MovesLeft = maxMoves;
                    }
                    else if (extendedRound && round <= 0)
                    {
                        OnRoundChanged?.Invoke(round + extendedRoundNumber);
                        Debug.Log("Round changed invoked " + round);
                        player1MovesLeft = maxMoves;
                        player2MovesLeft = maxMoves;
                        player3MovesLeft = maxMoves;
                        player4MovesLeft = maxMoves;
                    }
                }
                break;
        }

        Debug.Log("Currunt Round " + round);
        Debug.Log("extendedRound " + extendedRound);

        if (round > 0 || (extendedRound && round <= 0))
        {
            Debug.Log("This is Working");
            OnMoveCounterChanged?.Invoke(player1MovesLeft, player2MovesLeft, player3MovesLeft, player4MovesLeft);
            OnTurnChanged?.Invoke(Server_IsPlayer1Turn());
        }
        else
        {
            Debug.Log("Game Ended");
            StartCoroutine(StopServerAfterEndGameTimer());
        }
    }

    #endregion

    #region OnServer Update Score

    [ServerCallback]
    public void Server_AddScore()
    {
        if (!grid.gridFilled) return;

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                if (currentTurnPlayfabID == player1PlayfaID)
                {
                    player1Score += player1ScoreMultiplier;
                }
                else
                {
                    player2Score += player2ScoreMultiplier;
                }
                break;

            case MatchType.FourPlayer:

                if (serverteamtype==TeamType.TeamA)
                {
                    player1Score += player1ScoreMultiplier;
                }
                else if (serverteamtype == TeamType.TeamB)
                {
                    player2Score += player2ScoreMultiplier;
                }

                break;
        }



        OnScoresChanged?.Invoke(player1Score, player2Score);
    }

    [ServerCallback]
    public void Server_AddScoreMultiplier()
    {
        if (!grid.gridFilled) return;

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                if (currentTurnPlayfabID == player1PlayfaID)
                {
                    player1ScoreMultiplier++;
                }
                else
                {
                    player2ScoreMultiplier++;
                }
                break;

            case MatchType.FourPlayer:
                if (serverteamtype == TeamType.TeamA)
                {
                    player1ScoreMultiplier++;
                }
                else if (serverteamtype == TeamType.TeamB)
                {
                    player2ScoreMultiplier++;
                }
                break;
        }

        OnScoresMultiplierChanged?.Invoke(player1ScoreMultiplier, player2ScoreMultiplier);
    }

    #endregion

    #region Client & Server Game Over
    //Game over is also done in GamePlayer script

    [Server]
    IEnumerator StopServerAfterEndGameTimer()
    {
        Rpc_GameOver();

        if (isGameOver) yield break;

        isGameOver = true;
        int secondsToWait = GameOverUIManager.endGameTimer;

        while (secondsToWait > 0)
        {
            yield return new WaitForSeconds(1);
            if (GameNetworkManager.manager.numPlayers == 0)
            {
                Debug.Log("Server Quit After End Game Timer : 1");
                GameNetworkManager.manager.StopServer();
                Application.Quit();
            }
        }

        Debug.Log("Server Quit After End Game Timer : 2");
        GameNetworkManager.manager.StopServer();
        Application.Quit();
    }

    [ClientRpc]
    private void Rpc_GameOver()
    {
        if (!isGameOver)
            Client_GameOver();
        isGameOver = true;
    }

    [Client]
    public void Client_GameOver(bool otherUserQuit = false, bool leftGame = false)
    {
        int myScore = player1Score;
        int opponentScore = player2Score;

        int wonPlayerScore = Mathf.Max(player1Score, player2Score);
        int lostPlayerScore = Mathf.Min(player1Score, player2Score);

        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                myScore = LocalPlayer.isPlayer1 ? player1Score : player2Score;
                opponentScore = !LocalPlayer.isPlayer1 ? player1Score : player2Score;
                break;

            case MatchType.FourPlayer:

                if (currentTurnPlayerteamtype == TeamType.TeamA)
                {
                    myScore = player1Score;
                    opponentScore = player2Score;
                }
                else
                {
                    myScore = player2Score;
                    opponentScore = player1Score;
                }

                break;
        }

        OnGameEnd?.Invoke();
        print("Game Over : " + otherUserQuit.ToString() + " : " + leftGame.ToString());

        if (leftGame)
        {
            OnLostGame?.Invoke();
            GameMatchEventManager.GamePlayManager_GameLost();

            switch (matchtype)
            {
                case MatchType.TwoPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(GameplayUIManager.manager.opponentPlayfabID, false);
                    break;

                case MatchType.FourPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(MatchMakingUIManager.manager.OpponentTeam[0].playfabID, false, MatchMakingUIManager.manager.OpponentTeam[1].playfabID);
                    break;
            }
            return;
        }

        if (otherUserQuit)
        {
            OnWonGame?.Invoke();
            GameMatchEventManager.GamePlayManager_GameWon();

            switch (matchtype)
            {
                case MatchType.TwoPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(PlayerData.PlayfabID, true);
                    break;

                case MatchType.FourPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(MatchMakingUIManager.manager.LocalTeam[0].playfabID, false, MatchMakingUIManager.manager.LocalTeam[1].playfabID);
                    break;
            }
            return;
        }

        if (myScore > opponentScore || myScore == opponentScore)
        {
            OnWonGame?.Invoke();
            GameMatchEventManager.GamePlayManager_GameWon();

            switch (matchtype)
            {
                case MatchType.TwoPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(PlayerData.PlayfabID, true);
                    break;

                case MatchType.FourPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(MatchMakingUIManager.manager.LocalTeam[0].playfabID, false, MatchMakingUIManager.manager.LocalTeam[1].playfabID);
                    break;
            }
        }
        else
        {
            OnLostGame?.Invoke();
            GameMatchEventManager.GamePlayManager_GameLost();

            switch (matchtype)
            {
                case MatchType.TwoPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(GameplayUIManager.manager.opponentPlayfabID, false);
                    break;

                case MatchType.FourPlayer:
                    GameOverUIManager.manager.InitializeGameOverMenu(MatchMakingUIManager.manager.OpponentTeam[0].playfabID, false, MatchMakingUIManager.manager.OpponentTeam[1].playfabID);
                    break;
            }
        }

    }

    #endregion

    #region Helper Functions

    [Server]
    public bool Server_IsPlayer1Turn()
    {
        return currentTurnPlayfabID == player1PlayfaID;
    }

    [Server]
    public bool Server_IsMyTurn(string myplayfabid)
    {
        return currentTurnPlayfabID == myplayfabid;
    }

    [Server]
    public string Server_CheckWhichPlayerTurn()
    {
        return currentTurnPlayfabID;
    }

    [Client]
    public bool Client_IsMyTurn()
    {
        return currentTurnPlayfabID == PlayerData.PlayfabID;
    }

    [Client]
    public bool Client_AnyMovesLeft()
    {
        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                return LocalPlayer.isPlayer1 ? player1MovesLeft > 0 : player2MovesLeft > 0;
            case MatchType.FourPlayer:

                if (currentTurnPlayfabID == player1PlayfaID)
                {
                    return player1MovesLeft > 0;
                }
                if (currentTurnPlayfabID == player2PlayfaID)
                {
                    return player2MovesLeft > 0;
                }
                if (currentTurnPlayfabID == player3PlayfaID)
                {
                    return player3MovesLeft > 0;
                }
                if (currentTurnPlayfabID == player4PlayfaID)
                {
                    return player4MovesLeft > 0;
                }
                break;
        }

        return true;
    }

    [Server]
    public bool Server_AnyMovesLeft()
    {
        switch (matchtype)
        {
            case MatchType.TwoPlayer:
                return Server_IsPlayer1Turn() ? player1MovesLeft > 0 : player2MovesLeft > 0;
            case MatchType.FourPlayer:

                if (Server_CheckWhichPlayerTurn() == player1PlayfaID)
                {
                    return player1MovesLeft > 0;
                }
                if (Server_CheckWhichPlayerTurn() == player2PlayfaID)
                {
                    return player2MovesLeft > 0;
                }
                if (Server_CheckWhichPlayerTurn() == player3PlayfaID)
                {
                    return player3MovesLeft > 0;
                }
                if (Server_CheckWhichPlayerTurn() == player4PlayfaID)
                {
                    return player4MovesLeft > 0;
                }
                break;
        }
        return true;

    }

    #endregion
}