using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button linkAccountBtn;

    [Header("Windows")]
    [SerializeField] GameObject settingsWindow;
    [SerializeField] GameObject linkAccountWindow;

    #region Unity Functions

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += OnPlayerLoggedIn;
    }

    private void OnPlayerLoggedIn()
    {
        //if (!string.IsNullOrEmpty(PlayerData.CustomID))
        //{
        //    linkAccountBtn.interactable = true;
        //}
        //else
        //{
        //    linkAccountBtn.interactable = false;
        //}
    }

    #endregion

    #region Options Window Button Callbacks

    public void OnClick_PrivacyPolicy()
    {

    }

    public void OnClick_Settings()
    {
        UIAnimationManager.manager.PopUpPanel(settingsWindow, DG.Tweening.Ease.OutBounce);
    }

    public void OnClick_Logout()
    {
        AuthenticationManager.manager.Logout();
        UIAnimationManager.manager.PopDownPanel(LobbyUIManager.manager.optionWindowPanel, DG.Tweening.Ease.InBack, () =>
        {
           UIManager.manager.ClosePanel(UIManager.manager.lobbyUI, UIManager.manager.authenticationPanel, ()=> SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        });
    }

    public void OnClick_CopyUserID()
    {
        GUIUtility.systemCopyBuffer = PlayerData.PlayfabID;
    }

    #endregion

    #region Settings Window Button Callbacks

    public void OnClick_LinkAccountWindow()
    {
        UIAnimationManager.manager.PopUpPanel(linkAccountWindow, DG.Tweening.Ease.OutBounce);
    }

    public void OnClick_LinkAccount()
    {
        AuthenticationManager.manager.LinkAccount();
    }

    public void OnClick_LinkAccountClose()
    {
        UIAnimationManager.manager.PopDownPanel(linkAccountWindow, DG.Tweening.Ease.InBack);
    }

    public void OnClick_Settings_Close()
    {
        UIAnimationManager.manager.PopDownPanel(settingsWindow, DG.Tweening.Ease.InBack);
    }
    #endregion
}
