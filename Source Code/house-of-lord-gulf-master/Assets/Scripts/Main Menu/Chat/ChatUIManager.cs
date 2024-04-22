using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using StreamChat.Core.StatefulModels;
using System;
using System.Linq;
using StreamChat.Core.Requests;
using System.Threading.Tasks;

public class ChatUIManager : MonoBehaviour
{
    public static ChatUIManager manager;

    [Header("Scripts")]
    [SerializeField] ChatManager chatManager;

    [Header("Profile")]
    [SerializeField] Image avatarImage;
    [SerializeField] Image onlineImage;
    [SerializeField] TMP_Text nameText;
    [SerializeField] GameObject typingText;

    [Header("Messages")]
    [SerializeField] TMP_InputField messageInputText;
    [SerializeField] RectTransform messagesHolder;

    [Header("Prefabs")]
    [Header("Our Message Prefabs")]
    [SerializeField] GameObject ourTextMessagePrefab;
    [SerializeField] GameObject ourImageMessagePrefab;
    [SerializeField] GameObject ourVoiceMessagePrefab;
    [SerializeField] GameObject ourDeletedMessagePrefab;

    [Header("Their Message Prefabs")]
    [SerializeField] GameObject theirTextMessagePrefab;
    [SerializeField] GameObject theirImageMessagePrefab;
    [SerializeField] GameObject theirVoiceMessagePrefab;
    [SerializeField] GameObject theirDeletedMessagePrefab;

    [Header("Image Selection Panel")]
    [SerializeField] GameObject imageSelectionPanel;
    [SerializeField] Image imageSelectionImage;
    [SerializeField] Button selectImageButton;
    [SerializeField] Button cancelImageButton;

    [Header("Image FullScreen View")]
    [SerializeField] GameObject imageViewPanel;
    [SerializeField] Image imageViewImage;
    [SerializeField] Button closeImageViewButton;

    [Header("Miscellaneous")]
    [SerializeField] GameObject dateBoxPrefab;
    [SerializeField] Toggle ghostModeToggle;

    //Key - Creation Date | Value - Message Item
    Dictionary<DateTimeOffset, MessageItem> messagesDict = new();
    Dictionary<StreamSendMessageRequest, MessageItem> localMessagesDict = new();
    IStreamUser otherUser;
    private bool m_checking;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        chatManager.OnMessageListUpdated += UpdateMessageList;
        chatManager.OnMessageDeleted += (msges, id) => UpdateMessageList(msges);
        chatManager.OnMessageReceived += SetupMessageItem;
        chatManager.OnLocalMessageSent += SetupLocalMessageItem;
        chatManager.OnOtherUserTypingStatusChanged += OnOtherUserTypingStatusChanged;
        InitializeGhostModeToggle();
        messageInputText.onTouchScreenKeyboardStatusChanged.AddListener(OnClick_SendTextMessage);
    }

    private void OnDisable()
    {
        messageInputText.onTouchScreenKeyboardStatusChanged.RemoveListener(OnClick_SendTextMessage);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !chatManager.isNativeGallaryOpen && !TouchScreenKeyboard.visible)
        {
            OnClick_LeaveChannel();
        }
    }

    private void OnApplicationQuit()
    {
        OnClick_LeaveChannel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnClick_SendTextMessage();
        }

        //ghostModeToggle.interactable = IAPManager.manager.IsSubscriptionActive;
    }

    #endregion

    #region Updating Messages

    private void UpdateMessageList(IReadOnlyList<IStreamMessage> messages)
    {
        m_checking = false;//
       messagesDict.Clear();

        foreach (Transform child in messagesHolder)
            Destroy(child.gameObject);

        foreach(var msg in messages)
            SetupMessageItem(msg);

    }

    private void SetupMessageItem(IStreamMessage msg)
    {
        bool isMyMsg = msg.User.Id.Equals(chatManager.localUserData.User.Id);
        IStreamUser otherUser = chatManager.currentChannel.Members.Where((i) => !i.User.Id.Equals(chatManager.localUserData.User.Id)).First().User;

        MessageItem spawnedMsgItem = null;

        KeyValuePair<StreamSendMessageRequest, MessageItem> req = new(null, null);
        try
        {
            if (localMessagesDict.Count > 0)
            {
                var reqs = localMessagesDict.Where(i => !msg.IsDeleted && ((msg.Attachments.Count > 0 && i.Key.Attachments.Count > 0 && (i.Key.Attachments[0].ThumbUrl.Equals(msg.Attachments[0].AssetUrl) || i.Key.Attachments[0].OgScrapeUrl.Equals(msg.Attachments[0].OgScrapeUrl))) || i.Key.Text.Equals(msg.Text)));
                if (reqs.Count() > 0)
                    req = reqs.First();
            }
        }
        catch
        {
            Debug.LogError("Error Getting Local Message") ;
        }
        // Local Msg Not Found
        if (req.Key == null || req.Value == null)
        {
            GameObject messageToSpawn = null;
            DateTimeOffset lastMessageCreationDate = messagesDict.Count > 0 ? messagesDict.Last().Key : new(new DateTime(2000, 12, 12));

            if (lastMessageCreationDate.LocalDateTime.Date < msg.CreatedAt.LocalDateTime.Date)
                Instantiate(dateBoxPrefab, messagesHolder).GetComponentInChildren<TMP_Text>().text = msg.CreatedAt.LocalDateTime.Date.ToShortDateString();

            if (!msg.IsDeleted)
            {
                if (!string.IsNullOrEmpty(msg.Text)) //Its a text message
                    messageToSpawn = isMyMsg ? ourTextMessagePrefab : theirTextMessagePrefab;
                else if (!string.IsNullOrEmpty(msg.Attachments[0].ImageUrl))  // Its a image message
                    messageToSpawn = isMyMsg ? ourImageMessagePrefab : theirImageMessagePrefab;
                else if (!string.IsNullOrEmpty(msg.Attachments[0].AssetUrl) && !string.IsNullOrEmpty(msg.Attachments[0].OgScrapeUrl))  // Its a voice message
                    messageToSpawn = isMyMsg ? ourVoiceMessagePrefab : theirVoiceMessagePrefab;

                spawnedMsgItem = Instantiate(messageToSpawn, messagesHolder).GetComponent<MessageItem>();

            }
            else
            {
                messageToSpawn = isMyMsg ? ourDeletedMessagePrefab : theirDeletedMessagePrefab;
                Instantiate(messageToSpawn, messagesHolder);
            }
        }
        else
        {
            spawnedMsgItem = req.Value;
            localMessagesDict.Remove(req.Key);
        }

        if (spawnedMsgItem != null)
        {
            spawnedMsgItem.SetupMessage(msg);
            ReadReceipts.IsChatOpen(chatManager.currentChannel.Id, otherUser.Id, (isOpen) =>
            {
                if (isOpen)
                {
                    spawnedMsgItem.MarkMessageAsRead(true);
                    msg.MarkMessageAsLastReadAsync();
                }
                else
                {
                   /* if (!m_checking && spawnedMsgItem.m_mymsg)
                    {
                        ReadReceipts.IsMessageReadInChannel(chatManager.currentChannel.Id, otherUser.Id, msg.CreatedAt.UtcDateTime, (isRead) =>
                        {
                            spawnedMsgItem.MarkMessageAsRead(isRead);
                            if (isRead)
                            {
                                msg.MarkMessageAsLastReadAsync();
                            }
                            else
                            {
                                Debug.Log("Not Read");
                                Debug.Log("Checking Mine " + spawnedMsgItem.gameObject.name);
                                m_checking = true;
                                spawnedMsgItem.CheckForRead(chatManager.currentChannel.Id, otherUser.Id, msg);
                            }

                        });
                    }
                    else
                    {
                        Debug.Log("Not Read But Already Checking");
                        spawnedMsgItem.MarkMessageAsRead(false);
                    }*/
                    ReadReceipts.IsMessageReadInChannel(chatManager.currentChannel.Id, otherUser.Id, msg.CreatedAt.UtcDateTime, (isRead) => { spawnedMsgItem.MarkMessageAsRead(isRead); if(isRead) msg.MarkMessageAsLastReadAsync(); });
                }
            });
        }
        else
            Debug.Log("Error in setting up a message item, Message Item either null, deleted or not locally spawned.");

        messagesDict.Add(msg.CreatedAt.LocalDateTime, spawnedMsgItem);
        UpdateLayoutGroup();
    }

    private void SetupLocalMessageItem(StreamSendMessageRequest msg)
    {
        GameObject messageToSpawn = null;
        MessageItem spawnedMsgItem = null;
        IStreamUser otherUser = chatManager.currentChannel.Members.Where((i) => !i.User.Id.Equals(chatManager.localUserData.User.Id)).First().User;
        DateTimeOffset lastMessageCreationDate = messagesDict.Count > 0 ? messagesDict.Last().Key : new(new DateTime(2000, 12, 12));

        if (lastMessageCreationDate.LocalDateTime.Date < DateTime.Now.Date)
            Instantiate(dateBoxPrefab, messagesHolder).GetComponentInChildren<TMP_Text>().text = DateTime.Now.Date.ToShortDateString();

        if (!string.IsNullOrEmpty(msg.Text)) //Its a text message
            messageToSpawn = ourTextMessagePrefab;
        else if (!string.IsNullOrEmpty(msg.Attachments[0].ThumbUrl))  // Its a image message
            messageToSpawn = ourImageMessagePrefab;
        else if (!string.IsNullOrEmpty(msg.Attachments[0].OgScrapeUrl))  // Its a voice message
            messageToSpawn = ourVoiceMessagePrefab;

        spawnedMsgItem = Instantiate(messageToSpawn, messagesHolder).GetComponent<MessageItem>();
        spawnedMsgItem.SetupLocalMessage(msg);

        localMessagesDict.Add(msg, spawnedMsgItem);
        UpdateLayoutGroup();
    }

    internal void UpdateLayoutGroup()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(messagesHolder);
    }

    #endregion

    #region Button OnClick

    public void OnClick_ChatButton(string playfabID, Sprite avatarSpr, string username)
    {
        avatarImage.sprite = avatarSpr;
        nameText.text = username;
        typingText.SetActive(false);
        chatManager.CreateOrJoinChannelWithFriend(playfabID);
        messagesDict.Clear();
        SetupOnlinePresence(playfabID);

        foreach (Transform child in messagesHolder)
            Destroy(child.gameObject);
        UIManager.manager.OpenPanel(UIManager.manager.chatPanel);
    }

    private async void SetupOnlinePresence(string playfabID)
    {
        if (onlineImage == null) return;
        if (otherUser != null) otherUser.PresenceChanged -= User_PresenceChanged;

        while (!ChatManager.manager.isInitialized)
            await Task.Yield();

        var user = await ChatManager.manager.GetUserFromPlayfabID(playfabID);
        if (user != null)
        {
            otherUser = user;
            onlineImage.color = user.Online ? Color.green : Color.white;
            user.PresenceChanged += User_PresenceChanged;
        }
        else
        {
            Debug.LogError("No Get Stream User Found With playfab ID : " + playfabID);
        }
    }

    private void User_PresenceChanged(IStreamUser user, bool isOnline, DateTimeOffset? lastActive)
    {
        if (onlineImage == null) return;
        onlineImage.color = isOnline ? Color.green : Color.white;
    }

    public void OnClick_LeaveChannel()
    {
        chatManager.LeaveChatWithFriend();
        UIManager.manager.ClosePanel(UIManager.manager.chatPanel);
    }

    #endregion

    #region Message Send Button OnClick

    private void OnClick_SendTextMessage(TouchScreenKeyboard.Status status)
    {
        if(status == TouchScreenKeyboard.Status.Done && messageInputText.text.Length>0)
        {
            _ = chatManager.SendTextMessage(messageInputText.text);
            messageInputText.text = "";
        }
    }

    public void OnClick_SendTextMessage()
    {
        if (messageInputText.text.Length > 0)
        {
            _ = chatManager.SendTextMessage(messageInputText.text);
            messageInputText.text = "";
        }
    }

    public void OnClick_SendImageMessage()
    {
        if (!IAPManager.manager.IsSubscriptionActive) return;

        chatManager.isNativeGallaryOpen = true;
        NativeGallery.GetImageFromGallery((path)=>
        {
            chatManager.isNativeGallaryOpen = false;
            ImageManager.GetAndSetLocalTextureToImage(path, imageSelectionImage);
            selectImageButton.onClick.RemoveAllListeners();
            selectImageButton.onClick.AddListener(() =>
            {
                _ = chatManager.SendImageMessage(path);
                UIAnimationManager.manager.PopDownPanel(imageSelectionPanel);
            });
            cancelImageButton.onClick.RemoveAllListeners();
            cancelImageButton.onClick.AddListener(()=>
            {
                selectImageButton.onClick.RemoveAllListeners();
                UIAnimationManager.manager.PopDownPanel(imageSelectionPanel);
            });
            UIAnimationManager.manager.PopUpPanel(imageSelectionPanel);
        });
    }
    #endregion

    #region Typing Indicator

    public void TypingIndicator_OnMessageTFValueChanged(string value)
    {
        ChatManager.manager.SetTypingIndicator(value.Length > 0);
    }

    private void OnOtherUserTypingStatusChanged(bool typing)
    {
        typingText.SetActive(typing);
    }

    #endregion

    #region Image FullScreen View

    public void ShowImageFullScreen(Sprite spr)
    {
        imageViewImage.sprite = spr;
        closeImageViewButton.onClick.RemoveAllListeners();
        closeImageViewButton.onClick.AddListener(()=>UIAnimationManager.manager.PopDownPanel(imageViewPanel));
        UIAnimationManager.manager.PopUpPanel(imageViewPanel);
    }

    #endregion

    #region Ghost Mode

    private void InitializeGhostModeToggle()
    {
        ghostModeToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("GHOST_MODE",0)==1);
        ghostModeToggle.onValueChanged.AddListener(chatManager.ToggleGhostMode);
    }

    #endregion
}
