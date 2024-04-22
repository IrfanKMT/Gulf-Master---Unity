using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClearLinePiece : ClearablePiece
{
    [Header("Sounds")]
    [SerializeField] float soundVolume;
    [SerializeField] AudioClip rocketStartSFX;
    [SerializeField] AudioClip rocketEndSFX;

    [Header("Values")]
    [SerializeField] float clearingStartTime;
    [SerializeField] float animationEndTime;

    [Header("Animation Prefabs")]
    [SerializeField] GameObject redAnimationPrefab;
    [SerializeField] GameObject blueAnimationPrefab;
    [SerializeField] GameObject yellowAnimationPrefab;
    [SerializeField] GameObject purpleAnimationPrefab;
    [SerializeField] GameObject greenAnimationPrefab;

    Dictionary<ColorType, GameObject> colorAnimationPrefabs = new();

    public bool isRow;

    private void Start()
    {
        colorAnimationPrefabs.Add(ColorType.Red, redAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Blue, blueAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Green, greenAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Yellow, yellowAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Purple, purpleAnimationPrefab);
    }

    protected override void Clear(PieceType clearingPieceType)
    {
        StartCoroutine(StartClearing(clearingPieceType));
        Destroy(gameObject, animationEndTime);
    }

    IEnumerator StartClearing(PieceType clearingPieceType)
    {
        GamePlayManager.manager.Server_AddScore();

        GameObject rocketHolder = new("Rocket Holder");
        rocketHolder.transform.position = piece.transform.position;

        Instantiate(colorAnimationPrefabs[piece.ColorComponent.Color], rocketHolder.transform);
        piece.ColorComponent.SetSpriteColor(new(0, 0, 0, 0));

        SoundManager.manager.PlaySoundSeperately(rocketStartSFX, soundVolume);

        if (!isRow)
            rocketHolder.transform.localEulerAngles = new(0, 0, 90);

        rocketHolder.transform.SetParent(transform);
        
        yield return new WaitForSeconds(clearingStartTime);
        SoundManager.manager.PlaySoundSeperately(rocketEndSFX, soundVolume);

        if (isRow && clearingPieceType != PieceType.ROW_CLEAR)
            piece.GridRef.ClearRow(piece.X, piece.Y);
        else if (!isRow && clearingPieceType != PieceType.COLUMN_CLEAR)
            piece.GridRef.ClearColumn(piece.X, piece.Y);
    }
}

