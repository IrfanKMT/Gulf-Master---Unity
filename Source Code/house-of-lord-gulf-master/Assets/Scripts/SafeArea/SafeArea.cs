using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 anchorMin;
    private Vector2 anchorMax;

    public IEnumerator Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        RefreshPanel(Screen.safeArea);
        yield return new WaitForSecondsRealtime(1f);
        RefreshPanel(Screen.safeArea);
        yield return new WaitForSecondsRealtime(1f);
        RefreshPanel(Screen.safeArea);
        Debug.Log("-----------------This Code is Working---------------------");
    }
#if UNITY_EDITOR
    private void Update()
    {
        RefreshPanel(Screen.safeArea);
    }
#endif

    /// <summary>
    /// Sete Panel Anchor Min and Max
    /// </summary>
    /// <param name="safeArea"></param>
    public void RefreshPanel(Rect safeArea)
    {

        anchorMin = safeArea.position;
        anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        //_rectTransform.anchorMin = anchorMin;
        //print("AnchorMin : " +_rectTransform.anchorMin);
        _rectTransform.anchorMax = anchorMax;
        //print("AnchorMax : " + _rectTransform.anchorMax);
    }
}
