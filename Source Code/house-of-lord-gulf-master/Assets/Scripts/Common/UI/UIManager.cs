using UnityEngine;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager manager;

    [Header("Animations")]
    public Ease slideInEase;
    public Ease slideOutEase;

    [Header("Panels")]
    public GameObject lobbyUI;
    public GameObject boosterPanel;
    public GameObject authenticationPanel;
    public GameObject setPlayerProfilePanel;
    public GameObject chatPanel;
    public GameObject matchmakingPanel;
    public GameObject gameOverPanel;
    public GameObject leaderboardPanel;

    [Header("Lobby Panels")]
    public LobbyMenuPanel lobbyPanel;
    public LobbyMenuPanel shopPanel;
    public LobbyMenuPanel friendsPanel;
    public LobbyMenuPanel dailySpinPanel;
    public GameObject battlePassPanel;

    private void Awake()
    {
        manager = this;
    }

    public void OpenPanel(GameObject panelToOpen, GameObject fromPanel)
    {
        UIAnimationManager.manager.SlideInPanel(panelToOpen, slideInEase, (SlideDirection)Random.Range(0,2), ()=> { UIAnimationManager.manager.SlideOutPanel(fromPanel, slideOutEase, (SlideDirection)Random.Range(0, 2)); });
    }

    public void OpenPanel(GameObject panelToOpen)
    {
        UIAnimationManager.manager.SlideInPanel(panelToOpen, slideInEase, (SlideDirection)Random.Range(0, 2));
    }

    public void OpenPanel(GameObject panelToOpen, System.Action callback)
    {
        UIAnimationManager.manager.SlideInPanel(panelToOpen, slideInEase, (SlideDirection)Random.Range(0, 2),callback);
    }

    public void OpenPanel(GameObject panelToOpen, GameObject fromPanel, SlideDirection dir)
    {
        UIAnimationManager.manager.SlideInPanel(panelToOpen, slideInEase, dir, () => { UIAnimationManager.manager.SlideOutPanel(fromPanel, slideOutEase, (SlideDirection)Random.Range(0, 2)); });
    }

    public void OpenPanel(GameObject panelToOpen, GameObject fromPanel, SlideDirection dir, System.Action callback)
    {
        UIAnimationManager.manager.SlideInPanel(panelToOpen, slideInEase, dir, () => { callback(); UIAnimationManager.manager.SlideOutPanel(fromPanel, slideOutEase, (SlideDirection)Random.Range(0, 2)); });
    }

    public void ClosePanel(GameObject panelToClose, GameObject panelToShow)
    {
        panelToShow.SetActive(true);
        panelToClose.transform.SetSiblingIndex(panelToShow.transform.GetSiblingIndex() + 1);
        UIAnimationManager.manager.SlideOutPanel(panelToClose, slideOutEase, (SlideDirection)Random.Range(0, 2), () => { UIAnimationManager.manager.SlideInPanel(panelToShow, slideInEase, (SlideDirection)Random.Range(0, 2)); });
    }

    public void ClosePanel(GameObject panelToClose, GameObject panelToShow, System.Action callback)
    {
        panelToShow.SetActive(true);
        panelToClose.transform.SetSiblingIndex(panelToShow.transform.GetSiblingIndex() + 1);
        UIAnimationManager.manager.SlideOutPanel(panelToClose, slideOutEase, (SlideDirection)Random.Range(0, 2), () => { callback(); UIAnimationManager.manager.SlideInPanel(panelToShow, slideInEase, (SlideDirection)Random.Range(0, 2)); });
    }

    public void ClosePanel(GameObject panelToClose)
    {
        UIAnimationManager.manager.SlideOutPanel(panelToClose, slideOutEase, (SlideDirection)Random.Range(0, 2));
    }

    public void ClosePanel(GameObject panelToClose, System.Action callback)
    {
        UIAnimationManager.manager.SlideOutPanel(panelToClose, slideOutEase, (SlideDirection)Random.Range(0, 2), callback);
    }

    public void ClosePanel(GameObject panelToClose, SlideDirection dir, Ease ease = Ease.Unset)
    {
        UIAnimationManager.manager.SlideOutPanel(panelToClose, ease == Ease.Unset ? slideOutEase : ease, dir);
    }
}
