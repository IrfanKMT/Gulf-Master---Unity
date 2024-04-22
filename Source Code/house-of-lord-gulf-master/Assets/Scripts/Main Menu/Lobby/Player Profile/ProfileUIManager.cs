using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ProfileUIManager : MonoBehaviour
{
    [SerializeField] Ease popupEase;

    [Header("Album")]
    [SerializeField] GameObject loadingPanel;
    [SerializeField] Image loadingRadialFillImage;
    [SerializeField] GameObject[] albumImages;

    float targetData = 0;
    bool loaded = false;

    private void OnEnable()
    {
        if(loaded)
            StartCoroutine(PlayAlbumImagePopupAnimation());
    }

    #region Album

    void Update()
    {
        if (!loaded)
        {
            loadingRadialFillImage.fillAmount = Mathf.Clamp01(Mathf.MoveTowards(loadingRadialFillImage.fillAmount, targetData, Time.deltaTime));
            loaded = loadingRadialFillImage.fillAmount == 1f;

            if (loaded)
            {
                UIAnimationManager.manager.PopDownPanel(loadingPanel, Ease.InBack);
                StartCoroutine(PlayAlbumImagePopupAnimation());
            }
        }
    }

    public void ResetLoadingBarProgress()
    {
        targetData = 0;
        UIAnimationManager.manager.PopUpPanel(loadingPanel);
        loaded = false;
        loadingRadialFillImage.fillAmount = 0f;

        foreach (var btn in albumImages)
            btn.SetActive(false);
    }

    public void UpdateLoadedImagesData()
    {
        targetData += 1f / 8.5f;
    }

    IEnumerator PlayAlbumImagePopupAnimation()
    {
        foreach (var btn in albumImages)
            btn.SetActive(false);

        foreach (var btn in albumImages)
        {
            UIAnimationManager.manager.PopUpPanel(btn, popupEase);
            yield return new WaitForSeconds(0.15f);
        }
    }
    #endregion
}
