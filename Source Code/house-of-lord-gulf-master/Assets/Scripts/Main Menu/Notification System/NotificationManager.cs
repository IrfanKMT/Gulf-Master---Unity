using System;
using System.Collections;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using Firebase.Messaging;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Firebase;
using static System.Net.WebRequestMethods;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager manager;
    public event Action<MessageReceivedEventArgs> OnFirebaseNotificationReceived;
   // private const string endpointURL = "https://gulfmaster-notificationsystem.alienezy.repl.co/SendNotification";
    private const string endpointURL = "http://209.182.213.242/~mobile/gulf-master/fcm-notification.php";
   // private const string endpointURL = "http://209.182.213.242/~mobile/gulf-master/fcm-notification-extra.php";
    public string m_playfabid;
    public string m_notification_code;
    public string m_meassage;
    private FirebaseApp app;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            SendNotification(m_playfabid, m_notification_code, m_meassage);
        }
    }
#endif

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += GetTokenAsync;
        AuthenticationManager.manager.OnPlayerLoggedOut += () => SaveToken("");
        FirebaseMessaging.MessageReceived += OnMessageReceived;
    }

    #endregion

    #region Tokens

    private async void GetTokenAsync()
    {
        var task = FirebaseMessaging.GetTokenAsync();

        await task;

        if (task.IsCompleted)
        {
            StartCoroutine(SaveToken(task.Result));
        }
        else
        {
            Debug.LogError("Unable to get the token for the device. \nError : " + task.Exception + "\nStack Trace : " + task.Exception.StackTrace);
        }
    }

    private IEnumerator SaveToken(string token)
    {
        Debug.Log("Token"+token);
        yield return new WaitWhile(() => string.IsNullOrEmpty(PlayerData.PlayfabID));
        var DBTask = FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(PlayerData.PlayfabID).Child("Token").SetValueAsync(token).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning(task.Exception);
            }
        });
    }

    private void GetUserToken(string UserID, Action<string> callback)
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;

        dbInstance.GetReference("users").Child(UserID).Child("Token").GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError(DBTask.Exception);
                callback(null);
            }

            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                if (snapshot != null)
                {
                    var token = (string)(snapshot.Value);
                    Debug.Log(token);
                    callback(token);
                }
                else
                {
                    callback(null);
                }
            }
        });


    }

    #endregion

    #region Notifications

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        OnFirebaseNotificationReceived?.Invoke(e);
    }

    public void SendNotification(string playfabID, string NotificationCode, string description)
    {
        GetUserToken(playfabID, async (token) =>
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("Notification Token Not Found For playfab ID : " + playfabID);
                return;
            }

            await SendPostRequest(token, NotificationCode, description);
        });
    }

    private async Task SendPostRequest(string token, string NotificationCode, string desc)
    {
        Debug.Log("Send post Token"+token);
        WWWForm formData = new();


          formData.AddField("token", token);
         // formData.AddField("Title", NotificationCode);
         // formData.AddField("Desc", desc);
          formData.AddField("title", NotificationCode);
          formData.AddField("desc", desc);

      /*  formData.AddField("token", token);
        formData.AddField("desc", desc);

        switch (NotificationCode)
        {
            case "1":
                break;
            case "2":
                //formData.AddField("title", PlayerData.PlayfabID+ " Opens Chat");
                break;
            case "3":
                formData.AddField("title", PlayerData.Username + " Opens Chat");
                break;
            case "4":
                break;
            case "5":
                break;
            case "6":
                break;
        }
        formData.AddField("data", "data");
      */
        using UnityWebRequest request = UnityWebRequest.Post(endpointURL, formData);
        var asyncOperation = request.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Post request failed. Error: " + request.error);
        }
        else
        {
            Debug.Log("post response"+request.downloadHandler.text);
        }
    }

    #endregion
}
