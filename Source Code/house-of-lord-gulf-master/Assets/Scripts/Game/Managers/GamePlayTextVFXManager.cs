using UnityEngine;
using System.Collections;
using DG.Tweening;

public class GamePlayTextVFXManager : MonoBehaviour
{
    public static GamePlayTextVFXManager manager;

    public Transform textVFXHolder;

    [Header("Make sure the particle system automatically destroys")]
    [SerializeField] TextEffect extraMoveTxtVFX;
    [SerializeField] TextEffect yourTurnTxtVFX;
    [SerializeField] TextEffect opponentTurnTxtVFX;

    [Header("Rounds")]
    [SerializeField] TextEffect[] roundsTxtVFX;

    [SerializeField] float timeBetweenEachTextEffect = 2;
    [SerializeField] float speedOfEffect = 2;

    bool showingTextEffect = false;

    private TextEffect CurrruntEffect;
    private bool gameEnd;

    #region Unity Functions

#if !UNITY_SERVER

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        GamePlayManager.manager.OnGameEnd += GameEnded;
        Grid.grid.OnExtraMoveGained += () => ShowTextEffect(extraMoveTxtVFX); ;
        //GamePlayManager.manager.OnTurnChanged += (isMyTurn) => ShowTextEffect(isMyTurn ? yourTurnTxtVFX : opponentTurnTxtVFX);
        GamePlayManager.manager.OnTurnChanged +=TurnBasedEffects;
        GamePlayManager.manager.OnRoundChanged += (roundsLeft) => ShowTextEffect(roundsTxtVFX[GamePlayManager.manager.maxRounds - roundsLeft]);
    }

#endif

#endregion

    #region Private Functions

    private void GameEnded()
    {
        gameEnd = true;
    }

    private void TurnBasedEffects(bool Turn)
    {
        Debug.Log("game End Turn Changed" + gameEnd);
        if (gameEnd) return;
        CurrruntEffect = Turn ? yourTurnTxtVFX : opponentTurnTxtVFX;
        ShowTextEffect(CurrruntEffect);
    }

    private void ShowTextEffect(TextEffect no)=> StartCoroutine(Coroutine_ShowTextEffect(no));
    IEnumerator Coroutine_ShowTextEffect(TextEffect effect)
    {
        Debug.Log("Round Changed "+GamePlayManager.manager.currntround);

        while (showingTextEffect)
            yield return null;

        showingTextEffect = true;

        if (effect.textEffectPrefab == null)
        {
            Debug.LogError("Error In Showing Gameplay Text VFX: Prefab Not Found/Prefab Is Null");
            yield break;
        }

        GameObject go = Instantiate(effect.textEffectPrefab, textVFXHolder);
        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(Vector3.one, speedOfEffect);


        if (effect.textEffectsfx == null) yield break;

        if (effect.volumeOverride != 0)
            SoundManager.manager.PlaySoundSeperately(effect.textEffectsfx, effect.volumeOverride);
        else
            SoundManager.manager.PlaySoundSeperately(effect.textEffectsfx);

        yield return new WaitForSeconds(timeBetweenEachTextEffect);

        go.transform.DOScale(Vector3.zero, speedOfEffect);

        yield return new WaitForSeconds(speedOfEffect);
        Destroy(go);
        showingTextEffect = false;
    }

    #endregion

}

[System.Serializable]
public struct TextEffect
{
    public GameObject textEffectPrefab;
    public AudioClip textEffectsfx;
    public float volumeOverride;
}

public enum TextEffectSelector
{
    ExtraMove,
    YourTurn,
    OpponentTurn
}
