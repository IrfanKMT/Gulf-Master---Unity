using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Newtonsoft.Json;

public class FriendListsManager : MonoBehaviour
{
    public static FriendListsManager manager;
    readonly List<FriendListsData> defaultListData = new()
    {
        new FriendListsData
        {
            listName = "Friends",
            playfabIDs = new()
        },
        new FriendListsData
        {
            listName = "Family",
            playfabIDs = new()
        },
        new FriendListsData
        {
            listName = "Brothers",
            playfabIDs = new()
        },
        new FriendListsData
        {
            listName = "Girls",
            playfabIDs = new()
        },
        new FriendListsData
        {
            listName = "Work",
            playfabIDs = new()
        },
        new FriendListsData
        {
            listName = "Others",
            playfabIDs = new()
        }
    };
    internal List<FriendListsData> friendListData;

    [Header("List Panel")]
    public Transform listItemContainer;
    [SerializeField] FriendListItem listItemPrefab;

    [Header("Toggle UI")]
    [SerializeField] GameObject toggleListPanel;
    [SerializeField] FriendListToggleItem togglListItemPrefab;
    [SerializeField] Transform toggleContainer;

    private bool PlayerIsLogedIn;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += LoadFriendsList;
    }

    #endregion

    #region Initalize List Data

    private void LoadFriendsList()
    {
        var loadFriendListRequest = new GetUserDataRequest{ Keys = new List<string> { PlayfabDataKeys.FriendListData }};
        PlayFabClientAPI.GetUserData(loadFriendListRequest, OnFriendListLoaded, OnLoadingFriendListFailed);
    }

    private void OnListDataUpdated(UpdateUserDataResult result)
    {
        var loadUpdatedListData = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.FriendListData }
        };
        PlayFabClientAPI.GetUserData(loadUpdatedListData, OnFriendListLoaded, OnLoadingFriendListFailed);
    }

    private void OnFriendListLoaded(GetUserDataResult result)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.FriendListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.FriendListData].Value;
            List<FriendListsData> _friendListData = JsonConvert.DeserializeObject<List<FriendListsData>>(jsonData);
            if (_friendListData.Count == 6)
            {
                friendListData = _friendListData;
                InitializeListItemUI();
            }
        }
        else
        {
            SaveListData(defaultListData);
        }
    }

    private void OnLoadingFriendListFailed(PlayFabError error)
    {
        Debug.LogError($"Error in loading friend list : \nError Message : {error.ErrorMessage}\nError Report : {error.GenerateErrorReport()}");
    }

    private void OnUpdatingListDataFailed(PlayFabError error)
    {
        Debug.LogError($"Error while updating player's list data : \nError Message : {error.ErrorMessage}\nError Report : {error.GenerateErrorReport()}");
    }

    #endregion

    #region Update List Data

    private void SaveListData(List<FriendListsData> data)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        var setDefaultListDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {PlayfabDataKeys.FriendListData, jsonData }
            }
        };
        PlayFabClientAPI.UpdateUserData(setDefaultListDataRequest, OnListDataUpdated, OnUpdatingListDataFailed);
    }

    private void AddPlayfabIDToListData(string listName, string playfabID)
    {
        var loadFriendListRequest = new GetUserDataRequest { Keys = new List<string> { PlayfabDataKeys.FriendListData } };
        PlayFabClientAPI.GetUserData(loadFriendListRequest,(res) => AddPlayfabIDToList_OnFriendListLoaded(res, listName, playfabID), OnLoadingFriendListFailed);
    }

    private void AddPlayfabIDToList_OnFriendListLoaded(GetUserDataResult result, string listName, string playfabID)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.FriendListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.FriendListData].Value;
            List<FriendListsData> _friendListData = JsonConvert.DeserializeObject<List<FriendListsData>>(jsonData);

            foreach(var list in _friendListData)
            {
                if (list.listName.Equals(listName))
                {
                    if (!list.playfabIDs.Contains(playfabID))
                    {
                        list.playfabIDs.Add(playfabID);
                        break;
                    }
                }
            }

            SaveListData(_friendListData);
        }
        else
        {
            SaveListData(defaultListData);
        }
    }

    private void RemovePlayfabIDFromListData(string listName, string playfabID)
    {
        var loadFriendListRequest = new GetUserDataRequest { Keys = new List<string> { PlayfabDataKeys.FriendListData } };
        PlayFabClientAPI.GetUserData(loadFriendListRequest, (res) => RemovePlayfabIDFromList_OnFriendListLoaded(res, listName, playfabID), OnLoadingFriendListFailed);
    }

    private void RemovePlayfabIDFromList_OnFriendListLoaded(GetUserDataResult result, string listName, string playfabID)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.FriendListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.FriendListData].Value;
            List<FriendListsData> _friendListData = JsonConvert.DeserializeObject<List<FriendListsData>>(jsonData);

            foreach (var list in _friendListData)
            {
                if (list.listName.Equals(listName))
                {
                    if (list.playfabIDs.Contains(playfabID))
                    {
                        list.playfabIDs.Remove(playfabID);
                        break;
                    }
                }
            }

            SaveListData(_friendListData);
        }
        else
        {
            SaveListData(defaultListData);
        }
    }

    public void RemovePlayfabIDFromAllListData(string playfabID)
    {
        var loadFriendListRequest = new GetUserDataRequest { Keys = new List<string> { PlayfabDataKeys.FriendListData } };
        PlayFabClientAPI.GetUserData(loadFriendListRequest, (res) => RemovePlayfabIDFromAllList_OnFriendListLoaded(res, playfabID), OnLoadingFriendListFailed);
    }

    private void RemovePlayfabIDFromAllList_OnFriendListLoaded(GetUserDataResult result, string playfabID)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.FriendListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.FriendListData].Value;
            List<FriendListsData> _friendListData = JsonConvert.DeserializeObject<List<FriendListsData>>(jsonData);

            foreach (var list in _friendListData)
                if (list.playfabIDs.Contains(playfabID))
                    list.playfabIDs.Remove(playfabID);

            SaveListData(_friendListData);
        }
        else
        {
            SaveListData(defaultListData);
        }
    }

    public void UpdateListNameData(string oldlistName, string newListname)
    {
        var loadFriendListRequest = new GetUserDataRequest { Keys = new List<string> { PlayfabDataKeys.FriendListData } };
        PlayFabClientAPI.GetUserData(loadFriendListRequest, (res) => UpdateListNameData_OnFriendListLoaded(res, oldlistName, newListname), OnLoadingFriendListFailed);
    }

    private void UpdateListNameData_OnFriendListLoaded(GetUserDataResult result, string listName, string newListname)
    {
        if (result.Data.ContainsKey(PlayfabDataKeys.FriendListData))
        {
            string jsonData = result.Data[PlayfabDataKeys.FriendListData].Value;
            List<FriendListsData> _friendListData = JsonConvert.DeserializeObject<List<FriendListsData>>(jsonData);

            if(_friendListData.Where(i => i.listName.Equals(listName)).Any())
            {
                for (int i = 0; i < _friendListData.Count; i++)
                {
                    if (_friendListData[i].listName.Equals(listName))
                    {
                        _friendListData[i] = new FriendListsData() { listName = newListname, playfabIDs = _friendListData[i].playfabIDs};
                        break;
                    }
                }
                SaveListData(_friendListData);
            }
        }
        else
        {
            SaveListData(defaultListData);
        }
    }

    #endregion

    #region Friend List UI

    public void InitializeToggleUI(string playfabID)
    {
        if (friendListData.Count == 6)
        {
            foreach (Transform child in toggleContainer)
                Destroy(child.gameObject);

            foreach(var item in friendListData)
            {
                void onCallback() => AddPlayfabIDToListData(item.listName, playfabID);
                void offCallback() => RemovePlayfabIDFromListData(item.listName, playfabID);

                FriendListToggleItem toggleItem = Instantiate(togglListItemPrefab.gameObject, toggleContainer).GetComponent<FriendListToggleItem>();
                toggleItem.SetupList(item.listName, onCallback, offCallback, item.playfabIDs.Contains(playfabID));
            }

            toggleListPanel.transform.SetAsLastSibling();
            UIAnimationManager.manager.PopUpPanel(toggleListPanel);
        }
        else
        {
            Debug.LogError("Friend List Data Is Missing. Friend List Data Count : " + friendListData.Count);
        }
    }

    public void InitializeListItemUI()
    {
        if (friendListData.Count == 6)
        {
            foreach(Transform child in listItemContainer)
                Destroy(child.gameObject);

            foreach(var item in friendListData)
                Instantiate(listItemPrefab.gameObject, listItemContainer).GetComponent<FriendListItem>().Setup(item.listName, item.playfabIDs);
        }
    }

    public void OnClick_CloseButton()
    {
        UIAnimationManager.manager.PopDownPanel(toggleListPanel);
    }

    #endregion
}

[Serializable]
public struct FriendListsData
{
    public string listName;
    public List<string> playfabIDs;
}