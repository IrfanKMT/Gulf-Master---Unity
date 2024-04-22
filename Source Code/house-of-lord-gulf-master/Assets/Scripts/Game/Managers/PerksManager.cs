using System;
using Mirror;
using UnityEngine;

public class PerksManager : NetworkBehaviour
{
    public static PerksManager manager;
    [SerializeField] GameplayUIManager uiManager;

    #region Events

    public event Action<BoosterAndPerkItem, BoosterAndPerkItem, 
                        BoosterAndPerkItem, BoosterAndPerkItem, 
                        BoosterAndPerkItem, BoosterAndPerkItem, 
                        BoosterAndPerkItem, BoosterAndPerkItem> OnPerksInitialized; // First 2 are my perks, last 2 are opponent's perks
    public event Action<bool,bool> OnPlayerUsedPerk; //First bool tell if it is player 1 perk or player 2 perk, 2nd bool tell if its perk1 or perk2
    //public event Action<bool, bool> OnPlayerUsedPerk;

    #endregion

    #region Variables

    internal string player1Perk1ID;
    internal string player1Perk2ID;
    internal string player2Perk1ID;
    internal string player2Perk2ID;
    internal string player3Perk1ID;
    internal string player3Perk2ID;
    internal string player4Perk1ID;
    internal string player4Perk2ID;

    // Player 1 Perks
    BoosterAndPerkItem player1Perk1Item;
    BoosterAndPerkItem player1Perk2Item;

    // Player 2 Perks
    BoosterAndPerkItem player2Perk1Item;
    BoosterAndPerkItem player2Perk2Item;

    // Player 3 Perks
    BoosterAndPerkItem player3Perk1Item;
    BoosterAndPerkItem player3Perk2Item;

    // Player 4 Perks
    BoosterAndPerkItem player4Perk1Item;
    BoosterAndPerkItem player4Perk2Item;

    internal bool isPerkWorking = false; // Will be called from Perk Classes
    private Perk spawnedPerk;

    #endregion

    #region Sync Vars

    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer1Perk1Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer1Perk2Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer2Perk1Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer2Perk2Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer3Perk1Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer3Perk2Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer4Perk1Used;
    [SyncVar(hook = nameof(Hook_OnPerkUsed))] bool isPlayer4Perk2Used;

    #endregion

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        GamePlayManager.manager.OnTurnChanged += x => OnTurnChanged();
    }

    #endregion

    #region Initialization

    //Called from GameMode script
    public void Initialize(string p1p1, string p1p2,string p2p1,string p2p2,string p3p1,string p3p2 ,string p4p1 ,string p4p2)
    {

        if (isServer)
        {
            return;
        }

        player1Perk1ID = p1p1;
        player1Perk2ID = p1p2;

        player2Perk1ID = p2p1;
        player2Perk2ID = p2p2;

        if (player1Perk1ID == null || player1Perk2ID == null || player2Perk1ID == null || player2Perk2ID == null)
        {
            Debug.LogError("Error When Initializing Perks Manager: Perk ID is Null For some user!\nPlayer 1 Perks: " + player1Perk1ID + " | " + player1Perk2ID + "\nPlayer 2 Perks: " + player2Perk1ID + " | " + player2Perk2ID);
            return;
        }

        player1Perk1Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1Perk1ID);
        player1Perk2Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1Perk2ID);

        player2Perk1Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2Perk1ID);
        player2Perk2Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2Perk2ID);

        player3Perk1Item = null; 
        player3Perk2Item = null;
        player4Perk1Item = null; 
        player4Perk2Item = null;

        switch (GamePlayManager.manager.matchtype)
        {
            case MatchType.FourPlayer:
                player3Perk1ID = p3p1;
                player3Perk2ID = p3p2;

                player4Perk1ID = p4p1;
                player4Perk2ID = p4p2;

                if (player3Perk1ID == null || player3Perk2ID == null || player4Perk1ID == null || player4Perk2ID == null)
                {
                    Debug.LogError("Error When Initializing Perks Manager: Perk ID is Null For some user!\nPlayer 1 Perks: " + player1Perk1ID + " | " + player1Perk2ID + "\nPlayer 2 Perks: " + player2Perk1ID + " | " + player2Perk2ID);
                    return;
                }

                player3Perk1Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1Perk1ID);
                player3Perk2Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player1Perk2ID);

                player4Perk1Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2Perk1ID);
                player4Perk2Item = BoosterAndPerksData.data.GetBoosterOrPerkFromId(player2Perk2ID);
                break;
        }

        OnPerksInitialized?.Invoke(player1Perk1Item, player1Perk2Item, player2Perk1Item, player2Perk2Item, player3Perk1Item, player3Perk2Item, player4Perk1Item, player4Perk2Item);

#if !UNITY_SERVER
        if(!player1Perk1ID.Contains("hammer") && !player1Perk1ID.Contains("shuffle"))
            InventoryManager.manager.ConsumeItem(player1Perk1ID, 1, (success) => Debug.Log(success ? $"<color = green> Perk {player1Perk1ID} Consumed Successfully </color>" : $"<color = red> Perk {player1Perk1ID} Comsumption Failed </color>"));

        if(!player1Perk2ID.Contains("hammer") && !player1Perk2ID.Contains("shuffle"))
            InventoryManager.manager.ConsumeItem(player1Perk2ID, 1, (success) => Debug.Log(success ? $"<color = green> Perk {player1Perk2ID} Consumed Successfully </color>" : $"<color = red> Perk {player1Perk2ID} Comsumption Failed </color>"));
#endif
    }

    #endregion

    #region Event Callbacks

    private void OnTurnChanged()
    {
        Perk[] allPerks = FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allPerks)
            Destroy(item.gameObject);

        uiManager.HidePerksHighlight(true, false);
        uiManager.HidePerksHighlight(true, true);
        uiManager.HidePerksHighlight(false, false);
        uiManager.HidePerksHighlight(false, true);
    }


    #endregion

    #region Using Perks

    /// <summary>
    /// Return true if successfully used the perk
    /// </summary>
    //Called from game player script
    [Server]
    public void Server_UsePerk(bool isPlayer1Perk, bool isPerk1)
    {
        if (!Grid.grid.isFilling && Grid.grid.gridFilled  && !Grid.grid.Perk_IsAnyBoosterActive() && !Grid.grid.IsAnyGemMovingOrClearing() && !isPerkWorking && !Grid.grid.isSwappingPiece)
        {
            //if (isPlayer1Perk)
            //{
            //    if (isPerk1)
            //    {
            //        if (isPlayer1Perk1Used)
            //            return;
            //    }
            //    else
            //    {
            //        if (isPlayer1Perk2Used)
            //            return;
            //    }
            //}
            //else
            //{
            //    if (isPerk1)
            //    {
            //        if (isPlayer2Perk1Used)
            //            return;
            //    }
            //    else
            //    {
            //        if (isPlayer2Perk2Used)
            //            return;
            //    }
            //}

            int seed = UnityEngine.Random.Range(0, 99999);

            BoosterAndPerkItem perkToUse = isPlayer1Perk ? (isPerk1 ? player1Perk1Item : player1Perk2Item) : (isPerk1 ? player2Perk1Item : player2Perk2Item);

            Perk perk = Instantiate(perkToUse.itemPrefab).GetComponent<Perk>();
            perk.InitializePerk(seed, perkToUse, isPlayer1Perk, isPerk1);

            spawnedPerk = perk;

            GameplayUIManager.manager.OnPerkSuccessfullyUsed(isPlayer1Perk, isPerk1, perkToUse);
            Rpc_UsePerk(perkToUse.itemId, seed, isPlayer1Perk, isPerk1);
        }
    }

    #endregion

    #region RPCs

    [ClientRpc]
    private void Rpc_UsePerk(string perkID, int seed, bool isPlayer1Perk, bool isPerk1)
    {
        if (FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        BoosterAndPerkItem perkItem = BoosterAndPerksData.data.GetBoosterOrPerkFromId(perkID);
        Perk perk = Instantiate(perkItem.itemPrefab).GetComponent<Perk>();
        perk.InitializePerk(seed, perkItem, isPlayer1Perk, isPerk1);
        spawnedPerk = perk;

        GameplayUIManager.manager.OnPerkSuccessfullyUsed(isPlayer1Perk, isPerk1, perkItem);
    }

    [ClientRpc]
    private void Rpc_CancelPerk()
    {
        GameplayUIManager.manager.HidePerksHighlight(spawnedPerk.isPlayer1Perk, spawnedPerk.isPerk1);

        if (spawnedPerk != null)
            spawnedPerk.CancelPerk();

        spawnedPerk = null;
    }

    #endregion

    #region Commands

    [Command(requiresAuthority = false)]
    public void Cmd_CancelPerk()
    {
        GameplayUIManager.manager.HidePerksHighlight(spawnedPerk.isPlayer1Perk, spawnedPerk.isPerk1);
        if (spawnedPerk != null)
            spawnedPerk.CancelPerk();

        spawnedPerk = null;
        Rpc_CancelPerk();
    }

    #endregion

    #region Perk Used

    public void On_PerkUsed(bool isPlayer1Perk, bool isPerk1)
    {
        isPerkWorking = false;
        spawnedPerk = null;
        GameplayUIManager.manager.HidePerksHighlight(isPlayer1Perk, isPerk1);
        OnPlayerUsedPerk?.Invoke(isPlayer1Perk, isPerk1);

        if (!isServer) return;

        if (isPlayer1Perk)
        {
            if (isPerk1) isPlayer1Perk1Used = true;
            else isPlayer1Perk2Used = true;
        }
        else
        {
            if (isPerk1) isPlayer2Perk1Used = true;
            else isPlayer2Perk2Used = true;
        }
    }

    #endregion

    #region Syncvar Hooks

    private void Hook_OnPerkUsed(bool _old, bool _new)
    {
        uiManager.PerksManager_OnPerksUsedDataRecieved(isPlayer1Perk1Used, isPlayer1Perk2Used, isPlayer2Perk1Used, isPlayer2Perk2Used);
    }

    #endregion

    #region Reconnection

    //Destroy All perks that are instantiated by the user if he left the game and is rejoining
    [Server]
    public void Server_Reconnection_DestroyAllPerks(bool isPlayer1)
    {
        if (isPerkWorking) return;

        Perk[] allPerks = FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allPerks)
            if (item.isPlayer1Perk == isPlayer1)
            {
                uiManager.HidePerksHighlight(isPlayer1, item.isPerk1);
                DestroyImmediate(item.gameObject);
            }

        Rpc_Reconnection_DestroyAllPerks(isPlayer1);

            //Only spawn perks if its not already working
        if (FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length>0)
        {
            Perk perk = FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            Rpc_UsePerk(perk.perkID, perk.seed, perk.isPlayer1Perk, perk.isPerk1);
        }
    }

    [ClientRpc]
    private void Rpc_Reconnection_DestroyAllPerks(bool isPlayer1Perk)
    {
        Perk[] allPerks = FindObjectsByType<Perk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allPerks)
            if (item.isPlayer1Perk == isPlayer1Perk)
            {
                uiManager.HidePerksHighlight(isPlayer1Perk, item.isPerk1);
                Destroy(item.gameObject);
            }
    }

    #endregion
}
