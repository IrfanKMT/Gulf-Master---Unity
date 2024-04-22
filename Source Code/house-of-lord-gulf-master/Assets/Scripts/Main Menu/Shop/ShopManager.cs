using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    public ShopItem bronzeShopItem;
    public ShopItem silverShopItem;
    public ShopItem goldShopItem;
    public ShopItem diamondShopItem;
    public ShopItem perksShopItem;

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += CheckAndGrantGift;
    }

    #region In-Game Item Purchases

    public void PurchaseInGameItem(ShopItem item, bool blueCoins)
    {
        // string currencyCode = blueCoins ? "BC" : "PC";
        string currencyCode = "BC";
         int amount = item.price;

        bool canSpend = CurrencyManager.manager.CanSpend(amount, currencyCode);

        if (canSpend)
        {
            CurrencyManager.manager.SubtractUserVirtualCurency(currencyCode, amount, ()=> OnVirtualCurrencySubtracted(item, currencyCode), (error)=> OnSubtractingVirtualCurrencyFailed(item, error));
        }
        else
        {
            OnSubtractingVirtualCurrencyFailed(item, "Insufficient coins.");
        }
    }

    private void OnVirtualCurrencySubtracted(ShopItem item, string currencyCode)
    {
        if(item.payout.IDs.Length>0)
            InventoryManager.manager.GrantItems(item.payout.IDs, (success)=> InGameItemGrantResult(item, success, currencyCode));
    }

    private void OnSubtractingVirtualCurrencyFailed(ShopItem item, string error)
    {
        ShopUIManager.manager.OnPurchaseFailed($"Failed to deduct {item.price} coins.\nError : " + error);
    }

    private void InGameItemGrantResult(ShopItem item, bool success, string currencyCode)
    {
        // string currencyName = currencyCode == "BC" ? "Blue Coins" : "Pink Coins";
        string currencyName = "Blue Coins";
        if (success)
        {
            ShopUIManager.manager.OnPurchaseSuccessful(item, $"You have successfully purchased {item.itemName} for {item.price} {currencyName}.");
        }
        else
        {
            ShopUIManager.manager.OnPurchaseFailed($"Purchasing {item.name} failed, Your {item.price} {currencyName} will be refunded soon.");
            CurrencyManager.manager.AddUserVirtualCurency(currencyCode, item.price, OnVirtualCurrencyRefunded, (err)=>OnVirtualCurrencyRefundFailed(item, err));
        }
    }

    private void OnVirtualCurrencyRefunded()
    {
        Debug.Log("Virtual Currency Refunded");
    }

    private void OnVirtualCurrencyRefundFailed(ShopItem item, string error)
    {
        Debug.LogError($"Virtual Curreny Refund Failed for user {PlayerData.PlayfabID}.\nItem Details : \nName : {item.itemName}\nPrice : {item.price}\nError : " + error);
    }

    #endregion

    #region Coins Purchases

    public static void OnCoinPurchaseSuccessful(string coinCode, int amount)
    {
        // string coinName = coinCode == "BC" ? "Blue Coin" : "Pink Coin";
        string coinName = "Blue Coin";
        CurrencyManager.manager.AddUserVirtualCurency(coinCode, amount, ()=>OnCoinGrantedSuccessfully(coinName, amount), (err)=>OnFailedToGrantCoins(coinName,amount,err));
    }

    private static void OnCoinGrantedSuccessfully(string coinName, int amount)
    {
        IAPManager.manager.UpdatePurchasedItemsStatusToFirebaseDatabase(4);
        //ShopUIManager.manager.OnPurchaseSuccessful($"You have successfully purchased {amount} {coinName}.");
    }

    private static void OnFailedToGrantCoins(string coinName, int amount, string error)
    {
        ShopUIManager.manager.OnPurchaseFailed($"Error while purchasing {amount} {coinName}.\nError : " + error);
        IAPManager.manager.UpdatePurchasedItemsStatusToFirebaseDatabase(3);
    }

    #endregion

    #region Gift

    private void CheckAndGrantGift()
    {
        FirebaseDatabase.DefaultInstance
        .GetReference("DeviceIDs")
        .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Firebase Checking For Gifting Faulted.");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (!snapshot.HasChild(SystemInfo.deviceUniqueIdentifier))
                {
                    InventoryManager.manager.GrantItem("bst_rubberduck");
                    InventoryManager.manager.GrantItem("bst_paintbucket");
                    InventoryManager.manager.GrantItem("bst_firecracker");
                    SaveGifGrantedData();
                    print("Gift Granted");
                }
            }
        });


    }

    void SaveGifGrantedData()
    {
        FirebaseDatabase.DefaultInstance.RootReference
        .Child("DeviceIDs")
        .Child(SystemInfo.deviceUniqueIdentifier)
        .SetRawJsonValueAsync("{\"data\" : true}")
        .ContinueWith(task =>
        {
            if (task.IsCompleted)
                print("Firebase Gifting Data Saved");
            else
                Debug.LogError("Firebase Gifting Data Save Error");
        });
    }

    #endregion

    #region Special Offer

    /// <summary>
    /// Call this function after the user has bought special offer pack.
    /// </summary>
    public static void SaveSpecialOfferRecievedData()
    {
        List<BoosterAndPerkItem> items = new();
        items.AddRange(BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(ItemGroup.BronzeBoosters).ToList());
        items.AddRange(BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(ItemGroup.BronzeBoosters).ToList());
        items.AddRange(BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(ItemGroup.SilverBoosters).ToList());
        items.AddRange(BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(ItemGroup.SilverBoosters).ToList());
        items.AddRange(BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(ItemGroup.GoldBoosters).ToList());

        List<string> boosterIDs = new();
        foreach (var item in items)
            boosterIDs.Add(item.itemId);

        InventoryManager.manager.GrantItems(boosterIDs.ToArray(), GrantingSpecialOfferResult);
        UIAnimationManager.manager.PopDownPanel(ShopUIManager.manager.specialOfferWindow);
    }

    private static void GrantingSpecialOfferResult(bool success)
    {
        if (success)
        {
            FirebaseDatabase.DefaultInstance.RootReference.Child("SpecialOfferData").Child(SystemInfo.deviceUniqueIdentifier).SetRawJsonValueAsync("{\"data\" : true}").ContinueWith(task => { if (task.IsCompleted) print("Special Offer Data Recieved Saved"); else Debug.LogError("Special Offer Data Recieved Saving Error"); });
            ShopUIManager.manager.OnPurchaseSuccessful("You have successfully purchased the special offer pack.");
        }
        else
        {
            ShopUIManager.manager.OnPurchaseFailed("Unable to purchase special booster item, Please contact development team.");
        }
    }

    public static async Task<bool> IsSpecialOfferRecieved()
    {
        return await FirebaseDatabase.DefaultInstance.GetReference("SpecialOfferData").GetValueAsync().ContinueWithOnMainThread((task) => IsSpecialOfferRecievedConinuation(task));
    }

    static bool IsSpecialOfferRecievedConinuation(Task<DataSnapshot> task)
    {
        if (task.IsFaulted)
        {
            Debug.LogError("Special Offer Data Recieveing Error.");
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.HasChild(SystemInfo.deviceUniqueIdentifier))
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}
