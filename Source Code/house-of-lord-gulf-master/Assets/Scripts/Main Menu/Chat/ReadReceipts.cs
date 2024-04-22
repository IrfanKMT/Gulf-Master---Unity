using Firebase.Database;
using Firebase.Extensions;
using StreamChat.Core.StatefulModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ReadReceipts : MonoBehaviour
{
    public static ReadReceipts instance;
    private static TimeSpan span;

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += SetAllChatOpenToFalse;
    }

    /// <summary>
    /// Call this when client opens a chat , closes a chat and new message received when chat is opened for a client and whenever client send a message
    /// </summary>
    /// <param name="channelID"></param>
    /// <param name="clientID"> self client id</param>
    public static void SetClientLastReadForChannel(string channelID, string clientID)
    {
       // Debug.Log("SetClientLastReadForChannel" + System.DateTime.UtcNow.ToString());
       // Debug.Log("Formt date"+ System.DateTime.UtcNow.ToString("dd-MM-yyyy"));
       // Debug.Log("Formt date" + System.DateTime.UtcNow.ToString("HH:mm:ss"));
        string date = System.DateTime.UtcNow.ToString("dd-MM-yyyy");
        string time = System.DateTime.UtcNow.ToString("HH:mm:ss");

       /* FirebaseDatabase.DefaultInstance.RootReference.Child("ReadReceipts").Child("LastRead").Child(clientID).Child(channelID).SetValueAsync((date+" "+time)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error setting ReadReceipts : " + task.Exception);
            }
        });*/
         FirebaseDatabase.DefaultInstance.RootReference.Child("ReadReceipts").Child("LastRead").Child(clientID).Child(channelID).SetValueAsync(System.DateTime.UtcNow.ToString()).ContinueWithOnMainThread(task =>
         {
             if (task.IsFaulted)
             {
                 Debug.LogError("Error setting ReadReceipts : " + task.Exception);
             }
         });
    }

    /// <summary>
    /// Call whenever a message is rendered 
    /// </summary>
    /// <param name="channelID"> 
    /// channel ID for the chat the message is being rendered for
    /// </param>
    /// <param name="clientID">
    /// Enter the client id of the opposite person
    /// </param>
    /// <returns>boolean true = read false = unread</returns>
    public static void IsMessageReadInChannel(string channelID, string clientID, DateTime utcMsgCreationTime, Action<bool> action)
    {
        FirebaseDatabase.DefaultInstance.GetReference("ReadReceipts").Child("LastRead").Child(clientID).Child(channelID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            DataSnapshot snapshot = DBTask.Result;
            if (snapshot.Value != null)
            {
                string UserLastLoginTime = snapshot.Value.ToString();
                var _UserLastLoginTime = DateTime.Parse(UserLastLoginTime);
                 Debug.Log("UserLastLoginTime"+ UserLastLoginTime);
                 Debug.Log("utcMsgCreationTime"+ utcMsgCreationTime);
                 Debug.Log("(utcMsgCreationTime - _UserLastLoginTime).TotalSeconds"+ (utcMsgCreationTime - _UserLastLoginTime).TotalSeconds);
                Debug.Log((utcMsgCreationTime + "            " + _UserLastLoginTime));
                Debug.Log((utcMsgCreationTime - _UserLastLoginTime).Seconds);
                Debug.Log((utcMsgCreationTime - _UserLastLoginTime).Minutes);

                /*  span = utcMsgCreationTime.Subtract(_UserLastLoginTime);
                  Debug.Log("HH"+ span.Hours +"MM"+ span.Minutes+"SS"+ span.Seconds);
                  if (span.Hours < 0)
                  {
                      action(false);
                      return;
                  }

                  if (span.Minutes < 0)
                  {
                      action(false);
                      return;
                  }
                  if (span.Seconds <= 0)
                  {
                      action(false);
                      return;
                  }

                  action((utcMsgCreationTime - _UserLastLoginTime).TotalSeconds < 2);

                  */
                action((utcMsgCreationTime - _UserLastLoginTime).TotalSeconds < 2);

            }
            else
            {
                Debug.LogError("Value Is Null");
                action(false);
            }
        });

    }

    private static void GetLastReadTime(string channelID, string clientID, Action<DateTime> lastRead)
    {
        FirebaseDatabase.DefaultInstance.GetReference("ReadReceipts").Child("LastRead").Child(clientID).Child(channelID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.Value != null)
            {
                string UserLastLoginTime = snapshot.Value.ToString();

                var _UserLastLoginTime = DateTime.Parse(UserLastLoginTime);
                Debug.Log("GetLastReadTime"+ _UserLastLoginTime);
                lastRead(_UserLastLoginTime);
            }
            else
            {
                lastRead(new DateTime(2000, 10, 10));
            }
        });
    }

    public static void SetChatOpen(bool chatOpen, string clientID, string channelID)
    {
        if (PlayerPrefs.GetInt("GHOST_MODE", 0) == 1) chatOpen = false;

        FirebaseDatabase.DefaultInstance.RootReference.Child("ReadReceipts").Child("ChatOpen").Child(clientID).Child(channelID).SetValueAsync(chatOpen).ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError("Error setting ReadReceipts : " + task.Exception);
            }
        });
    }

    public static void IsChatOpen(string channelID, string clientID, Action<bool> action)
    {
        FirebaseDatabase.DefaultInstance.GetReference("ReadReceipts").Child("ChatOpen").Child(clientID).Child(channelID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            DataSnapshot snapshot = DBTask.Result;
            if (snapshot.Value != null)
            {
                if (bool.TryParse(snapshot.Value.ToString(), out bool chatOpen))
                    action(chatOpen);
                else
                    action(false);
            }
            else
            {
                action(false);
            }
        });

    }

    public static void GetUnreadMessages(List<IStreamMessage> msges, string channelID, string clientID, Action<int> action)
    {
        IsChatOpen(channelID, clientID, (isOpen) =>
        {
            if (isOpen)
            {
                action(0);
            }
            else
            {
                GetLastReadTime(channelID, clientID, lastRead =>
                {
                    int unread = 0;

                    foreach(var msg in msges)
                    {
                        if (msg.User.Id.Equals(clientID)) continue;
                        // Debug.Log("msg.CreatedAt.UtcDateTime"+ msg.CreatedAt.UtcDateTime);
                        // string date =
                        string date = msg.CreatedAt.UtcDateTime.ToString("dd-MM-yyyy");
                        string time = msg.CreatedAt.UtcDateTime.ToString("HH:mm:ss");
//                        Debug.Log(date+" "+ time);
                       // Debug.Log("utc time"+ msg.CreatedAt.UtcDateTime.ToString("dd-MM-yyyy"));
                       // Debug.Log("last read"+ lastRead);
                        bool isRead = (msg.CreatedAt.UtcDateTime - lastRead).TotalSeconds < 2;
                        if (!isRead)
                            unread++;
                    }

                    action(unread);
                });
            }
        });
    }

    private void SetAllChatOpenToFalse()
    {
        FirebaseDatabase.DefaultInstance.GetReference("ReadReceipts").Child("ChatOpen").Child(PlayerData.PlayfabID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                if (snapshot.Value != null)
                {
                    Dictionary<string, object> chatOpens = snapshot.Value as Dictionary<string, object>;
                    foreach (var item in chatOpens)
                        SetChatOpen(false, PlayerData.PlayfabID, item.Key);
                }
            }
        });

    }
}
