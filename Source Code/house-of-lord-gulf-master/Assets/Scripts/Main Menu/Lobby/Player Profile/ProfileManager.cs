using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEditor;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager manager;
    public event Action<string> OnLoadingNewProfileOrUpdatingProfile; // playfab id of the user whose profile is being loaded

    [SerializeField] ProfileUIManager uiManager;

    [Header("Profile")]
    [SerializeField] TMP_Text usernameTxt;
    [SerializeField] TMP_Text idTxt;
    [SerializeField] Image avatarImg;
    [SerializeField] Button avatarImgButton;
    [SerializeField] GameObject avatarImgDeleteButton;
    [SerializeField] Image countryImg;
    [SerializeField] Button sendFriendRequestButton;
    [SerializeField] Toggle sendFriendAcceptStatusToggle;

    [Header("Album Images")]
    [SerializeField] Color selectedImageColor;
    [SerializeField] List<Image> albumImages;
    [SerializeField] List<Button> albumImageBtns;
    [SerializeField] List<Button> albumImageDeleteBtns;

    [Header("Image Full Screen View")]
    [SerializeField] GameObject fullScreenImagePanel;
    [SerializeField] Image fullScreenImage;

    [Header("Sprite References")]
    [SerializeField] Sprite uploadImageSprite;
    Sprite avatarImgDefaultSprite;
    Sprite uploadErrorImageSprite;

    string[] albumURLs = new string[9];
    string avatarImageUrl = "";
    bool dataUpdated = false;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        avatarImgDefaultSprite = SpriteReferences.references.defaultAvatarSprite;
        uploadErrorImageSprite = SpriteReferences.references.errorSprite;

        // Loading Local User profile as soon as its logged in to save local user's country and gender data
        AuthenticationManager.manager.OnPlayerLoggedIn += () => LoadUserProfile(PlayerData.PlayfabID);
    }

    #endregion

    #region Save Local Player Profile Data

    private void SaveLocalPlayerProfileData()
    {
        if (!dataUpdated) return;

        AlbumData data = new()
        {
            imageURLs = albumURLs.ToArray(),
            avatarImageURL = avatarImageUrl
        };
        string jsonData = JsonUtility.ToJson(data);

        var saveProfileDataReq = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                {PlayfabDataKeys.PlayerAlbumData, jsonData}
            },
            Permission = UserDataPermission.Public
        };
        PlayFabClientAPI.UpdateUserData(saveProfileDataReq, (res) =>
        {
            dataUpdated = false;
            OnLoadingNewProfileOrUpdatingProfile?.Invoke(PlayerData.PlayfabID);
        }, err => OnError(err, PlayerData.PlayfabID));
    }

    #endregion

    #region Load Player Profile Data

    /// <summary>
    /// Call this function to load anyone's data using their playfab ID
    /// </summary>
    public void LoadUserProfile(string playfabID)
    {
        if (string.IsNullOrEmpty(playfabID)) return;

        uiManager.ResetLoadingBarProgress();
        avatarImg.sprite = SpriteReferences.references.defaultAvatarSprite;
        idTxt.text = "";
        usernameTxt.text = "";
        sendFriendRequestButton.onClick.RemoveAllListeners();
        sendFriendRequestButton.onClick.AddListener(() => FriendManager.manager.SendFriendRequestViaPlayfab(playfabID));

        bool isLocalPlayer = playfabID.Equals(PlayerData.PlayfabID);
        sendFriendRequestButton.gameObject.SetActive(!isLocalPlayer);

        sendFriendAcceptStatusToggle.interactable = isLocalPlayer;
        FriendManager.manager.CheckForFriendRequestStatus(playfabID, (on) => sendFriendAcceptStatusToggle.isOn = on);
        sendFriendAcceptStatusToggle.onValueChanged.RemoveAllListeners();
        sendFriendAcceptStatusToggle.onValueChanged.AddListener(on => FriendManager.manager.ToggleRequestStatus(on, playfabID));

        ProfileFetcher.FetchAndSetUserNameWithTag(playfabID, idTxt);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(playfabID, usernameTxt);


        var getPlayerProfileReq = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.PlayerAlbumData, PlayfabDataKeys.PlayerProfile },
            PlayFabId = playfabID
        };

        PlayFabClientAPI.GetUserData(getPlayerProfileReq, (res) => OnPlayerProfileDataRecieved(res, isLocalPlayer), (err) => OnError(err, playfabID));
        OnLoadingNewProfileOrUpdatingProfile?.Invoke(playfabID);
    }

    private async void OnPlayerProfileDataRecieved(GetUserDataResult result, bool isLocalPlayerData)
    {
        Debug.Log("OnPlayerProfileDataRecieved  "+ isLocalPlayerData);
        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerProfile) && SetPlayerProfileManager.manager.IsPlayerProfileCorrect(result.Data[PlayfabDataKeys.PlayerProfile].Value))
        {
            string data = result.Data[PlayfabDataKeys.PlayerProfile].Value;
            ProfileData profile = JsonUtility.FromJson<ProfileData>(data);

            countryImg.sprite = CountryDataReferences.reference.GetCountryFromIndex((int)profile.Country);

            if (isLocalPlayerData)
            {
                PlayerData.Country = (int)profile.Country;
                PlayerData.Gender = (int)profile.Gender;
            }
        }
        else if(isLocalPlayerData)
        {
            UIManager.manager.OpenPanel(UIManager.manager.setPlayerProfilePanel, UIManager.manager.lobbyUI);
        }

        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerAlbumData) && JsonUtility.FromJson<AlbumData>(result.Data[PlayfabDataKeys.PlayerAlbumData].Value)!=null)
        {
            AlbumData urls = JsonUtility.FromJson<AlbumData>(result.Data[PlayfabDataKeys.PlayerAlbumData].Value);
            albumURLs = urls.imageURLs;

            // Set Album Images
            for (int i = 0; i < albumURLs.Length; i++)
            {
                if (!string.IsNullOrEmpty(albumURLs[i]))
                {
                    RectTransformExtensions.SetAll(albumImages[i].rectTransform, 0f, 0f, 0f, 0f);
                    albumImages[i].GetComponent<AspectRatioFitter>().enabled = true;
                    await ImageManager.DownloadAndSetRemoteTextureToImage(albumURLs[i], albumImages[i]);
                }
                else
                {
                    albumImages[i].sprite = uploadImageSprite;
                    albumImages[i].GetComponent<AspectRatioFitter>().enabled = false;
                    RectTransformExtensions.SetAll(albumImages[i].rectTransform, 70f, 70f, 70f,70f);
                }

                uiManager.UpdateLoadedImagesData();
            }

            if (!string.IsNullOrEmpty(urls.avatarImageURL))
                await ImageManager.DownloadAndSetRemoteTextureToImage(urls.avatarImageURL, avatarImg);
            else
                avatarImg.sprite = SpriteReferences.references.defaultAvatarSprite;

            InitializeAvatarImageButton(isLocalPlayerData);
            InitializeButtons(isLocalPlayerData);
        }
        else
        {
            SetEmptyAlbumData(isLocalPlayerData);
        }
    }

    private void OnError(PlayFabError error, string playfabID)
    {
        Debug.LogError("Profile Manager Error : \nError while loading user profile with playfab ID " + playfabID + " : " + error.GenerateErrorReport());
    }

    private void SetEmptyAlbumData(bool isLocalPlayerData)
    {
        if (isLocalPlayerData)
        {
            string data = JsonUtility.ToJson(new AlbumData(), true);

            var setEmptyAlbumDataReq = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { PlayfabDataKeys.PlayerAlbumData, data } },
                Permission = UserDataPermission.Public
            };

            PlayFabClientAPI.UpdateUserData(setEmptyAlbumDataReq,
            (res) =>
            {
                albumURLs = new string[9];
                InitializeButtons(true);
                InitializeAvatarImageButton(true);
            }, (err) => OnError(err, PlayerData.PlayfabID));
        }

        for (int i = 0; i < albumURLs.Length; i++)
        {
            albumImages[i].GetComponent<AspectRatioFitter>().enabled = false;
            RectTransformExtensions.SetAll(albumImages[i].rectTransform, 70f, 70f, 70f, 70f);
            albumImages[i].sprite = uploadImageSprite;
            avatarImg.sprite = avatarImgDefaultSprite;
            uiManager.UpdateLoadedImagesData();
        }
    }

    #endregion

    #region Avatar image button callbacks

    private void InitializeAvatarImageButton(bool isLocalPlayerProfile)
    {
        if (!isLocalPlayerProfile)
        {
            avatarImgDeleteButton.SetActive(false);
            avatarImgDeleteButton.SetActive(false);
            avatarImgDeleteButton.GetComponent<Button>().onClick.RemoveAllListeners();
            avatarImgButton.onClick.RemoveAllListeners();

            if (avatarImg.sprite != avatarImgDefaultSprite && IAPManager.manager.IsSubscriptionActive)
                avatarImgButton.onClick.AddListener(() => OpenImageInFullScreen(avatarImg.sprite));

            return;
        }

        if (avatarImg.sprite == avatarImgDefaultSprite)
        {
            avatarImgButton.onClick.RemoveAllListeners();
            avatarImgButton.onClick.AddListener(OnClick_UploadAvatarImage);
            avatarImgDeleteButton.SetActive(false);
        }
        else
        {
            avatarImgButton.onClick.RemoveAllListeners();
            avatarImgDeleteButton.SetActive(true);
            avatarImgDeleteButton.GetComponent<Button>().onClick.RemoveAllListeners();
            avatarImgDeleteButton.GetComponent<Button>().onClick.AddListener(()=>
            {
                avatarImg.sprite = avatarImgDefaultSprite;
                avatarImageUrl = "";
                dataUpdated = true;
                InitializeAvatarImageButton(isLocalPlayerProfile);
                SaveLocalPlayerProfileData();
            });
        }
    }

    private void OnClick_UploadAvatarImage()
    {
        NativeGallery.GetImageFromGallery(async (path) =>
        {
            if (string.IsNullOrEmpty(path)) return;

            ImageManager.GetAndSetLocalTextureToImage(path, avatarImg);

            string url = await ImageManager.UploadImage(path);
            avatarImageUrl = url;
            dataUpdated = true;
            InitializeAvatarImageButton(true);
            SaveLocalPlayerProfileData();
        });
    }

    #endregion

    #region Button Callbacks

    private void OnClick_UploadImage(int index)
    {

        Debug.Log(index);

        albumImages[index].color = selectedImageColor;
        albumImageDeleteBtns[index].interactable = false;

        NativeGallery.GetImageFromGallery(async (path) =>
        {
            if (string.IsNullOrEmpty(path)) return;

            ImageManager.GetAndSetLocalTextureToImage(path, albumImages[index]);

            string url = await ImageManager.UploadImage(path);

            if (!string.IsNullOrEmpty(url))
            {
                albumURLs[index] = url;
                dataUpdated = true;
                albumImages[index].GetComponent<AspectRatioFitter>().enabled = true;
                RectTransformExtensions.SetAll(albumImages[index].rectTransform, 0f, 0f, 0f, 0f);
            }
            else
            {
                albumImages[index].GetComponent<AspectRatioFitter>().enabled = false;
                RectTransformExtensions.SetAll(albumImages[index].rectTransform, 70f, 70f, 70f, 70f);
                albumImages[index].sprite = uploadErrorImageSprite;
            }

            InitializeButtons(true);
            albumImages[index].color = Color.white;
            albumImageDeleteBtns[index].interactable = true;
            SaveLocalPlayerProfileData();
        });
    }

    private void OnClick_DeleteImage(int index)
    {
        dataUpdated = true;
        albumImages[index].sprite = uploadImageSprite;
        albumImages[index].GetComponent<AspectRatioFitter>().enabled = false;
        RectTransformExtensions.SetAll(albumImages[index].rectTransform, 70f, 70f, 70f, 70f);
        albumURLs[index] = string.Empty;
        albumImages[index].sprite = uploadImageSprite;
        InitializeButtons(true);
        SaveLocalPlayerProfileData();
    }

    #endregion

    #region Image Full Screen View

    private void OpenImageInFullScreen(Sprite spr)
    {
        Debug.Log("OpenImageInFullScreen");
        fullScreenImage.sprite = spr;
        UIAnimationManager.manager.PopUpPanel(fullScreenImagePanel);
    }

    public void OnClick_CloseFullScreenImageButton()
    {
        UIAnimationManager.manager.PopDownPanel(fullScreenImagePanel);
    }

    #endregion

    #region Helper Functions

    void InitializeButtons(bool isLocalPlayerProfile)
    {
        Debug.Log("InitializeButtons "+ isLocalPlayerProfile);

        if (isLocalPlayerProfile)
        {
            for (int i = 0; i < albumURLs.Length; i++)
            {
                int index = i;
                albumImageBtns[i].onClick.RemoveAllListeners();

                if (!string.IsNullOrEmpty(albumURLs[i]) && !string.IsNullOrWhiteSpace(albumURLs[i]))
                {
                    albumImageBtns[i].onClick.AddListener(() => OpenImageInFullScreen(albumImages[index].sprite));
                    albumImageDeleteBtns[i].gameObject.SetActive(true);
                    albumImages[i].color = Color.white;
                    albumImageDeleteBtns[i].onClick.RemoveAllListeners();
                    albumImageDeleteBtns[i].onClick.AddListener(() => OnClick_DeleteImage(index));
                }
                else
                {
                    albumImageBtns[i].interactable = IAPManager.manager.IsSubscriptionActive;
                    albumImageBtns[i].onClick.RemoveAllListeners();
                    albumImageBtns[i].onClick.AddListener(() => OnClick_UploadImage(index));
                    albumImageDeleteBtns[i].gameObject.SetActive(false);
                    albumImages[i].color = Color.white;
                }
            }
            return;
        }

        Debug.Log(albumURLs.Length);

        for (int i = 0; i < albumURLs.Length; i++)
        {
            int index = i;
            if (string.IsNullOrEmpty(albumURLs[i]) || string.IsNullOrWhiteSpace(albumURLs[i]))
            {
                Debug.Log("Added Listener" + i);
                albumImageBtns[i].interactable = IAPManager.manager.IsSubscriptionActive;
                albumImageBtns[i].onClick.RemoveAllListeners();
                albumImageBtns[i].onClick.AddListener(()=>OnClick_UploadImage(index));

                albumImageDeleteBtns[i].gameObject.SetActive(false);
                albumImages[i].color = Color.white;
            }
            else
            {
                Debug.Log("Removed Listener "+i);
                albumImageBtns[i].onClick.RemoveAllListeners();
                albumImageBtns[i].interactable = true;
                albumImageDeleteBtns[i].gameObject.SetActive(true);
                albumImages[i].color = Color.white;
                albumImageDeleteBtns[i].onClick.RemoveAllListeners();
                albumImageDeleteBtns[i].onClick.AddListener(() => OnClick_DeleteImage(index));
            }
        }
    }

    // Used by Leadboards Panel when someone visits someone else's profile
    public void SetCountryFlagManually(int country) => countryImg.sprite = CountryDataReferences.reference.GetCountryFromIndex((int)country);

    #endregion
}

[Serializable]
public class AlbumData
{
    public string[] imageURLs = new string[9];
    public string avatarImageURL = "";
}