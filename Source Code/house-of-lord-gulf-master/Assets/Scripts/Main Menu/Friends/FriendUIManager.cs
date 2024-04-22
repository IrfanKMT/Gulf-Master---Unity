using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class FriendUIManager : MonoBehaviour
{
    public static FriendUIManager manager;

    [Header("List")]
    [SerializeField] Transform friendListContainer;
    [SerializeField] Transform friendRequestsListContainer;
    [SerializeField] Transform blockedFriendListContainer;
    [SerializeField] Transform recentListContainer;
    [SerializeField] Transform messageListContainer;

    [Header("Panels")]
    public GameObject friendListPanel;
    [SerializeField] GameObject friendRequestListPanel;
    [SerializeField] GameObject blockedFriendListPanel;
    [SerializeField] GameObject recentsListPanel;
    [SerializeField] GameObject addFriendPanel;
    [SerializeField] GameObject myIDPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject listPanel;
    [SerializeField] GameObject messageListPanel;

    [Header("Prefabs")]
    public GameObject friendListItem;
    [SerializeField] GameObject friendRequestListItem;
    [SerializeField] GameObject friendBlockListItem;
    [SerializeField] GameObject recentListItem;
    [SerializeField] GameObject messageListItem;

    [Header("Add Friend Panel")]
    [SerializeField] TMP_InputField userID_TF;
    public TMP_Text addFriend_errorTxt;

    [Header("My ID Panel")]
    [SerializeField] TMP_Text myIDText;

    

    List<string> friendsList = new();
    List<string> friendRequestsList = new();
    List<string> blockedFriendsList = new();

    List<string> oldFriendsList = new();
    List<string> oldFriendRequestsList = new();
    List<string> oldBlockedFriendsList = new();

    List<FriendItem> messageListItems = new();


    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        foreach (Transform child in friendListContainer)
            Destroy(child.gameObject);

        foreach (Transform child in friendRequestsListContainer)
            Destroy(child.gameObject);

        foreach (Transform child in blockedFriendListContainer)
            Destroy(child.gameObject);

        foreach (Transform child in recentListContainer)
            Destroy(child.gameObject);

        foreach (Transform child in messageListContainer)
            Destroy(child.gameObject);
        InitializeUI();
    }

    #endregion

    #region Initialize UI

    public void OnEnableSetData()
    {
        myIDText.text = PlayerData.Username;
    }

    private void InitializeUI()
    {
        FriendManager.manager.OnFriendListUpdated += (IDs) =>
        {
            friendsList = IDs;
            if (!(oldFriendsList.All(friendsList.Contains) && oldFriendsList.Count == friendsList.Count))
            {
                oldFriendsList = IDs;
                print("Friend List Updated");
                ShowList(IDs, friendListItem, friendListContainer);
            }
        };

        FriendManager.manager.OnRequestsListUpdated += (IDs) =>
        {
            friendRequestsList = IDs;
            if (oldFriendRequestsList.All(friendRequestsList.Contains) && oldFriendRequestsList.Count == friendRequestsList.Count)
                return;

            oldFriendRequestsList = IDs;
            print("Request List Updated");
            ShowList(IDs, friendRequestListItem, friendRequestsListContainer);
        };

        FriendManager.manager.OnBlockListUpdated += (IDs) =>
        {
            blockedFriendsList = IDs;
            if (oldBlockedFriendsList.All(blockedFriendsList.Contains) && oldBlockedFriendsList.Count == blockedFriendsList.Count)
                return;
            print("Block List Updated");
            oldBlockedFriendsList = IDs;
            ShowList(IDs, friendBlockListItem, blockedFriendListContainer);
        };

        FriendManager.manager.OnRecentListUpdated += (IDs) =>
        {
            print("Recents List Updated");
            ShowList(IDs, recentListItem, recentListContainer);
        };

        ChatManager.manager.OnChannelListUpdated += channels =>
        {
            Debug.Log("OnChannelListUpdated");
            FriendManager.manager.FetchMyDeletedMessageListItem((deletedMsgList) => {
                List<FriendItem> oldMessageItemList = new();
                oldMessageItemList.AddRange(messageListItems);
                messageListItems.Clear();

                foreach (var channel in channels)
                {
                    var otheruser = channel.Members.Where(i => !i.User.Id.Equals(ChatManager.manager.localUserData.UserId)).First();
                    if (otheruser != null)
                    {
                        if (deletedMsgList.ContainsKey(otheruser.User.Id))
                            if ((bool)deletedMsgList[otheruser.User.Id])
                                continue;
                        bool gameObjectStillPresent = messageListContainer.GetComponentsInChildren<FriendItem>().Where(i => i.playfabID.Equals(otheruser.User.Id)).Any();
                        if (!gameObjectStillPresent)
                        {
                            GameObject item = Instantiate(messageListItem, messageListContainer);
                            FriendItem friendItem = item.GetComponent<FriendItem>();

                            if (friendItem == null)
                            {
                                Debug.LogError("Friend Item Not Found On GameObject : " + messageListItem.name);
                                Destroy(item);
                                continue;
                            }

                            friendItem.Setup(otheruser.User.Id, true);
                            friendItem.SetupUnreadNumbers(channel);
                            messageListItems.Add(friendItem);
                        }
                        else if(oldMessageItemList.Where(i => i.playfabID.Equals(otheruser.User.Id)).Any() && gameObjectStillPresent)
                            messageListItems.Add(oldMessageItemList.Where(i => i.playfabID.Equals(otheruser.User.Id)).First());
                    }
                }

                foreach (var item in oldMessageItemList)
                    if (!messageListItems.Where(i => i.playfabID.Equals(item.playfabID)).Any())
                        Destroy(item.gameObject);
            });
        };
    }

    #endregion

    #region Main Panel Button Clicks

    public void OnClick_FriendsButton()
    {
        ShowPanel(friendListPanel);
    }

    public void OnClick_RequestButton()
    {
        ShowPanel(friendRequestListPanel);
    }

    public void OnClick_BlockButton()
    {
        ShowPanel(blockedFriendListPanel);
    }

    public void OnClick_SettingsButton()
    {
        ShowPanel(settingsPanel);
    }

    public void OnClick_AddFriendButton()
    {
        ShowPanel(addFriendPanel);
    }

    public void OnClick_MyIDButton()
    {
        ShowPanel(myIDPanel);
    }

    public void OnClick_RecentsButton()
    {
        ShowPanel(recentsListPanel);
    }

    public void OnClick_ListButton()
    {
        FriendListsManager.manager.InitializeListItemUI();
        ShowPanel(listPanel);
    }

    public void OnClick_MessageListButton()
    {
        ShowPanel(messageListPanel);
    }

    #endregion

    #region Add Friend Panel Button Clicks

    public void AddFriend_OnClick_SendRequest()
    {
        string userID = userID_TF.text;

        if (userID.Contains("#") && userID.Length >= 8)
            FriendManager.manager.SendFriendRequest(userID);
        else
            addFriend_errorTxt.text = "Invalid User ID";
    }

    #endregion

    #region Update UI

    private void ShowList(List<string> list, GameObject prefab, Transform listContainer)
    {   
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        foreach(var friendID in list)
        {
            GameObject item = Instantiate(prefab, listContainer);
            FriendItem friendItem = item.GetComponent<FriendItem>();

            if (friendItem == null)
            {
                Debug.LogError("Friend Item Not Found On GameObject : " + prefab.name);
                Destroy(item);
                return;
            }

            friendItem.Setup(friendID);
            UIAnimationManager.manager.PopUpPanel(item);
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != friendListPanel && friendListPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(friendListPanel);

        if (panel != listPanel && listPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(listPanel);

        if (panel != friendRequestListPanel && friendRequestListPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(friendRequestListPanel);

        if (panel != blockedFriendListPanel && blockedFriendListPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(blockedFriendListPanel);

        if (panel != settingsPanel && settingsPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(settingsPanel);

        if (panel != myIDPanel && myIDPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(myIDPanel);

        if (panel != addFriendPanel && addFriendPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(addFriendPanel);

        if (panel != recentsListPanel && recentsListPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(recentsListPanel);

        if (panel != messageListPanel && messageListPanel.activeInHierarchy)
            UIManager.manager.ClosePanel(messageListPanel);

        if (!panel.activeInHierarchy)
            UIManager.manager.OpenPanel(panel);
    }

    #endregion
}
