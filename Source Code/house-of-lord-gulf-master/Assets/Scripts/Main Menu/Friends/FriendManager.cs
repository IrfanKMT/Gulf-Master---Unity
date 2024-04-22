using System;
using PlayFab;
using UnityEngine;
using Newtonsoft.Json;
using Firebase.Database;
using Firebase.Extensions;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class FriendManager : MonoBehaviour
{
    public static FriendManager manager;
    public event Action<List<string>> OnFriendListUpdated;
    public event Action<List<string>> OnRequestsListUpdated;
    public event Action<List<string>> OnBlockListUpdated;
    public event Action<List<string>> OnRecentListUpdated;

    internal List<string> friendList;
    public DatabaseReference DBref;
    [SerializeField] FriendUIManager uiManager;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
        DBref = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += () => LoadAllLists();
        NotificationManager.manager.OnFirebaseNotificationReceived += Manager_OnFirebaseNotificationReceived;
        //GameNetworkManager.manager.OnOpponentFound += (otherPlayer) => AddRecentListItem(otherPlayer);
    }

    #endregion

    #region Load Lists

    public void LoadAllLists()
    {
        LoadRecentListData();
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("users").Child(PlayerData.PlayfabID).Child("Friends").GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while loading friends list : \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }

            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                List<string> friends = new();
                List<string> requests = new();
                List<string> blocks = new();
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                    if ((string)childSnapshot.Value == "true") friends.Add(childSnapshot.Key);
                    else if ((string)childSnapshot.Value == "pending") requests.Add(childSnapshot.Key);
                    else if ((string)childSnapshot.Value == "Blocked") blocks.Add(childSnapshot.Key);

                friendList = friends;
                OnRequestsListUpdated?.Invoke(requests);
                OnBlockListUpdated?.Invoke(blocks);
                OnFriendListUpdated?.Invoke(friends);
            }
        });
    }

    #endregion

    #region Friend Request System

    public async void SendFriendRequest(string friendID)
    {
        string playfabID = await FriendIDReferenceManager.GetPlayfabIDWithFriendID(friendID);
        Debug.Log("SendFriendRequest playfab ID"+ playfabID);
        CheckForFriendRequestStatus(playfabID, (requestsAllowed) =>
        {
            if (requestsAllowed)
            {
                if (!string.IsNullOrEmpty(playfabID) && !playfabID.Equals(PlayerData.PlayfabID))
                {
                    CheckForBlockAndSend(playfabID, (value) =>
                    {
                        if (value)
                        {
                            var DBTask = DBref.Child("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).SetValueAsync("pending").ContinueWithOnMainThread((task) =>
                            {
                                if (task.IsFaulted)
                                {
                                    uiManager.addFriend_errorTxt.text = "Some Error Occured";
                                    Debug.LogError("Error while sending friend request [Our side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                                }
                                else
                                {
                                    uiManager.addFriend_errorTxt.text = "Sent!";
                                    SendNotification_UpdateList(playfabID);
                                    Debug.Log("Friend Request Send to User : \nPlayfab ID Of User : " + playfabID);
                                }
                            });
                        }
                        else
                        {
                            uiManager.addFriend_errorTxt.text = "You have been blocked by the user";
                            Debug.LogError("Error while sending friend request : \nUser Is BLocked");
                        }
                    });
                }
                else
                {
                    uiManager.addFriend_errorTxt.text = "User Not Found";
                    Debug.LogError("Error while sending friend request : \nPlayfab ID cant be found with friend ID : " + friendID);
                }
            }
            else
            {
                uiManager.addFriend_errorTxt.text = "Friend Request Status Closed";
                Debug.Log("Error while sending friend request : Friend Request Status Closed");
            }
        });
    }

    public void SendFriendRequestViaPlayfab(string playfabID, Action OnSuccess = null, Action OnError = null)
    {
        Debug.Log("SendFriendRequestViaPlayfab PlayfabID"+ playfabID);
        if (!string.IsNullOrEmpty(playfabID) && !playfabID.Equals(PlayerData.PlayfabID))
        {
            if (friendList.Contains(playfabID)) return;

            CheckForFriendRequestStatus(playfabID, (requestsAllowed) =>
            {
                if (requestsAllowed)
                {
                    CheckForBlockAndSend(playfabID, (value) =>
                    {
                        if (value)
                        {
                            var DBTask = DBref.Child("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).SetValueAsync("pending").ContinueWithOnMainThread((task) =>
                            {
                                if (task.IsFaulted)
                                {
                                    uiManager.addFriend_errorTxt.text = "Some Error Occured";
                                    Debug.LogError("Error while sending friend request [Our side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                                }
                                else
                                {
                                    if (OnSuccess != null)
                                        OnSuccess?.Invoke();
                                    uiManager.addFriend_errorTxt.text = "Sent!";
                                    Debug.Log("Friend Request Send to User : \nPlayfab ID Of User : " + playfabID);
                                    SendNotification_UpdateList(playfabID);
                                    return;
                                }
                            });
                        }
                        else
                        {
                            uiManager.addFriend_errorTxt.text = "You have been blocked by the user";
                            Debug.LogError("Error while sending friend request : \nUser Is BLocked");
                        }
                    });
                }
                else
                {
                    uiManager.addFriend_errorTxt.text = "Friend Request Status Closed";
                    Debug.Log("Error while sending friend request : Friend Request Status Closed");
                }
            });
        }
        else
        {
            uiManager.addFriend_errorTxt.text = "User Not Found";
            Debug.LogError("Error while sending friend request : \nPlayfab ID is null");
        }
        if (OnError != null)
            OnError?.Invoke();
    }

    public void AcceptFriendRequest(string playfabID)
    {
        var DBTask = DBref.Child("users").Child(PlayerData.PlayfabID).Child("Friends").Child(playfabID).SetValueAsync("true").ContinueWithOnMainThread((task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while accepting friend request [Our Side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                return;
            }
        });

        var DBTaskTwo = DBref.Child("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).SetValueAsync("true").ContinueWithOnMainThread((task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while accepting friend request [Friend's Side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                return;
            }
        });
        LoadAllLists();
        SendNotification_UpdateList(playfabID);
    }

    public void RejectFriendRequest(string playfabID)
    {
        var DBTask = DBref.Child("users").Child(PlayerData.PlayfabID).Child("Friends").Child(playfabID).SetValueAsync(null).ContinueWithOnMainThread((task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while rejecting friend request [Our Side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                return;
            }
        });

        var DBTaskTwo = DBref.Child("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).SetValueAsync(null).ContinueWithOnMainThread((task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while rejecting friend request [Friend's Side]: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
                return;
            }
        });

        LoadAllLists();
        SendNotification_UpdateList(playfabID);
    }

    #endregion

    #region Friend Request Accepting Status

    public void ToggleRequestStatus(bool status, string playfabID)
    {
        var DBTask = DBref.Child("users").Child(playfabID).Child("RequestStatus").SetValueAsync(status?"true":"false").ContinueWithOnMainThread((task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while toggling request status: \nError : " + task.Exception);
                return;
            }
            else
            {
                Debug.Log("Successfully Updated Request Status");
            }
        });
    }

    public void CheckForFriendRequestStatus(string playfabID, Action<bool> callback)
    {
        print(playfabID);
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("users").Child(playfabID).Child("RequestStatus").GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while checking for friend request status: \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }

            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;

                if ((string)snapshot.Value == "False")
                {
                    callback(false);
                }
                else
                {
                    callback(true);
                }
            }
        });
    }

    #endregion

    #region Block System

    private void CheckForBlockAndSend(string playfabID, Action<bool> callback)
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while checking for blocked friend: \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }

            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;

                if ((string)snapshot.Value == "Blocked")
                {
                    callback(false);
                }
                else
                {
                    callback(true);
                }
            }
        });
    }

    public void BlockUser(string playfabID)
    {
        var DBTask = DBref.Child("users").Child(PlayerData.PlayfabID).Child("Friends").Child(playfabID).SetValueAsync("Blocked").ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while blocking user: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            }
            else
            {
                var DBTask2 = DBref.Child("users").Child(playfabID).Child("Friends").Child(PlayerData.PlayfabID).SetValueAsync(null).ContinueWithOnMainThread(task2 =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Error while blocking user: \nError : " + task2.Exception + "\nError Message : " + task2.Exception.Message + "\n\nStack Trace : " + task2.Exception.StackTrace);
                    }
                    else
                    {
                        Debug.Log("User Blocked" + playfabID);
                        FriendListsManager.manager.RemovePlayfabIDFromAllListData(playfabID);
                        LoadAllLists();
                        SendNotification_UpdateList(playfabID);
                    }
                });
            }
        });
    }

    public void UnblockUser(string playfabID)
    {
        var DBTask = DBref.Child("users").Child(PlayerData.PlayfabID).Child("Friends").Child(playfabID).SetValueAsync("false").ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while unblocking user: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            }
            else
            {
                Debug.Log("User UnBlocked" + playfabID);
                LoadAllLists();
            }
        });
    }

    #endregion

    #region Recents

    // Called Automatically From Events
    private void AddRecentListItem(string playfabID)
    {
        var updateRecentsList = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.PlayerRecentListData }
        };

        PlayFabClientAPI.GetUserData(updateRecentsList, res => AddRecent_OnRecentListFetched(res, playfabID), OnFetchingRecentListFailed);
    }

    private void AddRecent_OnRecentListFetched(GetUserDataResult result, string playfabIDToAdd)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerRecentListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.PlayerRecentListData].Value;
            List<string> recentIDs = JsonConvert.DeserializeObject<List<string>>(jsonData);

            if (recentIDs.Contains(playfabIDToAdd)) return;

            recentIDs.Add(playfabIDToAdd);
            if (recentIDs.Count > 10)
                recentIDs.RemoveAt(0);
            print(recentIDs.Count);
            string updatedJsonData = JsonConvert.SerializeObject(recentIDs);
            print(updatedJsonData);
            var updateRecentListRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerRecentListData, updatedJsonData } }
            };
            PlayFabClientAPI.UpdateUserData(updateRecentListRequest, AddRecent_OnRecentsListUpdated, AddRecent_OnUpdatingRecentListFailed);
        }
        else
        {
            List<string> recentIDs = new() { playfabIDToAdd };
            string updatedJsonData = JsonConvert.SerializeObject(recentIDs);
            var updateRecentListRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerRecentListData, updatedJsonData } }
            };
            PlayFabClientAPI.UpdateUserData(updateRecentListRequest, AddRecent_OnRecentsListUpdated, AddRecent_OnUpdatingRecentListFailed);
        }
    }

    private void AddRecent_OnRecentsListUpdated(UpdateUserDataResult result)
    {
        LoadRecentListData();
        Debug.Log("Recent List Updated");
    }

    private void AddRecent_OnUpdatingRecentListFailed(PlayFabError error)
    {
        Debug.LogError($"Error while updating recents list : \nError Message : {error.ErrorMessage}:\nError Report : {error.GenerateErrorReport()}");
    }

    private void LoadRecentListData()
    {
        var updateRecentListRequest = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.PlayerRecentListData }
        };
        PlayFabClientAPI.GetUserData(updateRecentListRequest, OnRecentListDataLoaded, OnFetchingRecentListFailed);
    }

    private void OnRecentListDataLoaded(GetUserDataResult result)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerRecentListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.PlayerRecentListData].Value;
            List<string> recentIDs = JsonConvert.DeserializeObject<List<string>>(jsonData);
            if (recentIDs.Count > 0)
                OnRecentListUpdated?.Invoke(recentIDs);
        }
    }

    private void OnFetchingRecentListFailed(PlayFabError error)
    {
        Debug.LogError($"Error while fetching recents list : \nError Message : {error.ErrorMessage}:\nError Report : {error.GenerateErrorReport()}");
    }

    #endregion

    #region Deleted Message List Items

    public void AddDeletedMessageListItem(string playfabID)
    {
        var DBTask = DBref.Child("DeletedMessages").Child(PlayerData.PlayfabID).Child(playfabID).SetValueAsync(true).ContinueWithOnMainThread((task) => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while deleting message list item : \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            }
        });
    }

    public void RemoveMyDeletedMessageListItem(string playfabID)
    {
        var DBTask = DBref.Child("DeletedMessages").Child(PlayerData.PlayfabID).Child(playfabID).SetValueAsync(false).ContinueWithOnMainThread((task) => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while removing deleted message list item from deleted messages list : \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            }
        });
    }

    public void RemoveOtherDeletedMessageListItem(string playfabID)
    {
        var DBTask = DBref.Child("DeletedMessages").Child(playfabID).Child(PlayerData.PlayfabID).SetValueAsync(false).ContinueWithOnMainThread((task) => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error while removing deleted message list item from deleted messages list : \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            }
            else
            {
                SendNotification_UpdatingMessageList(playfabID);
            }
        });
    }

    public void FetchMyDeletedMessageListItem(Action<Dictionary<string, object>> callback)
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("DeletedMessages").Child(PlayerData.PlayfabID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while loading friends list : \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }
            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                if (snapshot.Value != null)
                {
                    Dictionary<string, object> deletedMsgList = snapshot.Value as Dictionary<string, object>;
                    callback(deletedMsgList);
                }
                else
                {
                    callback(new Dictionary<string, object>());
                }
            }
        });
    }

    public void CheckOtherDeletedMessageListItem(string playfabID, Action<bool> callback)
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("DeletedMessages").Child(playfabID).Child(PlayerData.PlayfabID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while loading friends list : \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }
            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                if (snapshot.Value != null)
                {
                    callback((bool)snapshot.Value);
                }
                else
                {
                    callback(false);
                }
            }
        });
    }

    #endregion

    #region Notification Handler

    private void SendNotification_UpdateList(string playfabID)
    {
        NotificationManager.manager.SendNotification(playfabID, NotificationCodes.FriendManager_UpdateList, "");
    }

    private void SendNotification_UpdatingMessageList(string playfabID)
    {
        NotificationManager.manager.SendNotification(playfabID, NotificationCodes.FriendManager_UpdateMessageList, "");
    }

    private void Manager_OnFirebaseNotificationReceived(Firebase.Messaging.MessageReceivedEventArgs e)
    {
        if (e.Message.Notification.Title.Equals(NotificationCodes.FriendManager_UpdateList))
            LoadAllLists();

        if (e.Message.Notification.Title.Equals(NotificationCodes.FriendManager_UpdateMessageList))
            ChatManager.manager.LoadAllChannels();
    }

    #endregion
}