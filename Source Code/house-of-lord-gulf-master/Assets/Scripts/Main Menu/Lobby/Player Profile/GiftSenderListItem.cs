using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class GiftSenderListItem : MonoBehaviour
{
    [SerializeField] Image avatarImage;
    [SerializeField] TMP_Text username;
    [SerializeField] TMP_Text amount;
    [SerializeField] TMP_Text timeText;
    DateTime sentDate;

    public void Setup(string playfabID, string amount, string sentDateStr)
    {
        Debug.Log("Setup data.date" + sentDateStr);
        ProfileFetcher.FetchAndSetAvatarImage(playfabID, avatarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(playfabID, username);
        this.amount.text = amount + " Coins";

        if(DateTime.TryParse(sentDateStr, out DateTime sentDate))
        {
            this.sentDate = sentDate;
            InvokeRepeating(nameof(UpdateTimeText), 0, 60);
        }
    }

    private void UpdateTimeText()
    {
        System.DateTime dateTime = System.DateTime.Parse(TimeFormat.GetFormattedUTCDate());
        TimeSpan timePassed = dateTime - sentDate;
        timeText.text = Mathf.FloorToInt((float)timePassed.TotalDays) > 365 ? Mathf.FloorToInt(Mathf.FloorToInt((float)timePassed.TotalDays)/365) + " years ago" : Mathf.FloorToInt((float)timePassed.TotalDays)>0 ? Mathf.FloorToInt((float)timePassed.TotalDays) + " days ago" : Mathf.FloorToInt((float)timePassed.TotalHours) > 0 ? Mathf.FloorToInt((float)timePassed.TotalHours) + " hours ago" : Mathf.FloorToInt((float)timePassed.TotalMinutes) + " minutes ago";
    }
}
