using UnityEngine;
using System.Collections.Generic;

public class BombClearPiece : ClearablePiece
{
    [Header("Sounds")]
    [SerializeField] float bombSoundVolume;
    [SerializeField] AudioClip bombSoundSFX;

    [Header("Effects")]
    [SerializeField] GameObject redBombDestroyEffect;
    [SerializeField] GameObject blueBombDestroyEffect;
    [SerializeField] GameObject yellowBombDestroyEffect;
    [SerializeField] GameObject purpleBombDestroyEffect;
    [SerializeField] GameObject greenBombDestroyEffect;

    private readonly Dictionary<ColorType, GameObject> colorEffects = new();

    private void Start()
    {
        colorEffects.Add(ColorType.Red, redBombDestroyEffect);
        colorEffects.Add(ColorType.Blue, blueBombDestroyEffect);
        colorEffects.Add(ColorType.Green, greenBombDestroyEffect);
        colorEffects.Add(ColorType.Yellow, yellowBombDestroyEffect);
        colorEffects.Add(ColorType.Purple, purpleBombDestroyEffect);
    }

    protected override void Clear(PieceType clearingPieceType)
    {
        base.Clear(clearingPieceType);

        GamePlayManager.manager.Server_AddScore();
        SoundManager.manager.PlaySoundSeperately(bombSoundSFX, bombSoundVolume);

        Instantiate(colorEffects[piece.ColorComponent.Color], piece.transform.position, Quaternion.identity);

        piece.GridRef.ClearBombAdjacent(piece.X, piece.Y);
        Destroy(gameObject);
    }
}
