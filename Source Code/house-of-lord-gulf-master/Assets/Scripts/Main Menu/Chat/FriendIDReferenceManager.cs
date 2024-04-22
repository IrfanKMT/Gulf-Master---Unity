using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Threading.Tasks;
using StreamChat.Core.Helpers;
using System.Collections;

public class FriendIDReferenceManager : MonoBehaviour
{
    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += OnPlayerLoggedIn;
    }

    private async void OnPlayerLoggedIn()
    {
        string playfabID = await GetPlayfabIDWithFriendID(PlayerData.Username);
        if (string.IsNullOrEmpty(playfabID))
        {
            SavePlayfabIDWithFriendID(PlayerData.Username, PlayerData.PlayfabID);
        }
    }

    public static async Task<string> GetPlayfabIDWithFriendID(string friendID)
    {
        friendID = friendID.Replace("#", "");
        return await FirebaseDatabase.DefaultInstance.GetReference("PlayFabID").GetValueAsync().ContinueWithOnMainThread((task) => GetPlayfabIDwithFriendTagContinuation(task, friendID));
    }

    static string GetPlayfabIDwithFriendTagContinuation(Task<DataSnapshot> task, string friendID)
    {
        if (task.IsFaulted)
        {
            print("Error in Firebase Playfab ID Reference");
        }
        else if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;
            foreach(DataSnapshot child in snapshot.Children) 
            {
                if (child.Key.Equals(friendID) || child.Key.Equals(friendID.ToLower()))
                {
                    IDictionary dictUser = (IDictionary)child.Value;
                    return (string) dictUser["PlayfabID"];
                }           
            }
        }
        return "";
    }

    private static void SavePlayfabIDWithFriendID(string friendID, string id)
    {
        friendID = friendID.Replace("#", "");
        PlayfabIDReference data = new()
        {
            PlayfabID = id
        };
        string json = JsonUtility.ToJson(data);
        FirebaseDatabase.DefaultInstance.RootReference.Child("PlayFabID").Child(friendID).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            task.LogExceptionsOnFailed();
            if (task.IsCompleted)
                print("Playfab ID saved on firebase.");
            else
                print("Failed to save Playfab ID on firebase"); });

    }
}

class PlayfabIDReference
{
    public string PlayfabID;
}
