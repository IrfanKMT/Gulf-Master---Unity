using System.Collections;
using UnityEngine;
using System;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager manager;

    [SerializeField] TMP_Text loadingMessageText;
    [SerializeField] TMP_Text loadingPercentText;
    [SerializeField] GameObject loadingBarPanel;
    [SerializeField] RectTransform loader;
    [SerializeField] Vector3 loadingPosition;

    Action LoadCompleteCallback;
    bool isLoading = false;

    private void Awake()
    {
        manager = this;
    }

    public void ShowLoadingBar(string text)
    {
        loadingBarPanel.transform.SetAsLastSibling();
        if (!isLoading)
        {
            bool loaderReached100 = Mathf.MoveTowards(loader.localPosition.x, 8, Time.deltaTime * 2f) >= -5f;
            if (!loaderReached100)
            {
                StopAllCoroutines();
                LoadCompleteCallback?.Invoke();
            }

            StartCoroutine(Load());
            isLoading = true;
            loadingMessageText.text = text;
        }
        else
        {
            Debug.LogError("Loader is busy.\nText : " + text);
        }
    }

    public void UpdateLoadingText(string text)
    {
        loadingMessageText.text = text;
    }

    public void HideLoadingBar(Action action = null)
    {
        LoadCompleteCallback = action;
        isLoading = false;
    }

    IEnumerator Load()
    {
        bool loaderReached100 = false;
        bool loaderReached99 = false;

        loader.localPosition = loadingPosition;
        loadingBarPanel.SetActive(true);

        while (!loaderReached99)
        {
            yield return null;

            float speed = isLoading ? Time.deltaTime/2f : Time.deltaTime*4f;
            float x = Mathf.Lerp(loader.localPosition.x, 8, speed);

            loader.localPosition = new Vector3(x, 0, 0);

            int percent = Mathf.Abs(((int)loadingPosition.x - ((int)loader.localPosition.x)) / 9);
            loadingPercentText.text = percent.ToString() + "%";

            loaderReached99 = x >= -5f;
        }

        while (isLoading)
            yield return null;

        while (!loaderReached100)
        {
            yield return null;

            float x = Mathf.MoveTowards(loader.localPosition.x, 8, Time.deltaTime*2f);
            loader.localPosition = new Vector3(x, 0, 0);

            int percent = Mathf.Abs(((int)loadingPosition.x - ((int)loader.localPosition.x)) / 9);
            loadingPercentText.text = percent.ToString() + "%";

            loaderReached100 = x >= -5f;
        }

        loadingBarPanel.SetActive(false);
        LoadCompleteCallback?.Invoke();
    }
}
