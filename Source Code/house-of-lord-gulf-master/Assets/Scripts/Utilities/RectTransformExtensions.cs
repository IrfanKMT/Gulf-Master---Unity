using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    public static void SetAll(this RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static Vector2 GetSnapToPositionToBringChildIntoView(this ScrollRect instance, RectTransform child)
    {
        Canvas.ForceUpdateCanvases();
        Vector2 viewportLocalPosition = instance.viewport.localPosition;
        Vector2 childLocalPosition = child.localPosition;
        Vector2 result = new Vector2(0 - (viewportLocalPosition.x + childLocalPosition.x), 0 - (viewportLocalPosition.y + childLocalPosition.y));
        return result;
    }
}
