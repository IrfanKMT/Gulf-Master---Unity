using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using System.Collections.Generic;
/// <summary>
/// To Grant Blue Coins, ID : "BC_<any number>". Example : "BC_100"
/// To Grant Random Booster, check ID on BoosterAndPerkData class
/// </summary>

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager manager;
    public event Action<GetUserInventoryResult> OnInventoryUpdated;

    internal Dictionary<string, ItemInstance> userInventory = new();

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += OnPlayerLoggedIn;
    }

    #endregion

    #region Initialize

    private void OnPlayerLoggedIn()
    {
        LoadingManager.manager.UpdateLoadingText("Loading Player Inventory");

        var getUserInventoryRequest = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(getUserInventoryRequest, OnUserInventoryInitialized, OnRecieveInventoryInitializeError);
    }

    private void OnUserInventoryInitialized(GetUserInventoryResult result)
    {
        userInventory.Clear();
        foreach (var item in result.Inventory)
        {
            if (!userInventory.ContainsKey(item.ItemId))
                userInventory.Add(item.ItemId, item);
        }
        OnInventoryUpdated?.Invoke(result);
        LoadingManager.manager.HideLoadingBar(()=>
        {
            GameObject activePanel = UIManager.manager.authenticationPanel.activeInHierarchy ? UIManager.manager.authenticationPanel : UIManager.manager.setPlayerProfilePanel;
            UIManager.manager.OpenPanel(UIManager.manager.lobbyUI,activePanel);
        });
    }

    private void OnRecieveInventoryInitializeError(PlayFabError error)
    {
        Debug.LogError("Error Loading User Inventory.\nError : " + error.GenerateErrorReport());
        LoadingManager.manager.HideLoadingBar();
    }

    #endregion

    #region User Inventory

    public void UpdateUserInventory()
    {
        var getUserInventoryRequest = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(getUserInventoryRequest, OnUserInventoryRecieved, OnRecieveInventoryError);
    }

    private void OnUserInventoryRecieved(GetUserInventoryResult result)
    {
        userInventory.Clear();
        foreach (var item in result.Inventory)
        {
            if (!userInventory.ContainsKey(item.ItemId))
                userInventory.Add(item.ItemId, item);
        }
        OnInventoryUpdated?.Invoke(result);
    }

    private void OnRecieveInventoryError(PlayFabError error)
    {
        Debug.LogError("Error Loading User Inventory.\nError : " + error.GenerateErrorReport());
    }

    #endregion

    #region Grant Item

    public void GrantItem(string itemID, Action<bool> callback)
    {
        var grantItemRequest = new PlayFab.ServerModels.GrantItemsToUserRequest
        {
            ItemIds = new List<string> { itemID },
            PlayFabId = PlayerData.PlayfabID
        };

        PlayFabServerAPI.GrantItemsToUser(grantItemRequest,
            (res) =>
            {
                callback(true);
                OnItemGranted(res.ItemGrantResults);
            },
            (err) =>
            {
                callback(false);
                OnItemGrantError(err);
            });
    }

    public void GrantItem(string itemID)
    {
        if (itemID.Equals(BoosterAndPerksData.randomBoosterId))
        {
            itemID = BoosterAndPerksData.data.GetRandomItemFromBoosters().itemId;
        }
        else if (itemID.StartsWith(CurrencyManager.BlueCoinIDPrefix))
        {
            if (int.TryParse(itemID.Replace(CurrencyManager.BlueCoinIDPrefix, ""), out int amt))
                CurrencyManager.manager.AddUserVirtualCurency("BC", amt, () => { }, err => { });

            return;
        }

        var grantItemRequest = new PlayFab.ServerModels.GrantItemsToUserRequest
        {
            ItemIds = new List<string> { itemID },
            PlayFabId = PlayerData.PlayfabID
        };

        PlayFabServerAPI.GrantItemsToUser(grantItemRequest,
            (res) => OnItemGranted(res.ItemGrantResults),
            (err) => OnItemGrantError(err));
    }

    public void GrantItems(string[] itemIDs, Action<bool> callback)
    {
        List<string> itemsToGrant = new();

        foreach (var item in itemIDs)
            if (item.Equals(BoosterAndPerksData.randomBoosterId))
            {
                string randomBoosterID = BoosterAndPerksData.data.GetRandomItemFromBoosters().itemId;
                itemsToGrant.Add(randomBoosterID);
                print(randomBoosterID);
            }
            else if (item.StartsWith(CurrencyManager.BlueCoinIDPrefix) && int.TryParse(item.Replace(CurrencyManager.BlueCoinIDPrefix, ""), out int amt))
            {
                CurrencyManager.manager.AddUserVirtualCurency("BC", amt, () => { }, err => { });
                print("Granted amt : " + amt);
            }
            else
                itemsToGrant.Add(item);

        var grantItemRequest = new PlayFab.ServerModels.GrantItemsToUserRequest
        {
            ItemIds = itemsToGrant,
            PlayFabId = PlayerData.PlayfabID
        };

        PlayFabServerAPI.GrantItemsToUser(grantItemRequest,
            (res) =>
            {
                callback(true);
                OnItemGranted(res.ItemGrantResults);
            },
            (err) =>
            {
                callback(false);
                OnItemGrantError(err);
            });
    }

    private void OnItemGranted(List<PlayFab.ServerModels.GrantedItemInstance> items)
    {
        UpdateUserInventory();
    }

    private void OnItemGrantError(PlayFabError error)
    {
        Debug.LogError("Error while granting item to user : \nError : " + error.GenerateErrorReport());
    }

    #endregion

    #region Consume Item

    public void ConsumeItem(string itemID, int amount, Action<bool> callback)
    {
        if (userInventory.ContainsKey(itemID) || amount <= 0)
        {
            var consumeItemRequest = new ConsumeItemRequest
            {
                ConsumeCount = amount,
                ItemInstanceId = userInventory[itemID].ItemInstanceId
            };
            PlayFabClientAPI.ConsumeItem(consumeItemRequest,
                (res) =>
                {
                    callback(true);
                    OnItemConsumed(userInventory[itemID]);
                },
                (err) =>
                {
                    callback(false);
                    OnConsumeItemError(err, userInventory[itemID]);
                });
        }
        else
        {
            callback(false);
        }
    }

    private void OnItemConsumed(ItemInstance item)
    {
        UpdateUserInventory();
    }

    private void OnConsumeItemError(PlayFabError error, ItemInstance item)
    {
        Debug.LogError($"Error while consuming item {item.ItemId}\nError : " + error.GenerateErrorReport());
    }

    #endregion

    #region Check Item

    public bool IsItemInInventory(string itemID)
    {
        return userInventory.ContainsKey(itemID);
    }

    #endregion
}
