using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamChat.Core.StatefulModels;
using System.Linq;

public class FriendItem : MonoBehaviour
{
    [SerializeField] Image avatarImage;
    [SerializeField] Image onlineImage;
    [SerializeField] TMPro.TMP_Text nameText;
    [SerializeField] TMPro.TMP_Text unreadCountText;
    internal string playfabID = "";
    internal int unreadCount = 0;
    bool isMessageListItem = false;

    private void Update()
    {
        if (isMessageListItem)
        {
            if (!FriendManager.manager.friendList.Contains(playfabID))
            {
                Destroy(gameObject);
            }
        }
    }

    public void Setup(string playfabID, bool isMessageListItem = false)
    {
        //Debug.Log("   " + playfabID);
        this.playfabID = playfabID;
        ProfileFetcher.FetchAndSetUserNameWithTag(playfabID, nameText);
        ProfileFetcher.FetchAndSetAvatarImage(playfabID, avatarImage);
        SetupOnlinePresence();

        this.isMessageListItem = isMessageListItem;
        if (isMessageListItem)
            FriendManager.manager.OnFriendListUpdated += OnFriendListUpdated;
    }

    #region Setup Online Presence

    private async void SetupOnlinePresence()
    {
        if (onlineImage == null) return;

        while (!ChatManager.manager.isInitialized)
            await Task.Yield();

        var user = await ChatManager.manager.GetUserFromPlayfabID(playfabID);
        if (user != null)
        {
            onlineImage.color = user.Online ? Color.green : Color.white;
            user.PresenceChanged += User_PresenceChanged;
        }
        else
        {
            Debug.LogError("No Get Stream User Found With playfab ID : " + playfabID);
        }
    }

    private void User_PresenceChanged(StreamChat.Core.StatefulModels.IStreamUser user, bool isOnline, System.DateTimeOffset? lastActive)
    {
        if (onlineImage == null) return;
        onlineImage.color = isOnline ? Color.green : Color.white;
    }

    #endregion

    #region Setup Unread Numbers

    public void SetupUnreadNumbers(IStreamChannel channel)
    {
        Debug.Log("SetupUnreadNumbers");
        if (unreadCountText == null || channel == null) return;

        ReadReceipts.GetUnreadMessages(channel.Messages.ToList(), channel.Id, ChatManager.manager.localUserData.UserId, (unread) =>
        {
            Debug.Log("MessageReceived Event Added In Msg Box");
            unreadCount = unread;
            unreadCountText.text = unread.ToString();
            channel.MessageReceived += MessageReceived;
        });
        
    }

    private void MessageReceived(IStreamChannel channel, IStreamMessage message)
    {
        Debug.Log("MessageReceived " + message);
        if (unreadCountText == null) return;
        ReadReceipts.GetUnreadMessages(channel.Messages.ToList(), channel.Id, ChatManager.manager.localUserData.UserId, (unread) =>
        {
            unreadCount = unread;
            unreadCountText.text = unread.ToString();
        });
    }

    #endregion

    #region Blocking And Unblocking

    public void OnClick_Block()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Blocking The User, Playfab ID is null");
            return;
        }
        FriendManager.manager.BlockUser(playfabID);
    }

    public void OnClick_UnBlock()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error UnBlocking The User, Playfab ID is null");
            return;
        }
        FriendManager.manager.UnblockUser(playfabID);
    }

    #endregion

    #region Accepting and Rejecting Friend Request

    public void OnClick_Accept()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Accepting Friend Request of The User, Playfab ID is null");
            return;
        }
        FriendManager.manager.AcceptFriendRequest(playfabID);
    }

    public void OnClick_Reject()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Rejecting Friend Request of The User, Playfab ID is null");
            return;
        }
        FriendManager.manager.RejectFriendRequest(playfabID);
    }

    #endregion

    #region Chat Button

    public void OnClick_Chat()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Opening Chat, Playfab ID is null");
            return;
        }

        if (string.IsNullOrEmpty(nameText.text))
        {
            Debug.LogError("Error Opening Chat, Name is null");
            return;
        }

        ChatUIManager.manager.OnClick_ChatButton(playfabID, avatarImage.sprite, nameText.text);
        if (isMessageListItem)
        {
            unreadCountText.text = 0.ToString();
            unreadCount = 0;
        }
    }

    #endregion

    #region Add Friend [Recents]

    public void OnClick_AddFriend()
    {
        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Sending Friend Request To The User, Playfab ID is null");
            return;
        }
        FriendManager.manager.SendFriendRequestViaPlayfab(playfabID);
    }

    #endregion

    #region Lists

    public void OnClick_List()
    {
        if (!IAPManager.manager.IsSubscriptionActive) return;

        if (string.IsNullOrEmpty(playfabID))
        {
            Debug.LogError("Error Adding User To A List, Playfab ID is null");
            return;
        }

        FriendListsManager.manager.InitializeToggleUI(playfabID);
    }

    #endregion

    #region Deleteing Message List Item

    private void OnFriendListUpdated(List<string> IDs)
    {
        if (!IDs.Contains(playfabID))
        {
            Destroy(gameObject);
        }
    }

    public void OnClick_DeleteMessageItem()
    {
        FriendManager.manager.AddDeletedMessageListItem(playfabID);
        Destroy(gameObject);
    }

    #endregion
}
