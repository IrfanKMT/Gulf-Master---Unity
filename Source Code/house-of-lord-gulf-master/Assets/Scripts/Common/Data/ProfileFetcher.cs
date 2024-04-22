using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class ProfileFetcher
{
    public static void FetchAndSetUserNameWithTag(string playfabID, TMPro.TMP_Text nameTxt)
    {
#if !UNITY_SERVER
        var getDisplayNameRequest = new GetAccountInfoRequest { PlayFabId = playfabID };
        PlayFabClientAPI.GetAccountInfo(getDisplayNameRequest, (res) =>
        {
            string username = res.AccountInfo.TitleInfo.DisplayName;
            if (string.IsNullOrEmpty(username))
            {
                nameTxt.text = "Loading...";
                Debug.LogError("Profile Fetcher : Error Fetching Account Info. DisplayName is empty or null");
                return;
            }
            else
            {
                username = username[0..^4] + "#" + username.Remove(0, username.Length - 4);
                nameTxt.text = username;
            }
        },
        err =>
        {
            Debug.LogError("Profile Fetcher : Error Fetching Account Info. Playfab ID : " + playfabID + "\nError Message : " + err.ErrorMessage + "\nError Details : " + err.GenerateErrorReport());
        });
#endif
    }

    public static void FetchAndSetUserNameWithoutTag(string playfabID, TMPro.TMP_Text nameTxt)
    {
#if !UNITY_SERVER
        var getDisplayNameRequest = new GetAccountInfoRequest { PlayFabId = playfabID };
        PlayFabClientAPI.GetAccountInfo(getDisplayNameRequest, (res) =>
        {
            string username = res.AccountInfo.TitleInfo.DisplayName;
            if (string.IsNullOrEmpty(username))
            {
                nameTxt.text = "NameNotFound";
                Debug.LogError("Profile Fetcher : Error Fetching Account Info. DisplayName is empty or null");
            }

            username = username[0..^4];
            nameTxt.text = username;
        },
        err =>
        {
            Debug.LogError("Profile Fetcher : Error Fetching Account Info. Error Message : " + err.ErrorMessage + "\nError Details : " + err.GenerateErrorReport());
        });
#endif
    }

    public static void FetchAndSetAvatarImage(string playfabID, Image avatarImage)
    {
#if !UNITY_SERVER
        var getAvatarImageDataRequest = new GetUserDataRequest
        {
            Keys = new List<string> { PlayfabDataKeys.PlayerAlbumData },
            PlayFabId = playfabID
        };
        PlayFabClientAPI.GetUserData(getAvatarImageDataRequest, (res)=> OnPlayerAlbumDataRecieved(res,avatarImage), (err)=> avatarImage.sprite = SpriteReferences.references.defaultAvatarSprite);
#endif
    }

    private static async void OnPlayerAlbumDataRecieved(GetUserDataResult result, Image avatarImage)
    {
#if !UNITY_SERVER
        if (result.Data.ContainsKey(PlayfabDataKeys.PlayerAlbumData))
        {
            string data = result.Data[PlayfabDataKeys.PlayerAlbumData].Value;
            AlbumData album = JsonUtility.FromJson<AlbumData>(data);

            if (album != null)
            {
                string currentAvatarImgUrl = album.avatarImageURL;

                if (!string.IsNullOrWhiteSpace(currentAvatarImgUrl))
                    await ImageManager.DownloadAndSetRemoteTextureToImage(currentAvatarImgUrl, avatarImage);
                else
                    avatarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
            }
            else
                avatarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
        }
        else
            avatarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
#endif
    }
}
