using UnityEngine;
using Mirror;

public class GamePlaySFXManager : MonoBehaviour
{
    [SerializeField] AudioClip[] backgroundMusic;
    [SerializeField] AudioClip[] gameWonSFX;
    [SerializeField] AudioClip[] gameLostSFX;

    [ClientCallback]
    private void Awake()
    {
        GamePlayManager.manager.OnGameStarted += OnGameStarted;
        GamePlayManager.manager.OnWonGame += OnGameWon;
        GamePlayManager.manager.OnLostGame += OnGameLost;

    }

    private void OnGameStarted()
    {
        SoundManager.manager.PlayBackgroundMusic(backgroundMusic[Random.Range(0, backgroundMusic.Length)]);
    }

    private void OnGameWon()
    {
        SoundManager.manager.StopBackgroundMusic();
        SoundManager.manager.PlaySoundSeperately(gameWonSFX[Random.Range(0,gameWonSFX.Length)]);
    }

    private void OnGameLost()
    {
        SoundManager.manager.StopBackgroundMusic();
        SoundManager.manager.PlaySoundSeperately(gameLostSFX[Random.Range(0, gameLostSFX.Length)]);
    }

    public void Play_MouseOnClickSound()
    {
        SoundManager.manager.Play_ButtonClickSound();
    }
}
