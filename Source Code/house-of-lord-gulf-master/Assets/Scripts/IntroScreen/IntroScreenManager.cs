using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroScreenManager : MonoBehaviour
{
    [SerializeField]
    private string SceneName;
    [Space]
    public Image Image;
    public AudioSource Audiosource;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        Audiosource.Play();
        Image.DOFade(1f, 2f);
        yield return new WaitForSecondsRealtime(3f);
        Image.DOFade(0f, 0.7f);
        yield return new WaitForSecondsRealtime(0.7f);
        SceneManager.LoadScene(SceneName);
        Debug.Log("Loading Scene Now");
    }
}
