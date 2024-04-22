using UnityEngine;
using DG.Tweening;

public class AuthenticationUIManager : MonoBehaviour
{
    [Header("Animations")]
    [SerializeField] Ease popupEase;
    [SerializeField] Ease popdownEase;

    [Header("Panels")]
    [SerializeField] GameObject signInPanel;
    [SerializeField] GameObject signUpPanel;
    [SerializeField] GameObject forgotPasswordPanel;
    [SerializeField] GameObject accountRecoveryMailSentPanel;

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedOut += OnPlayerLoggedOut;
    }

    private void OnPlayerLoggedOut()
    {
        UIAnimationManager.manager.PopDownPanel(signInPanel, popdownEase);
        UIAnimationManager.manager.PopDownPanel(forgotPasswordPanel, popdownEase);
        UIAnimationManager.manager.PopDownPanel(signUpPanel, popdownEase);
        UIAnimationManager.manager.PopDownPanel(accountRecoveryMailSentPanel, popdownEase);
    }

    public void OnClick_SignInPanelButton()
    {
        UIAnimationManager.manager.PopUpPanel(signInPanel, popupEase);
    }

    public void OnClick_SignUpPanelButton()
    {
        UIAnimationManager.manager.PopUpPanel(signUpPanel, popupEase);
    }

    public void OnClick_ForgotPasswordPanelButton()
    {
        UIAnimationManager.manager.PopUpPanel(forgotPasswordPanel, popupEase);
    }

    public void OnClick_CloseButton()
    {
        UIAnimationManager.manager.PopDownPanel(signInPanel, popdownEase);
        UIAnimationManager.manager.PopDownPanel(signUpPanel, popdownEase);
    }

    public void OnClick_CloseAccountRecoveryPanel()
    {
        UIAnimationManager.manager.PopDownPanel(forgotPasswordPanel, popdownEase);
        UIAnimationManager.manager.PopDownPanel(accountRecoveryMailSentPanel, popdownEase);
    }

    internal void Open_AccountRecoveryEmailSentPanel()
    {
        UIAnimationManager.manager.PopUpPanel(accountRecoveryMailSentPanel, popupEase);
    }

}
