using UnityEngine;
using System.Collections;

public class GameOverEndScreenTimerVFXManager : MonoBehaviour
{
    //[Header("Fireworks")]
    //[SerializeField] GameObject fireworksEffect;
    //[SerializeField] ParticleSystem fireworkEffectParticleSystem;

    //[Header("Stars")]
    //[SerializeField] GameObject starsEffect;
    //[SerializeField] ParticleSystem[] starsEffectParticleSystems;
    //[SerializeField] float starsEffectDuration;

    //[Header("Background Godrays")]
    //[SerializeField] GameObject starBackgroundEffect;
    //[SerializeField] ParticleSystem starBackgroundEffectParticleSystem;

    //private void Start()
    //{
    //    GamePlayManager.manager.OnGameStarted += OnGameStarted;
    //    GamePlayManager.manager.OnWonGame += ()=> StartCoroutine(OnWonGame());
    //    GamePlayManager.manager.OnLostGame += OnLostGame;
    //}

    //private void OnGameStarted()
    //{
    //    fireworksEffect.SetActive(false);
    //    starBackgroundEffect.SetActive(false);
    //    foreach (var item in starsEffectParticleSystems)
    //        item.gameObject.SetActive(false);
    //    starsEffect.SetActive(false);
    //}

    //IEnumerator OnWonGame()
    //{
    //    starsEffect.SetActive(true);
    //    starBackgroundEffect.SetActive(true);
    //    starBackgroundEffectParticleSystem.Play();

    //    fireworksEffect.SetActive(true);
    //    fireworkEffectParticleSystem.Play();

    //    foreach (var item in starsEffectParticleSystems)
    //    {
    //        item.gameObject.SetActive(true);
    //        item.Play();
    //        yield return new WaitForSeconds(starsEffectDuration);
    //    }
    //}

    //private void OnLostGame()
    //{
    //    fireworksEffect.SetActive(false);
    //    starBackgroundEffect.SetActive(false);
    //    starsEffect.SetActive(false);
    //}
}
