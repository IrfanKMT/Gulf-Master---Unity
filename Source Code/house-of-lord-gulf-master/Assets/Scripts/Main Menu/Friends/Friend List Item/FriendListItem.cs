using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FriendListItem : MonoBehaviour
{
    [SerializeField] TMP_Text listNameText;
    [SerializeField] TMP_InputField editNameTF;
    List<string> playfabIDs;

    public void Setup(string name, List<string> playfabIDs)
    {
        listNameText.text = name;
        this.playfabIDs = playfabIDs;
        editNameTF.onTouchScreenKeyboardStatusChanged.AddListener(OnClick_EditName);
    }

    public void OnClick()
    {
        foreach (Transform child in FriendListsManager.manager.listItemContainer)
            Destroy(child.gameObject);

        foreach (string playfabID in playfabIDs)
        {
            GameObject item = Instantiate(FriendUIManager.manager.friendListItem, FriendListsManager.manager.listItemContainer);
            FriendItem friendItem = item.GetComponent<FriendItem>();

            if (friendItem == null)
            {
                Debug.LogError("Friend Item Not Found On GameObject : " + FriendUIManager.manager.friendListItem.name);
                Destroy(item);
                return;
            }

            friendItem.Setup(playfabID);
            UIAnimationManager.manager.PopUpPanel(item);
        }
    }

    public void OnClick_EditName(TouchScreenKeyboard.Status status)
    {
        if(status == TouchScreenKeyboard.Status.Done)
        {
            FriendListsManager.manager.UpdateListNameData(listNameText.text, editNameTF.text);
            editNameTF.text = "";
        }
    }
}
