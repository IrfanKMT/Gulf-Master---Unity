using System;
using Mirror;
using UnityEngine;

public class BoosterManager : NetworkBehaviour
{
    public static BoosterManager manager;

    #region Events

    // Booster Events
    public event Action<BoosterAndPerkItem, BoosterAndPerkItem, BoosterAndPerkItem, BoosterAndPerkItem> OnBoostersInitialized; // My Booster, My Teammate Booster, Opponent Team Booster, Opponent Team Booster
    public event Action<int, int, int, int, int, int, int, int> OnBoosterCandiesChanged; //My Booster Candies, My Max Candies, My Teammate Booster Candies, My Teammate Max Candies, Opponent Team Booster Candies, Opponent Team Max Candies, Opponent Team Booster Candies, Opponent Team Max Candies
    public event Action<bool> OnBoosterActiveStatusChanged;
    #endregion

    #region Sync Vars
    //Sync Vars
    [SyncVar(hook = nameof(Hook_OnPlayerBoosterCandiesUpdated))] int player1BoosterCandies = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerBoosterCandiesUpdated))] int player2BoosterCandies = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerBoosterCandiesUpdated))] int player3BoosterCandies = 0;
    [SyncVar(hook = nameof(Hook_OnPlayerBoosterCandiesUpdated))] int player4BoosterCandies = 0;

    #endregion

    #region Variables

    public Transform boosterHolder;

    [Space]
    public GamePlayManager gamePlayManager;

    //Server only
    internal bool isBoosterWorking = false; // Set From Booster Classes

    private BoosterAndPerkItem player1Booster;
    private BoosterAndPerkItem player2Booster;
    private BoosterAndPerkItem player3Booster;
    private BoosterAndPerkItem player4Booster;

    #endregion

    #region Unity and Mirror Functions

    private void Awake()
    {
        manager = this;
    }

    public override void OnStartServer()
    {
        Grid.grid.Grid_OnCandyDestroyed += Server_OnCandyDestroyed;
    }

    public override void OnStartClient()
    {
        gamePlayManager.OnLostGame += Client_OnLostGame;
    }

    private void Start()
    {
        gamePlayManager.OnTurnChanged += (x) => OnTurnChanged();
    }


    #endregion

    #region Initialization

    public void Initialize(string player1BoosterID, string player2BoosterID, string player3BoosterID, string player4BoosterID)
    {
        if (BoosterAndPerksData.data != null)
        {
            switch (gamePlayManager.matchtype)
            {
                case MatchType.TwoPlayer:
                    player1Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1BoosterID);
                    player2Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2BoosterID);
                    player3Booster = new BoosterAndPerkItem();
                    player4Booster = new BoosterAndPerkItem();
                    break;

                case MatchType.FourPlayer:
                    player1Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1BoosterID);
                    player2Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player3BoosterID);
                    player3Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2BoosterID);
                    player4Booster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player4BoosterID);
                    break;
            }


        }

        OnBoostersInitialized?.Invoke(player1Booster, player2Booster, player3Booster, player4Booster);
        OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                        player2BoosterCandies, player2Booster.candiesToActivate,
                                        player3BoosterCandies, player3Booster.candiesToActivate,
                                        player4BoosterCandies, player4Booster.candiesToActivate);
    }
    #endregion

    #region Event Callbacks

    [Server]
    private void Server_OnCandyDestroyed(GamePiece piece, TeamType t = TeamType.None)
    {
        if (piece == null) return;
        if (!Grid.grid.gridFilled) return;
        if (Grid.grid.beingDestroyedByBooster) return;
        if (isBoosterWorking) return;

        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (piece.ColorComponent.Color == ColorType.Blue && gamePlayManager.Server_IsPlayer1Turn())
                {
                    player1BoosterCandies = Mathf.Clamp(player1BoosterCandies + 1, 0, player1Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);

                    if (player1BoosterCandies == player1Booster.candiesToActivate)
                        OnBoosterActiveStatusChanged?.Invoke(true);
                }
                else if (piece.ColorComponent.Color == ColorType.Red && !gamePlayManager.Server_IsPlayer1Turn())
                {
                    player2BoosterCandies = Mathf.Clamp(player2BoosterCandies + 1, 0, player2Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);
                }

                break;

            case MatchType.FourPlayer:

                if (piece.ColorComponent.Color == ColorType.Blue && gamePlayManager.Server_IsMyTurn(gamePlayManager.player1PlayfaID))
                {
                    Debug.Log("BLUE - 1");
                    player1BoosterCandies = Mathf.Clamp(player1BoosterCandies + 1, 0, player1Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);

                    if (player1BoosterCandies == player1Booster.candiesToActivate)
                        OnBoosterActiveStatusChanged?.Invoke(true);
                }
                else if (piece.ColorComponent.Color == ColorType.Blue && gamePlayManager.Server_IsMyTurn(gamePlayManager.player2PlayfaID))
                {
                    Debug.Log("BLUE - 2");
                    player2BoosterCandies = Mathf.Clamp(player2BoosterCandies + 1, 0, player2Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);

                    if (player2BoosterCandies == player2Booster.candiesToActivate)
                        OnBoosterActiveStatusChanged?.Invoke(true);
                }
                else if (piece.ColorComponent.Color == ColorType.Red && gamePlayManager.Server_IsMyTurn(gamePlayManager.player3PlayfaID))
                {
                    Debug.Log("RED - 1");
                    player3BoosterCandies = Mathf.Clamp(player3BoosterCandies + 1, 0, player3Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);

                    if (player3BoosterCandies == player3Booster.candiesToActivate)
                        OnBoosterActiveStatusChanged?.Invoke(true);
                }
                else if (piece.ColorComponent.Color == ColorType.Red && gamePlayManager.Server_IsMyTurn(gamePlayManager.player4PlayfaID))
                {
                    Debug.Log("RED - 2");
                    player4BoosterCandies = Mathf.Clamp(player4BoosterCandies + 1, 0, player4Booster.candiesToActivate);
                    OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                                player2BoosterCandies, player2Booster.candiesToActivate,
                                                player3BoosterCandies, player3Booster.candiesToActivate,
                                                player4BoosterCandies, player4Booster.candiesToActivate);

                    if (player4BoosterCandies == player4Booster.candiesToActivate)
                        OnBoosterActiveStatusChanged?.Invoke(true);
                }
                break;
        }


    }

    private void OnTurnChanged()
    {
        Booster[] allBoosters = FindObjectsByType<Booster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allBoosters)
            Destroy(item.gameObject);
    }

    [Client]
    private void Client_OnLostGame()
    {
        switch (GamePlayManager.manager.matchtype)
        {
            case MatchType.TwoPlayer:
                BoosterAndPerkItem localPlayerBooster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(GameMode.gameMode.localPlayer1.boosterID);
                InventoryManager.manager.ConsumeItem(localPlayerBooster.itemId, 1, (success) => Debug.Log(success ? "<color=green>Booster Successfully Consumed</color>" : "<color=red>Booster Consumption Failed</color>"));
                break;
            case MatchType.FourPlayer:
                BoosterAndPerkItem myboosterid = BoosterAndPerksData.data.GetBoosterOrPerkFromId(MatchMakingUIManager.manager.LocalTeam[0].boosterID);
                InventoryManager.manager.ConsumeItem(myboosterid.itemId, 1, (success) => Debug.Log(success ? "<color=green>Booster Successfully Consumed</color>" : "<color=red>Booster Consumption Failed</color>"));
                break;
        }
    }

    #endregion

    #region Commands And RPCs

    //Called from game player script
    [Server]
    public void Server_UseBooster(bool isPlayer1, string playfabID = "")
    {
        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (isPlayer1 != gamePlayManager.Server_IsPlayer1Turn())
                    return;

                if (!Grid.grid.isFilling && Grid.grid.gridFilled && !isBoosterWorking && !IsAnyBoosterActive() && !Grid.grid.IsAnyGemMovingOrClearing() && !Grid.grid.isSwappingPiece && !PerksManager.manager.isPerkWorking)
                {
                    BoosterAndPerkItem playerBooster = isPlayer1 ? player1Booster : player2Booster;

                    int seed = UnityEngine.Random.Range(0, 9999);

                    Booster booster = Instantiate(playerBooster.itemPrefab, boosterHolder).GetComponent<Booster>();
                    booster.InitializeBooster(seed, isPlayer1, playerBooster.itemId);

                    Rpc_UseBooster(playerBooster.itemId, seed, isPlayer1);
                }
                break;

            case MatchType.FourPlayer:

                string currentTurnPlayfabID = gamePlayManager.currentTurnPlayfabID;

                if (playfabID != currentTurnPlayfabID)
                {
                    Debug.Log("Not my turn, So Can't use Booster!");
                    return;
                }

                if (!Grid.grid.isFilling && Grid.grid.gridFilled && !isBoosterWorking && !IsAnyBoosterActive() && !Grid.grid.IsAnyGemMovingOrClearing() && !Grid.grid.isSwappingPiece && !PerksManager.manager.isPerkWorking)
                {
                    BoosterAndPerkItem playerBooster;

                    if (currentTurnPlayfabID == gamePlayManager.player1PlayfaID)
                    {
                        playerBooster = player1Booster;
                    }
                    else if (currentTurnPlayfabID == gamePlayManager.player2PlayfaID)
                    {
                        playerBooster = player2Booster;
                    }
                    else if (currentTurnPlayfabID == gamePlayManager.player3PlayfaID)
                    {
                        playerBooster = player3Booster;
                    }
                    else
                    {
                        playerBooster = player4Booster;
                    }

                    int seed = UnityEngine.Random.Range(0, 9999);

                    Booster booster = Instantiate(playerBooster.itemPrefab, boosterHolder).GetComponent<Booster>();
                    booster.InitializeBooster(seed, isPlayer1, playerBooster.itemId);

                    Rpc_UseBooster(playerBooster.itemId, seed, isPlayer1);
                }
                break;
        }


    }

    [ClientRpc]
    private void Rpc_UseBooster(string boosterID, int seed, bool isPlayer1)
    {
        if (IsAnyBoosterActive()) return;

        BoosterAndPerkItem playerBooster = BoosterAndPerksData.data.GetBoosterOrPerkFromId(boosterID);
        Booster booster = Instantiate(playerBooster.itemPrefab, boosterHolder).GetComponent<Booster>();
        booster.InitializeBooster(seed, isPlayer1, boosterID);
    }

    //Destroy All Boosters that are instantiated by the user if he left the game and is rejoining

    [Server]
    public void Server_Reconnection_DestroyAllBoosters(bool isPlayer1)
    {
        if (isBoosterWorking) return;

        Booster[] allBoosters = FindObjectsByType<Booster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allBoosters)
            if (item.isPlayer1Booster == isPlayer1)
                DestroyImmediate(item.gameObject);

        Rpc_Reconnection_DestroyAllBoosters(isPlayer1);

        //Only spawn booster if its not already working
        if (IsAnyBoosterActive())
        {
            Booster booster = Server_GetActiveBooster();
            Rpc_UseBooster(booster.boosterID, booster.seed, booster.isPlayer1Booster);
        }
    }

    [ClientRpc]
    private void Rpc_Reconnection_DestroyAllBoosters(bool isPlayer1)
    {
        Booster[] allBoosters = FindObjectsByType<Booster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allBoosters)
            if (item.isPlayer1Booster == isPlayer1)
                Destroy(item.gameObject);
    }

    #endregion

    #region SyncVar Hooks

    private void Hook_OnPlayerBoosterCandiesUpdated(int oldCandies, int newCandies)
    {
        if (player1Booster != null && player2Booster != null)
            OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                        player2BoosterCandies, player2Booster.candiesToActivate,
                                        player3BoosterCandies, player3Booster.candiesToActivate,
                                        player4BoosterCandies, player4Booster.candiesToActivate);
    }

    #endregion

    #region Helper Functions

    /// <summary>
    /// Called By Booster Classes
    /// </summary>
    [ServerCallback]
    public void Server_SetBoosterCollectedCandyToZero()
    {
        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (gamePlayManager.Server_IsPlayer1Turn())
                {
                    player1BoosterCandies = 0;
                }
                else
                {
                    player2BoosterCandies = 0;
                }
                break;

            case MatchType.FourPlayer:

                if (gamePlayManager.Server_CheckWhichPlayerTurn() == gamePlayManager.player1PlayfaID)
                {
                    Debug.Log("Player 1 booster to zero");
                    player1BoosterCandies = 0;
                }
                else if (gamePlayManager.Server_CheckWhichPlayerTurn() == gamePlayManager.player2PlayfaID)
                {
                    Debug.Log("Player 1 booster to zero");
                    player2BoosterCandies = 0;
                }
                else if (gamePlayManager.Server_CheckWhichPlayerTurn() == gamePlayManager.player3PlayfaID)
                {
                    Debug.Log("Player 1 booster to zero");
                    player3BoosterCandies = 0;
                }
                else if (gamePlayManager.Server_CheckWhichPlayerTurn() == gamePlayManager.player4PlayfaID)
                {
                    Debug.Log("Player 1 booster to zero");
                    player4BoosterCandies = 0;
                }
                break;
        }

        OnBoosterCandiesChanged?.Invoke(player1BoosterCandies, player1Booster.candiesToActivate,
                                        player2BoosterCandies, player2Booster.candiesToActivate,
                                        player3BoosterCandies, player3Booster.candiesToActivate,
                                        player4BoosterCandies, player4Booster.candiesToActivate);
    }

    [Client]
    public bool Client_IsBoosterCandyFull(bool player1)
    {
        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (player1)
                {
                    return player1BoosterCandies == player1Booster.candiesToActivate;
                }
                else
                {
                    return player2BoosterCandies == player2Booster.candiesToActivate;
                }

            case MatchType.FourPlayer:
                if(PlayerData.PlayfabID == gamePlayManager.player1PlayfaID)
                {
                    return player1BoosterCandies == player1Booster.candiesToActivate;
                }
                else if (PlayerData.PlayfabID == gamePlayManager.player2PlayfaID)
                {
                    return player2BoosterCandies == player2Booster.candiesToActivate;
                }
                else if (PlayerData.PlayfabID == gamePlayManager.player3PlayfaID)
                {
                    return player3BoosterCandies == player3Booster.candiesToActivate;
                }
                else
                {
                    return player4BoosterCandies == player4Booster.candiesToActivate;
                }
        }
        return false;
    }

    public bool IsAnyBoosterActive()
    {
        return FindObjectsByType<Booster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0;
    }

    [Server]
    private Booster Server_GetActiveBooster()
    {
        return FindObjectsByType<Booster>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)[0];
    }

    #endregion
}
