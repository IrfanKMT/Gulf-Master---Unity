using System.Linq;
using StreamChat.Core;
using StreamChat.Core.QueryBuilders.Filters;
using StreamChat.Core.QueryBuilders.Filters.Users;
using StreamChat.Core.StatefulModels;
using StreamChat.Libs.Auth;
using UnityEngine;
using System.Collections.Generic;
using System;
using StreamChat.Core.Requests;
using System.Threading.Tasks;
using System.IO;
using StreamChat.Core.QueryBuilders.Filters.Channels;
using StreamChat.Core.Helpers;
using UnityEngine.Android;

public class ChatManager : MonoBehaviour
{
    public static ChatManager manager;

    // Message Events
    public event Action<List<IStreamChannel>> OnChannelListUpdated;

    public event Action<IReadOnlyList<IStreamMessage>> OnMessageListUpdated;
    public event Action<IStreamMessage> OnMessageReceived;
    public event Action<IReadOnlyList<IStreamMessage>,string> OnMessageDeleted; //All Messages Excluding the deleted message and ID of the deleted msg

    public event Action<StreamSendMessageRequest> OnLocalMessageSent;

    public event Action<bool> OnOtherUserTypingStatusChanged; //true is local user starts typing, false if other user started typing

    // Chat 
    private IStreamChatClient _chatClient;
    internal IStreamLocalUserData localUserData;
    internal IStreamChannel currentChannel;
    internal string oldChannelID;
    internal bool isInitialized = false;
    internal bool isNativeGallaryOpen = false;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        if(!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
        AuthenticationManager.manager.OnPlayerLoggedIn += ConnectLocalClient;
        AuthenticationManager.manager.OnPlayerLoggedOut += DisconnectLocalClient;
        NotificationManager.manager.OnFirebaseNotificationReceived += OnFirebaseNotificationReceived;
        DeleteCache();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus || (!hasFocus && (TouchScreenKeyboard.visible || isNativeGallaryOpen)))
        {
            if (currentChannel != null)
                ReadReceipts.SetChatOpen(true, PlayerData.PlayfabID, currentChannel.Id);
            SetOnlinePresence(true);
        }
        else
        {
            if (currentChannel != null)
                ReadReceipts.SetChatOpen(false, PlayerData.PlayfabID, currentChannel.Id);
            SetOnlinePresence(false);
        }
    }

    private void OnApplicationQuit()
    {
        if(currentChannel!=null)
            ReadReceipts.SetChatOpen(false, PlayerData.PlayfabID, currentChannel.Id);

        SetOnlinePresence(false);
        if (_chatClient != null)
            DisconnectLocalClient();

    }

    #endregion

    #region Chat Client

    private async void ConnectLocalClient()
    {
        Debug.Log("ConnectLocalClient");
        var userId = StreamChatClient.SanitizeUserId(PlayerData.PlayfabID); // Remove disallowed characters
        var userToken = StreamChatClient.CreateDeveloperAuthToken(userId);
        var credentials = new AuthCredentials("8d7877j3t2yn", userId, userToken);

        _chatClient = StreamChatClient.CreateDefaultClient();
        localUserData = await _chatClient.ConnectUserAsync(credentials);
        SetOnlinePresence(true);
        LoadAllChannels();
        isInitialized = true;
    }

    private async void DisconnectLocalClient()
    {
        await _chatClient.DisconnectUserAsync();
        //Debug.Log($"Local Chat Client disconnected");
        isInitialized = false;
    }

    #endregion 

    #region Channels

    internal async void CreateOrJoinChannelWithFriend(string friendPlayfabID, bool sendNotif = true)
    {
        Debug.Log("CreateOrJoinChannelWithFriend");
        Debug.Log("friendPlayfabID "+ friendPlayfabID);
        SetOnlinePresence(true);
        var filters = new IFieldFilterRule[]{UserFilter.Id.EqualsTo(friendPlayfabID)};
        var users = await _chatClient.QueryUsersAsync(filters);
        Debug.Log("Users "+users);
        var otherUser = users.First();
        var localUser = _chatClient.LocalUserData.User;
        Debug.Log("localUser"+ localUser);

        Dictionary<string, dynamic> channelData = new();
        channelData.Add("message_limit", 100);
        LeaveChatWithFriend();
        Debug.Log("async start");
        currentChannel = await _chatClient.GetOrCreateChannelWithMembersAsync(ChannelType.Messaging, new[] { localUser, otherUser }, channelData);
        await currentChannel.ShowAsync();
        _ = currentChannel.MarkChannelReadAsync();
        Debug.Log("async done");
        currentChannel.MessageReceived += Callback_OnMessageReceived;
        currentChannel.MessageDeleted += Callback_OnMessageDeleted;
        currentChannel.UserStartedTyping += Callback_UserStartedTyping;
        currentChannel.UserStoppedTyping += Callback_UserStoppedTyping;
        FriendManager.manager.RemoveMyDeletedMessageListItem(friendPlayfabID);

        ReadReceipts.SetChatOpen(true, PlayerData.PlayfabID, currentChannel.Id);
        LoadAllChannels();
        SetLastRead();
        SendNotificationForUpdatingMessageList();
        if (sendNotif)
            SendNotificationForOpeningChannel();

        LoadMessages(currentChannel.Messages);
        oldChannelID = friendPlayfabID;
    }

    internal void LeaveChatWithFriend()
    {
        //Debug.Log("LeaveChatWithFriend");
        if (currentChannel == null)
            return;

        SetLastRead();
        ReadReceipts.SetChatOpen(false, PlayerData.PlayfabID, currentChannel.Id);
        currentChannel.MessageReceived -= Callback_OnMessageReceived;
        currentChannel.MessageDeleted -= Callback_OnMessageDeleted;
        currentChannel.UserStartedTyping -= Callback_UserStartedTyping;
        currentChannel.UserStoppedTyping -= Callback_UserStoppedTyping;
        if(currentChannel.Messages.Count>0)
            currentChannel.Messages.Last().MarkMessageAsLastReadAsync();
        currentChannel = null;
    }

    internal async void LoadAllChannels()
    {
        Debug.Log("LoadAllChannels");
        var filters = new List<IFieldFilterRule>
        {
            ChannelFilter.Members.In(localUserData.UserId),
        };

        var channels = await _chatClient.QueryChannelsAsync(filters);
        OnChannelListUpdated?.Invoke(channels.ToList());
    }

    #endregion

    #region Messages

    private void LoadMessages(IReadOnlyList<IStreamMessage> messages)
    {
        Debug.Log("Load msg");
        OnMessageListUpdated?.Invoke(messages);
    }

    internal async Task<IStreamMessage> SendTextMessage(string message)
    {
        Debug.Log("SendTextMessage  "+message);
        StreamSendMessageRequest messageRequest = new()
        {
            Text = message,
        };

        SetLastRead();
        OnLocalMessageSent?.Invoke(messageRequest);

        IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
        if (otherUser != null)
            FriendManager.manager.CheckOtherDeletedMessageListItem(otherUser.Id, (isDeleted) =>
            {
                if (isDeleted)
                {
                    FriendManager.manager.RemoveOtherDeletedMessageListItem(otherUser.Id);
                }
            });

        var msg = await currentChannel.SendNewMessageAsync(messageRequest);

        return msg;
    }

    internal async Task<IStreamMessage> SendImageMessage(string pathToImage)
    {

        Debug.Log("SendImageMessage  " + pathToImage);

        if (!File.Exists(pathToImage))
        {
            Debug.LogError("Entered Path To Image File Is Invalid : \nPath : " + pathToImage);
            return null;
        }

        StreamSendMessageRequest localMessageRequest = new() { Attachments = new List<StreamAttachmentRequest> { new StreamAttachmentRequest { ImageUrl ="", ThumbUrl = pathToImage, AssetUrl = "", OgScrapeUrl = "" } } };
        OnLocalMessageSent?.Invoke(localMessageRequest);


        var imageData = File.ReadAllBytes(pathToImage);
        var imageUploadResponse = await currentChannel.UploadImageAsync(imageData, "imageName" + UnityEngine.Random.Range(0,99999).ToString());
        var imageWebUrl = imageUploadResponse.FileUrl;

        IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
        if (otherUser != null)
            FriendManager.manager.CheckOtherDeletedMessageListItem(otherUser.Id, (isDeleted) =>
            {
                if (isDeleted)
                {
                    FriendManager.manager.RemoveOtherDeletedMessageListItem(otherUser.Id);
                }
            });

        StreamSendMessageRequest messageRequest = new() {Attachments = new List<StreamAttachmentRequest> { new StreamAttachmentRequest { ImageUrl = imageWebUrl,ThumbUrl="", AssetUrl = pathToImage, OgScrapeUrl = "" } } };

        SetLastRead();
        var msg = await currentChannel.SendNewMessageAsync(messageRequest);
        return msg;
    }

    internal async Task<IStreamMessage> SendVoiceMessage(byte[] data, string fileName)
    {

        Debug.Log("SendVoiceMessage  " + fileName);
        string pathToVoice = Path.Join(Application.temporaryCachePath, fileName);
        if (!File.Exists(pathToVoice))
        {
            Debug.LogError("Entered Path To Voice File Is Invalid : \nPath : " + pathToVoice);
            return null;
        }

        StreamSendMessageRequest localMessageRequest = new() { Attachments = new List<StreamAttachmentRequest> { new StreamAttachmentRequest { ImageUrl = "", ThumbUrl = "", OgScrapeUrl = pathToVoice, AssetUrl = "" } } };
        OnLocalMessageSent?.Invoke(localMessageRequest);

        var fileUploadResponse = await currentChannel.UploadFileAsync(data, "voiceName" + UnityEngine.Random.Range(0, 99999).ToString());
        var fileWebUrl = fileUploadResponse.FileUrl;

        IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
        if (otherUser != null)
        {
            FriendManager.manager.CheckOtherDeletedMessageListItem(otherUser.Id, (isDeleted) =>
            {
                if (isDeleted)
                {
                    FriendManager.manager.RemoveOtherDeletedMessageListItem(otherUser.Id);
                }
            });
        }

        StreamSendMessageRequest messageRequest = new(){Attachments = new List<StreamAttachmentRequest> { new StreamAttachmentRequest {ImageUrl = "", ThumbUrl="", AssetUrl = fileWebUrl, OgScrapeUrl = pathToVoice } }};

        SetLastRead();
        var msg = await currentChannel.SendNewMessageAsync(messageRequest);
        return msg;
    }

    internal async void DeleteMessage(string msgID)
    {
        foreach(var msg in currentChannel.Messages)
        {
            if(!msg.IsDeleted && msg.Id.Equals(msgID))
            {
                await msg.SoftDeleteAsync();
                return;
            }
        }
    }

    #endregion

    #region Receiving Messages

    private void Callback_OnMessageReceived(IStreamChannel channel, IStreamMessage message)
    {
        if(channel.Id.Equals(currentChannel.Id) && !message.User.Id.Equals(localUserData.User.Id))
            SetLastRead();
        OnMessageReceived?.Invoke(message);
    }

    private void Callback_OnMessageDeleted(IStreamChannel channel, IStreamMessage message, bool isHardDelete)
    {
        if(channel.Id.Equals(currentChannel.Id) && !message.User.Id.Equals(localUserData.User.Id))
            SetLastRead();
        OnMessageDeleted?.Invoke(channel.Messages, message.Id);
    }

    #endregion

    #region Typing Indicator

    internal void SetTypingIndicator(bool typing)
    {
        if (currentChannel == null) return;
        if (typing)
            currentChannel.SendTypingStartedEventAsync();
        else
            currentChannel.SendTypingStoppedEventAsync();
    }

    private void Callback_UserStartedTyping(IStreamChannel channel, IStreamUser user)
    {
        if (!user.UniqueId.Equals(localUserData.User.UniqueId))
        {
            OnOtherUserTypingStatusChanged?.Invoke(true);
        }
    }

    private void Callback_UserStoppedTyping(IStreamChannel channel, IStreamUser user)
    {
        if (!user.UniqueId.Equals(localUserData.User.UniqueId))
        {
            OnOtherUserTypingStatusChanged?.Invoke(false);
        }
    }

    #endregion

    #region Last Read

    private void SetLastRead()
    {
        if (currentChannel == null || PlayerPrefs.GetInt("GHOST_MODE",0) == 1)
            return;

        ReadReceipts.SetClientLastReadForChannel(currentChannel.Id, PlayerData.PlayfabID);
    }

    #endregion     

    #region Online Presence

    public async Task<IStreamUser> GetUserFromPlayfabID(string playfabID)
    {
        if (_chatClient == null) return null;

        var filters = new IFieldFilterRule[]
        {
            UserFilter.Id.EqualsTo(playfabID)
        };

        var users = await _chatClient.QueryUsersAsync(filters);
        if (users.Count() > 0)
            return users.First();
        else
            return null;
    }

    private void SetOnlinePresence(bool isOnline)
    {
        if (PlayerPrefs.GetInt("GHOST_MODE", 0) == 1)
            isOnline = false;

        if(_chatClient != null && _chatClient.IsConnected)
            _chatClient.UpsertUsers(new List<StreamUserUpsertRequest> { new StreamUserUpsertRequest { Id = _chatClient.LocalUserData.User.UniqueId, Invisible = !isOnline } }).LogExceptionsOnFailed();
    }

    #endregion

    #region Notification handler

    private void SendNotificationForOpeningChannel()
    {
        IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
        ReadReceipts.IsChatOpen(currentChannel.Id, otherUser.Id, (chatOpen)=>
        {
            if (chatOpen)
            {
                NotificationManager.manager.SendNotification(otherUser.Id, NotificationCodes.ChatManager_ChatViewedCode, localUserData.UserId);
            }
        });
    }

    private void SendNotificationForUpdatingMessageList()
    {
        IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
        NotificationManager.manager.SendNotification(otherUser.Id, NotificationCodes.ChatManager_UpdateChatCode, "");
    }

    private void OnFirebaseNotificationReceived(Firebase.Messaging.MessageReceivedEventArgs e)
    {
        Debug.Log("OnFirebaseNotificationReceived call");
        Debug.Log("e.Message.Notification"+ e.Message.Notification.Title);
        Debug.Log("OnFirebaseNotificationReceived get title");

        if (e.Message.Notification.Title.Equals(NotificationCodes.ChatManager_ChatViewedCode))
        {
            IStreamUser otherUser = currentChannel.Members.Where(i => !i.User.Id.Equals(localUserData.UserId)).First().User;
            Debug.Log("Other user id"+otherUser.Id);
            if (otherUser.Id.Equals(e.Message.Notification.Body))
            {
                Debug.Log("Other user id match and call callback");
                currentChannel.MessageReceived -= Callback_OnMessageReceived;
                currentChannel.MessageDeleted -= Callback_OnMessageDeleted;
                currentChannel.UserStartedTyping -= Callback_UserStartedTyping;
                currentChannel.UserStoppedTyping -= Callback_UserStoppedTyping;
                currentChannel = null;
                //CreateOrJoinChannelWithFriend(e.Message.Notification.Title,false);
                CreateOrJoinChannelWithFriend(e.Message.Notification.Body, false);

            }
        }

        if (e.Message.Notification.Title.Equals(NotificationCodes.ChatManager_UpdateChatCode))
        {
            LoadAllChannels();
        }
    }

    #endregion

    #region Ghost Mode

    public void ToggleGhostMode(bool on)
    {
        if (on)
            PlayerPrefs.SetInt("GHOST_MODE", 1);
        else
            PlayerPrefs.SetInt("GHOST_MODE", 0);
        SetOnlinePresence(true);
    }

    #endregion

    #region Cache Handling

    public static void DeleteCache()
    {
        string path = Application.temporaryCachePath;

        DirectoryInfo di = new(path);

        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }
    }



    #endregion
}