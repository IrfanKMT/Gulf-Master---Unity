using PlayFab.ClientModels;
using PlayFab;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager manager;
    public static string BlueCoinIDPrefix = "BC_";

    [SerializeField] TMP_Text[] blueCoinTxts;
  //  [SerializeField] TMP_Text[] pinkCoinTxts;

    Dictionary<string, int> VirtualCurrency = new();

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        InventoryManager.manager.OnInventoryUpdated += OnInventoryUpdated;
        NotificationManager.manager.OnFirebaseNotificationReceived += OnFirebaseNotificationReceived;
    }

   

    #endregion

    private void OnInventoryUpdated(GetUserInventoryResult res)
    {
        VirtualCurrency = res.VirtualCurrency;
        int blueCoins = VirtualCurrency["BC"];
       // int pinkCoins = VirtualCurrency["PC"];

        foreach (var blueCoinTxt in blueCoinTxts)
            blueCoinTxt.text = blueCoins.ToString();

       // foreach (var pinkCoinTxt in pinkCoinTxts)
          //  pinkCoinTxt.text = pinkCoins.ToString();
    }

    /// <summary>
    /// Logs The Error
    /// </summary>
    public void AddUserVirtualCurency(string virtualCurrency, int amount, Action onSuccess, Action<string> onError)
    {
        Debug.Log("Adding Virtual Currency : " + virtualCurrency + " | Amount : " + amount);
        if (VirtualCurrency.ContainsKey(virtualCurrency))
        {
            var request = new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = virtualCurrency,
                Amount = amount
            };

            PlayFabClientAPI.AddUserVirtualCurrency(request, (res) =>
            {
                InventoryManager.manager.UpdateUserInventory();
                onSuccess?.Invoke();
            },
            err =>
            {
                onError?.Invoke(err.GenerateErrorReport());
                Debug.LogError("Error Adding Virtual Currency : Error - " + err.GenerateErrorReport());
            });
        }
        else
        {
            onError?.Invoke($"Virtual Currency Code Invalid : [{virtualCurrency}]");
            Debug.LogError($"Error Adding Virtual Currency : \nVirtual Currency Code [{virtualCurrency}] is wrong");
        }
    }

    public void SubtractUserVirtualCurency(string virtualCurrency, int amount, Action onSuccess, Action<string> onError)
    {
        if (VirtualCurrency.ContainsKey(virtualCurrency) && amount > 0)
        {
            var request = new SubtractUserVirtualCurrencyRequest
            {
                VirtualCurrency = virtualCurrency,
                Amount = amount
            };
            PlayFabClientAPI.SubtractUserVirtualCurrency(request, (res) =>
            {
                InventoryManager.manager.UpdateUserInventory();
                onSuccess?.Invoke();
            }, err =>
            {
                onError?.Invoke(err.ErrorMessage);
                Debug.LogError("Error Subtracting Virtual Currency : Error - " + err.GenerateErrorReport());
            });
        }
        else
        {
            onError?.Invoke($"Virtual Currency Code[{virtualCurrency}] is wrong or amount[{amount}] is invalid");
            Debug.LogError($"Error Subtracting Virtual Currency : \nVirtual Currency Code [{virtualCurrency}] is wrong or amount [{amount}] is invalid!");
        }
    }

    public bool CanSpend(int amt, string currencyCode)
    {
        if (VirtualCurrency.ContainsKey(currencyCode))
        {
            int coins = VirtualCurrency[currencyCode];

            if (amt <= coins)
            {
                return true;
            }
        }

        return false;
    }

    public int GetVirtualCurrencyAmount(string currencyCode)
    {
        if (VirtualCurrency.ContainsKey(currencyCode))
        {
            return VirtualCurrency[currencyCode];
        }
        return 0;
    }

    public void SendVirtualCurrency(int amount, string playfabID, Action onSuccess, Action<string> onError)
    {
        Debug.Log("Fun Call");
        if (!string.IsNullOrEmpty(playfabID) && GetVirtualCurrencyAmount("BC")>=amount)
        {
            SubtractUserVirtualCurency("BC", amount, () =>
            {
                var request = new PlayFab.ServerModels.AddUserVirtualCurrencyRequest
                {
                    VirtualCurrency = "BC",
                    Amount = amount,
                    PlayFabId = playfabID
                };

                PlayFabServerAPI.AddUserVirtualCurrency(request,
                (res) =>
                {
                    onSuccess();
                    OnUserGifted_SendNotification(playfabID);
                },
                err =>
                {
                    onError(err.ErrorMessage);
                    Debug.LogError("Error Adding Virtual Currency : Error - " + err.GenerateErrorReport());
                    AddUserVirtualCurency("BC", amount, () => { }, (err) => { Debug.LogError("Error In  Refunding The Amount : Error : " + err); });
                });
            },
            err =>
            {
                Debug.LogError("Error Sending Gift. Error : " + err);
                onError(err);
            });
        }
        else
        {
            if(string.IsNullOrEmpty(playfabID))
                onError("Invalid Friend ID.");
            else if(GetVirtualCurrencyAmount("BC") >= amount)
                onError("No Blue Coins Left");
        }
    }

    #region Notification Handler

    private void OnUserGifted_SendNotification(string playfabID)
    {
        Debug.Log("OnUserGifted_SendNotification");
        NotificationManager.manager.SendNotification(playfabID, NotificationCodes.CurrencyManager_CurrencyUpdateCode, "update");
    }

    private void OnFirebaseNotificationReceived(Firebase.Messaging.MessageReceivedEventArgs obj)
    {
        Debug.Log("OnFirebaseNotificationReceived");
        if (obj.Message.Notification.Title.Equals(NotificationCodes.CurrencyManager_CurrencyUpdateCode))
        {
            InventoryManager.manager.UpdateUserInventory();
        }
    }
    #endregion
}