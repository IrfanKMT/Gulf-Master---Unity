    using TMPro;
using System;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using PlayFab.ServerModels;
using System.Collections.Generic;

public class GiftingManager : MonoBehaviour
{
    [Header("Gifting Panel")]
    [SerializeField] Button sendGiftBtn;
    [SerializeField] GameObject sendGiftPanel;
    [SerializeField] TMP_InputField amountTF;
    [SerializeField] TMP_InputField friendIDTF;
    [SerializeField] TMP_Text sendGiftErrorText;

    [Header("Senders And Receivers List")]
    [SerializeField] int maxGiftSenderListCount = 10;
    [SerializeField] GameObject giftSendersListPanel;
    [SerializeField] Transform giftSendersListHolder;
    [SerializeField] GiftSenderListItem giftSenderListItem;
    [SerializeField] TMP_Text unseenGiftsText;

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += UpdateSenderList;
        NotificationManager.manager.OnFirebaseNotificationReceived += OnFirebaseNotificationReceived;

        ProfileManager.manager.OnLoadingNewProfileOrUpdatingProfile += (playfabID) =>
        {
            friendIDTF.text = "";
            bool isLocalPlayer = PlayerData.PlayfabID.Equals( playfabID);

            if (!isLocalPlayer)
            {
                var getUsernameRequest = new GetUserAccountInfoRequest{PlayFabId = playfabID};
                PlayFabServerAPI.GetUserAccountInfo(getUsernameRequest, res =>
                {
                    string username = res.UserInfo.TitleInfo.DisplayName[0..^4] + "#" + res.UserInfo.TitleInfo.DisplayName.Remove(0, res.UserInfo.TitleInfo.DisplayName.Length - 4);
                    friendIDTF.text = username;
                },
                err => Debug.LogError("Gifting Manager : Error in fetching username from playfab ID. \nError Message : " + err.ErrorMessage + "\nError Report : " + err.GenerateErrorReport()));
            }
        };
    }

    #region Notification Handlers

    private void OnFirebaseNotificationReceived(Firebase.Messaging.MessageReceivedEventArgs e)
    {
        if (e.Message.Notification.Title.Equals(NotificationCodes.GiftingManager_UpdateGiftSenderList))
            UpdateSenderList();
    }

    #endregion

    #region Update Gift Sender's List

    private void UpdateSenderList()
    {
        var getSenderListRequest = new GetUserDataRequest
        {
            PlayFabId = PlayerData.PlayfabID,
            Keys = new List<string> { PlayfabDataKeys.PlayerGiftSenderList }
        };

        PlayFabServerAPI.GetUserData(getSenderListRequest, res =>
        {
            if (!res.Data.ContainsKey(PlayfabDataKeys.PlayerGiftSenderList)) return;

            string jsonData = res.Data[PlayfabDataKeys.PlayerGiftSenderList].Value;
            Debug.Log("PlayerGiftSenderList Json "+ jsonData);
            PlayfabSendGiftData senderList = JsonConvert.DeserializeObject<PlayfabSendGiftData>(jsonData);

            foreach (Transform child in giftSendersListHolder)
                Destroy(child.gameObject);

            if(senderList.sentGiftDatas!=null)
                foreach (SendGiftData data in senderList.sentGiftDatas)
                {
                   // Debug.Log("data.date"+ data.sentDate);
                    Instantiate(giftSenderListItem, giftSendersListHolder).GetComponent<GiftSenderListItem>().Setup(data.playfabID, data.amount.ToString(), data.sentDate);
                }

            int unseenGifts = 0;

            if (senderList.sentGiftDatas != null)
            {
                Debug.Log("Last Seen Data"+ senderList.lastSeenDate);
                if (senderList.lastSeenDate != null)
                {
                    if (DateTime.TryParse(senderList.lastSeenDate, out DateTime lastSeenData))
                    {
                        foreach (var gift in senderList.sentGiftDatas)
                            if (DateTime.TryParse(gift.sentDate, out DateTime sentDate))
                            {
                                // Debug.Log("sentDate"+ sentDate+ "  "+ "lastSeenData"+ lastSeenData);
                                // Debug.Log(sentDate > lastSeenData);
                                if (sentDate > lastSeenData || lastSeenData == null)
                                    unseenGifts++;
                            }
                    }
                }else
                {
                    unseenGifts++;
                }
                            
            }
            unseenGiftsText.text = unseenGifts.ToString();

        }, err => Debug.LogError("Error In Fetching Local User's Sender List :\nPlayfab ID : " + PlayerData.PlayfabID + "\nError Message : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));
    }

    #endregion

    #region Button Clicks

    public void OnClick_OpenGiftSendersList()
    {
        UIAnimationManager.manager.PopUpPanel(giftSendersListPanel);
        UpdateLocalPlayersLastSeenTime();
        unseenGiftsText.text = "0";
    }

    public void OnClick_CloseGiftSendersList()
    {
        UIAnimationManager.manager.PopDownPanel(giftSendersListPanel);
    }

    public void OnClick_GiftUser()
    {
        UIAnimationManager.manager.PopUpPanel(sendGiftPanel);
    }

    public void OnClick_CloseSendGiftPanel()
    {
        UIAnimationManager.manager.PopDownPanel(sendGiftPanel);
    }

    public void OnClick_SendGiftButton()
    {
        if (string.IsNullOrEmpty(friendIDTF.text))
        {
            sendGiftErrorText.text = "Please enter a valid friend ID";
            return;
        }

        if (int.TryParse(amountTF.text, out int amt))
        {
            SendGiftToFriend(amt, friendIDTF.text);   
        }
        else
        {
            sendGiftErrorText.text = "Please enter a valid amount";
        }
    }

    #endregion

    #region Sending Gifts

    private async void SendGiftToFriend(int amt, string friendID)
    {
        string friendPlayfabID = await FriendIDReferenceManager.GetPlayfabIDWithFriendID(friendID);
        Debug.Log("SendGiftToFriend"+amt+" "+friendID+ friendPlayfabID);
        CurrencyManager.manager.SendVirtualCurrency(amt, friendPlayfabID, () =>
        {
            // Update Friend's Playfab DB
            Debug.Log("dd");
            var getSenderListOfFriendRequest = new GetUserDataRequest
            {
                PlayFabId = friendPlayfabID,
                Keys = new List<string> { PlayfabDataKeys.PlayerGiftSenderList }
            };
            Debug.Log("aa");
            PlayFabServerAPI.GetUserData(getSenderListOfFriendRequest, res =>
            {
                PlayfabSendGiftData friendSenderData = new();

                if (res.Data.ContainsKey(PlayfabDataKeys.PlayerGiftSenderList))
                {
                    string jsonData = res.Data[PlayfabDataKeys.PlayerGiftSenderList].Value;
                    friendSenderData = JsonConvert.DeserializeObject< PlayfabSendGiftData > (jsonData);
                }
                if(friendSenderData.sentGiftDatas!=null)
                {
                    Debug.Log(friendSenderData.sentGiftDatas.Count);

                }
                else
                {
                    Debug.Log("List Null");
                }
            
                if (friendSenderData.sentGiftDatas != null)
                {
                    if(friendSenderData.sentGiftDatas.Count > maxGiftSenderListCount)
                    {
                        friendSenderData.sentGiftDatas.RemoveAt(0);
                    }
                }
                else
                {
                    friendSenderData.sentGiftDatas = new List<SendGiftData>();
                }
                   

                //  friendSenderData.sentGiftDatas.Add(new() { amount = amt, playfabID = PlayerData.PlayfabID, sentDate = DateTime.UtcNow.ToString() });
                  friendSenderData.sentGiftDatas.Add(new() { amount = amt, playfabID = PlayerData.PlayfabID, sentDate = TimeFormat.GetFormattedUTCDate() });

                string updatedJsonData = JsonConvert.SerializeObject(friendSenderData);
                Debug.Log("updatedJsonData"+ updatedJsonData);
                var addUserToSenderListRequest = new UpdateUserDataRequest
                {
                    PlayFabId = friendPlayfabID,
                    Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerGiftSenderList, updatedJsonData } }
                };

                PlayFabServerAPI.UpdateUserData(addUserToSenderListRequest, (res) =>
                {
                    Debug.Log("Sender's List Updated In Friend's ID");
                    NotificationManager.manager.SendNotification(friendPlayfabID, NotificationCodes.GiftingManager_UpdateGiftSenderList, "");

                }, (err) => Debug.LogError("Error Updating Sender List in Friend's Playfab ID :\nFriend Playfab ID : " + friendPlayfabID + "\nMy Playfab ID : " + PlayerData.PlayfabID + "\nError Message : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));

            }, err => Debug.LogError("Error Fetching Sender List from Friend's Playfab ID :\nFriend Playfab ID : " + friendPlayfabID + "\nError Message : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));

            // Update Local User playfab DB Last Seen Time
            UpdateLocalPlayersLastSeenTime();

            sendGiftErrorText.text = "Sent Gift";
        },
        err =>
        {
            sendGiftErrorText.text = err;
        });
    }

    private void UpdateLocalPlayersLastSeenTime()
    {
        var updateLastSeenDateRequest = new GetUserDataRequest
        {
            PlayFabId = PlayerData.PlayfabID,
            Keys = new List<string> { PlayfabDataKeys.PlayerGiftSenderList }
        };
        PlayFabServerAPI.GetUserData(updateLastSeenDateRequest, res =>
        {
            //  PlayfabSendGiftData friendSenderData = new() { sentGiftDatas = new() { }, lastSeenDate = DateTime.UtcNow.ToString()};
           // Debug.Log("UTC"+ TimeFormat.GetFormattedUTCDate());
            PlayfabSendGiftData friendSenderData = new() { sentGiftDatas = new() { }, lastSeenDate = TimeFormat.GetFormattedUTCDate() };

            if (res.Data.ContainsKey(PlayfabDataKeys.PlayerGiftSenderList))
            {
                string jsonData = res.Data[PlayfabDataKeys.PlayerGiftSenderList].Value;
                friendSenderData = JsonConvert.DeserializeObject<PlayfabSendGiftData>(jsonData);
            }
           // Debug.Log("UTC" + TimeFormat.GetFormattedUTCDate());
            friendSenderData.lastSeenDate = TimeFormat.GetFormattedUTCDate();//DateTime.UtcNow.ToString();
            string updatedJsonData = JsonConvert.SerializeObject(friendSenderData);

            var addUserToSenderListRequest = new UpdateUserDataRequest
            {
                PlayFabId = PlayerData.PlayfabID,
                Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerGiftSenderList, updatedJsonData } }
            };

            PlayFabServerAPI.UpdateUserData(addUserToSenderListRequest, (res) =>{},
            (err) => Debug.LogError("Error Updating Last Gift Seen Data :\nMy Playfab ID : " + PlayerData.PlayfabID + "\nError Message : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));

        }, err => Debug.LogError("Error Fetching Sender List Data from Local Player's Playfab ID :\nError Message : " + err.ErrorMessage + "\nReport: " + err.GenerateErrorReport()));

    }

    #endregion
}

[Serializable]
struct PlayfabSendGiftData
{
    public string lastSeenDate;
    public List<SendGiftData> sentGiftDatas;
}

[Serializable]
struct SendGiftData
{
    public string playfabID;
    public int amount;
    public string sentDate;
}