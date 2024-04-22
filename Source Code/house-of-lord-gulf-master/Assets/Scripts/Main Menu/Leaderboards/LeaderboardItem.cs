using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] Image avatarImage;
    [SerializeField] TMPro.TMP_Text usernameText;

    int country = 0;
    string playfabID = "";

    public void SetupItem(string playfabID, int country)
    {
        this.country = country;
        this.playfabID = playfabID;
        ProfileFetcher.FetchAndSetAvatarImage(playfabID, avatarImage);

        if (usernameText != null)
            ProfileFetcher.FetchAndSetUserNameWithoutTag(playfabID, usernameText);
    }

    public void ResetData()
    {
        playfabID = "";
        avatarImage.sprite = SpriteReferences.references.defaultAvatarSprite;

        if(usernameText!=null)
            usernameText.text = "";
    }

    public void OpenProfile()
    {
        if (!IAPManager.manager.IsSubscriptionActive) return;

        if (!string.IsNullOrEmpty(playfabID))
        {
            if (country != 0)
                ProfileManager.manager.SetCountryFlagManually(country);
            else
                Debug.LogError("Leaderboard Item Error: Country Not Initialized: Playfab ID : " + playfabID);

            ProfileManager.manager.LoadUserProfile(playfabID);
            LeaderboardsUIManager.manager.OnClick_Leaderboard_Back(()=>UIAnimationManager.manager.PopUpPanel(LobbyUIManager.manager.profileWindowPanel));
        }
    }

    public void SendFriendRequest()
    {
        if (!string.IsNullOrEmpty(playfabID))
            FriendManager.manager.SendFriendRequestViaPlayfab(playfabID, () =>
            {
            }, () =>
            {
            });
    }
}
