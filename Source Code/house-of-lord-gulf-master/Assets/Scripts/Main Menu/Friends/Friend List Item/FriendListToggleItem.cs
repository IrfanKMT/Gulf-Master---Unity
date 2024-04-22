using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendListToggleItem : MonoBehaviour
{
     public Toggle toggle;
    [SerializeField] TMP_Text listNameText;

    Action OnToggleCallback;
    Action OffToggleCallback;

    public void SetupList(string listName, Action onCallback, Action offCallback, bool isOn)
    {
        listNameText.text = listName;
        OnToggleCallback = onCallback;
        OffToggleCallback = offCallback;
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        toggle.SetIsOnWithoutNotify(isOn);
    }

    private void OnToggleValueChanged(bool on)
    {
        if (on)
        {
            OnToggleCallback();
        }
        else
        {
            OffToggleCallback();
        }
        StartCoroutine(WaitToMakeToggleInteractable());
    }

    IEnumerator WaitToMakeToggleInteractable()
    {
        toggle.interactable = false;
        yield return new WaitForSeconds(10);
        toggle.interactable = true;
    }
}
