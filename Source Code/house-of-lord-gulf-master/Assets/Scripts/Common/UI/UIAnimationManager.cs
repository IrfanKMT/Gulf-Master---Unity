using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

public class UIAnimationManager : MonoBehaviour
{
    public static UIAnimationManager manager;

    [SerializeField][Range(0.1f,5f)] float fadeTime = 0.5f;
    [SerializeField][Range(0.1f,5f)] float slideTime = 1f;

    List<GameObject> processingPanels = new();
    List<GameObject> slidInPanels = new();


    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        slidInPanels.Add(UIManager.manager.authenticationPanel);        
        slidInPanels.Add(UIManager.manager.lobbyPanel.gameObject);
        slidInPanels.Add(ShopUIManager.manager.coinsCatalog);
        slidInPanels.Add(FriendUIManager.manager.friendListPanel);
    }

    #region Popping

    public void PopUpPanel(GameObject panel, Ease ease = Ease.Unset)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(1f, fadeTime).SetEase(ease == Ease.Unset ? Ease.OutBounce : ease).OnComplete(() => { processingPanels.Remove(panel); });
    }

    public void PopUpPanel(GameObject panel, Ease ease, Action callback)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(1f, fadeTime).SetEase(ease).OnComplete(() => { processingPanels.Remove(panel); callback(); });
    }

    public void PopUpPanel(GameObject panel, Action callback)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(1f, fadeTime).SetEase(Ease.OutBounce).OnComplete(() => { processingPanels.Remove(panel); callback(); });
    }

    public void PopDownPanel(GameObject panel, Ease ease = Ease.Unset)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        panel.transform.DOScale(Vector3.zero, fadeTime).SetEase(ease == Ease.Unset ? Ease.InBack : ease).OnComplete(() => { processingPanels.Remove(panel); panel.SetActive(false); });
    }

    public void PopDownPanel(GameObject panel, Action callback)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        panel.transform.DOScale(Vector3.zero, fadeTime).SetEase(Ease.InBack).OnComplete(() =>
        {
            processingPanels.Remove(panel);
            panel.SetActive(false);
            callback();
        });
    }

    public void PopDownPanel(GameObject panel, Ease ease, Action callback)
    {
        if (processingPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        panel.transform.DOScale(Vector3.zero, fadeTime).SetEase(ease).OnComplete(() =>
        {
            processingPanels.Remove(panel);
            panel.SetActive(false);
            callback();
        });
    }

    #endregion

    #region Sliding

    public void SlideInPanel(GameObject panel, Ease ease, SlideDirection dir)
    {
        if (processingPanels.Contains(panel) || slidInPanels.Contains(panel)) return;
        panel.transform.SetAsLastSibling();
        processingPanels.Add(panel);

        int x = dir == SlideDirection.Left ? -Screen.width: Screen.width;
        if (panel.TryGetComponent(out RectTransform rect))
            x = dir == SlideDirection.Left ? (int)-(rect.rect.width) : (int)rect.rect.width;

        panel.transform.localPosition = new Vector3(x, 0, 0);
        panel.SetActive(true);
        panel.transform.DOMoveX(0, slideTime).SetEase(ease).OnComplete(()=>
        {
            processingPanels.Remove(panel);
            slidInPanels.Add(panel);
        });
    }


    public void SlideInPanel(GameObject panel, Ease ease, SlideDirection dir, Action callback)
    {
        if (processingPanels.Contains(panel) || slidInPanels.Contains(panel)) return;
        panel.transform.SetAsLastSibling();
        processingPanels.Add(panel);

        int x = dir == SlideDirection.Left ? -Screen.width : Screen.width;
        if (panel.TryGetComponent(out RectTransform rect))
            x = dir == SlideDirection.Left ? (int)-(rect.rect.width) : (int)rect.rect.width;

        panel.transform.localPosition = new Vector3(x, 0, 0);
        panel.SetActive(true);
        panel.transform.DOMoveX(0, slideTime).SetEase(ease).OnComplete(() => { callback(); processingPanels.Remove(panel); slidInPanels.Add(panel); }) ;
    }

    public void SlideOutPanel(GameObject panel, Ease ease, SlideDirection dir)
    {
        if (processingPanels.Contains(panel) || !slidInPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        int x = dir == SlideDirection.Left ? -Screen.width : Screen.width;
        if (panel.TryGetComponent(out RectTransform rect))
            x = dir == SlideDirection.Left ? (int)-(rect.rect.width) : (int)rect.rect.width;

        panel.transform.DOLocalMoveX(x, slideTime).SetEase(ease).OnComplete(() => { panel.SetActive(false); processingPanels.Remove(panel); if(slidInPanels.Contains(panel)) slidInPanels.Remove(panel); } );
    }


    public void SlideOutPanel(GameObject panel, Ease ease, SlideDirection dir, Action callback)
    {
        if (processingPanels.Contains(panel) || !slidInPanels.Contains(panel)) return;
        processingPanels.Add(panel);

        int x = dir == SlideDirection.Left ? -Screen.width : Screen.width;
        if (panel.TryGetComponent(out RectTransform rect))
            x = dir == SlideDirection.Left ? (int) -(rect.rect.width) : (int) rect.rect.width;

        panel.transform.DOLocalMoveX(x, slideTime).SetEase(ease).OnComplete(() => { callback(); panel.SetActive(false); processingPanels.Remove(panel); if (slidInPanels.Contains(panel)) slidInPanels.Remove(panel); });
    }

    public void ClosePanelWithoutAnimation(GameObject panel)
    {
        if (slidInPanels.Contains(panel))
        {
            slidInPanels.Remove(panel);
            panel.SetActive(false);
        }
    }

    #endregion

    #region Helper Functions

    public bool IsPanelSliding(GameObject panel)
    {
        return processingPanels.Contains(panel);
    }

    #endregion
}

public enum SlideDirection
{
    Left,
    Right
}
