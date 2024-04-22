using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
//using Photon.Pun;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager manager;

    [Header("Main Menu")]
    [SerializeField] Button playButton;

    [Header("Animations")]
    [SerializeField] Ease popupEase;
    [SerializeField] Ease popdownEase;
    [SerializeField] Ease slideEase;

    [Header("Windows")]
    [SerializeField] GameObject playWindowPanel;
    [SerializeField] GameObject gameModeSelectionPanel;
    public GameObject profileWindowPanel;
    public GameObject optionWindowPanel;

    internal bool isVoiceOn = false; //Used By Network Manager

    #region Unity Functions

    private void Awake()
    {
        manager = this;
        AuthenticationManager.manager.OnPlayerLoggedIn += InitializeLobby;
    }

    //private void Update()
    //{
    //    SetPlayButtonEnableDisable();
    //}

    #endregion

    #region Lobby Header Button Clicks

    public void Lobby_Header_OnClick_ProfileButton()
    {
        ProfileManager.manager.LoadUserProfile(PlayerData.PlayfabID);
        UIAnimationManager.manager.PopUpPanel(profileWindowPanel, popupEase);
    }

    public void Lobby_Header_OnClick_OptionsButton()
    {
        UIAnimationManager.manager.PopUpPanel(optionWindowPanel, popupEase);
    }

    #endregion

    #region Play Panel

    public void OnClick_Lobby_PlayButton()
    {
        UIAnimationManager.manager.PopUpPanel(gameModeSelectionPanel);
    }

    public void OnClickTwoPlayer()
    {
        UIAnimationManager.manager.PopDownPanel(gameModeSelectionPanel);
        UIAnimationManager.manager.PopUpPanel(playWindowPanel);
        MatchMakingManager.manager.matchType = MatchType.TwoPlayer;
        MatchMakingManager.manager.buildServerID = "17cb105d-9748-491f-97aa-a2a9c8f0639b";
    }

    public void OnClickFourPlayer()
    {
        UIAnimationManager.manager.PopDownPanel(gameModeSelectionPanel);
        UIAnimationManager.manager.PopUpPanel(playWindowPanel);
        MatchMakingManager.manager.matchType = MatchType.FourPlayer;
        MatchMakingManager.manager.buildServerID = "f5ce7731-84a1-46c3-82b2-1e86b55db7ee";
    }

    public void OnClick_Play_PlayWithVoiceButton()
    {
        isVoiceOn = true;
        UIManager.manager.OpenPanel(UIManager.manager.boosterPanel);
        OnClick_Play_ClosePanel();
    }

    public void OnClick_Play_PlayWithoutVoiceButton()
    {
        isVoiceOn = false;
        UIManager.manager.OpenPanel(UIManager.manager.boosterPanel);
        OnClick_Play_ClosePanel();
    }

    public void OnClick_Play_ClosePanel()
    {
        if (playWindowPanel.activeInHierarchy)
        {
            UIAnimationManager.manager.PopDownPanel(playWindowPanel);
        }
        else
        {
            UIAnimationManager.manager.PopDownPanel(gameModeSelectionPanel);
        }
    }

    #endregion

    #region Lobby Footer Button Clicks

    public void OnClick_Shop()
    {
        OpenPanel(UIManager.manager.shopPanel);
    }

    public void OnClick_Home()
    {
        //Hide All Panels
        OpenPanel(UIManager.manager.lobbyPanel);
    }

    public void OnClick_Friends()
    {
        //Hide All Panels
        OpenPanel(UIManager.manager.friendsPanel);
    }

    public void OnClick_DailySpin()
    {
        //Hide All Panels
        OpenPanel(UIManager.manager.dailySpinPanel);
    }

    private void OpenPanel(LobbyMenuPanel panelToOpen)
    {
        LobbyMenuPanel visiblePanel = GetVisiblePanel();

        if (visiblePanel != null & visiblePanel.pannelIndex != panelToOpen.pannelIndex)
        {
            UIAnimationManager.manager.SlideOutPanel(visiblePanel.gameObject, slideEase, visiblePanel.pannelIndex > panelToOpen.pannelIndex ? SlideDirection.Right : SlideDirection.Left);
            UIAnimationManager.manager.SlideInPanel(panelToOpen.gameObject, slideEase, visiblePanel.pannelIndex < panelToOpen.pannelIndex ? SlideDirection.Right : SlideDirection.Left);
        }
    }

    #endregion 

    #region Profile Popup Button Clicks

    public void ProfileWindow_OnClick_CloseButton()
    {
        UIAnimationManager.manager.PopDownPanel(profileWindowPanel, popdownEase);
    }

    #endregion

    #region Options Popup Button Clicks

    public void OptionsWindow_OnClick_CloseButton()
    {
        UIAnimationManager.manager.PopDownPanel(optionWindowPanel, popdownEase);
    }

    #endregion

    #region Battle Pass UI

    public void OnClick_BattlePass()
    {
        BattlePassUIManager.manager.SnapToProfile();
        UIManager.manager.OpenPanel(UIManager.manager.battlePassPanel);
    }

    public void OnClick_Close_BattlePass()
    {
        UIManager.manager.ClosePanel(UIManager.manager.battlePassPanel);
    }

    #endregion

    #region Initializing

    private void InitializeLobby()
    {
        //playButton.interactable = NetworkManager.manager.IsInLobby();
        profileWindowPanel.SetActive(false);
        optionWindowPanel.SetActive(false);
        UIManager.manager.shopPanel.gameObject.SetActive(false);
        UIManager.manager.battlePassPanel.SetActive(false);
        UIManager.manager.friendsPanel.gameObject.SetActive(false);
        UIManager.manager.dailySpinPanel.gameObject.SetActive(false);
        //NetworkManager.manager.OnPlayerJoinedLobby += () => playButton.interactable = true;
    }

    #endregion

    #region Helper Functions

    public LobbyMenuPanel GetVisiblePanel()
    {
        if (UIManager.manager.shopPanel.gameObject.activeInHierarchy && !UIAnimationManager.manager.IsPanelSliding(UIManager.manager.shopPanel.gameObject))
            return UIManager.manager.shopPanel;

        if (UIManager.manager.dailySpinPanel.gameObject.activeInHierarchy && !UIAnimationManager.manager.IsPanelSliding(UIManager.manager.dailySpinPanel.gameObject))
            return UIManager.manager.dailySpinPanel;

        if (UIManager.manager.lobbyPanel.gameObject.activeInHierarchy && !UIAnimationManager.manager.IsPanelSliding(UIManager.manager.lobbyPanel.gameObject))
            return UIManager.manager.lobbyPanel;

        if (UIManager.manager.friendsPanel.gameObject.activeInHierarchy && !UIAnimationManager.manager.IsPanelSliding(UIManager.manager.friendsPanel.gameObject))
            return UIManager.manager.friendsPanel;

        return null;
    }

    private void SetPlayButtonEnableDisable()
    {

#if ENABLE_VIVOX
        //playButton.interactable = PhotonNetwork.InLobby && VivoxManager.manager.isLoggedIn;
#else
        //playButton.interactable = PhotonNetwork.InLobby;
#endif

    }

    #endregion

}