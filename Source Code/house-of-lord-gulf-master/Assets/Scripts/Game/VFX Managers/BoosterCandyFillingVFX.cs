using UnityEngine;
using System.Collections;
using AssetKits.ParticleImage;

public class BoosterCandyFillingVFX : MonoBehaviour
{
    [SerializeField] ParticleImage boosterCandyFillingVFX;

    [Header("Targets")]
    [SerializeField] Transform blueBoosterCandyFillingTarget;
    [SerializeField] Transform redBoosterCandyFillingTarget;

    [Header("Candy Textures")]
    [SerializeField] Texture blueCandyTex;
    [SerializeField] Texture redCandyTex;

    private void Start()
    {
        Grid.grid.Grid_OnCandyDestroyed += (candy, TeamType) => StartCoroutine(OnCandyDestroyed(candy, GamePlayManager.manager.currentTurnPlayerteamtype));
    }

    IEnumerator OnCandyDestroyed(GamePiece obj ,TeamType t=TeamType.None)
    {
        if (Grid.grid.beingDestroyedByBooster) yield break;
        if (GamePlayManager.manager.LocalPlayer == null || BoosterManager.manager.Client_IsBoosterCandyFull(GamePlayManager.manager.LocalPlayer.isPlayer1)) yield break;
        if (obj.ColorComponent.Color != ColorType.Blue && GamePlayManager.manager.Client_IsMyTurn()) yield break;
        if (obj.ColorComponent.Color != ColorType.Red && !GamePlayManager.manager.Client_IsMyTurn()) yield break;

        ParticleImage particle = Instantiate(boosterCandyFillingVFX.gameObject, obj.transform.position, Quaternion.identity).GetComponent<ParticleImage>();
        particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.transform.localScale = new(2, 2, 2);
        particle.texture = obj.ColorComponent.Color == ColorType.Blue ? blueCandyTex : redCandyTex;
        particle.attractorTarget = obj.ColorComponent.Color == ColorType.Blue ? blueBoosterCandyFillingTarget : redBoosterCandyFillingTarget;
        Destroy(particle.gameObject, 5);
        yield return new WaitWhile(() => !particle.isStopped);
        Destroy(particle.gameObject);
    }
}
