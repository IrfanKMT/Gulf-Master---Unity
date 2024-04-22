using DG.Tweening;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Utitlits 
{
    public static void _PopupOpenEffect(Transform m_t)
    {
        m_t.localScale = Vector3.zero;
        m_t.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public static async Task _ButtonEffectWaiter(Transform m_t, int m_mili_second, float m_time)
    {
        m_t.DOPunchPosition(new Vector3(5f, 5f, 5f), m_time, 10, 1);
        await Task.Delay(m_mili_second);
    }

    public static void _TextAnimation(Transform m_t)
    {
        m_t.DOPunchPosition(new Vector3(-5f, 5f, 5f), 0.1f, 10, 1);
    }

    public  static void _FadeOutImage(Image m_image)
    {
        m_image.DOFade(0.01f, 0.2f);
    }

    public static void _FadeInImage(Image m_image)
    {
        m_image.DOFade(1f, 0.2f);
    }

    public static void _DoTextColorBlack(TextMeshProUGUI  m_txt)
    {
        m_txt.color = Color.black;
    }

    public  static void _DoTextColorWhite(TextMeshProUGUI m_txt)
    {
        m_txt.color = Color.white;
    }

    public static async Task _Waiter(int m_mili_second_to_wait)
    {
        await Task.Delay(m_mili_second_to_wait);
    }

    public static void _DoArrowAnimation(Transform m_t)
    {
        m_t.DOScale(1,0.5f).SetEase(Ease.OutBack);
    }
}
