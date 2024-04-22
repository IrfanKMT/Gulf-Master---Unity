using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClearRandomPiece : ClearablePiece
{
    [Header("Sound")]
    [SerializeField] float electricHitSoundVolume;
    [SerializeField] AudioClip electricHitSound;

    [Header("Values")]
    [SerializeField] float clearingStartTime;
    [SerializeField] float animationEndTime;
    [SerializeField] float timeBetweenClearingEachGem;

    [Header("Animations")]
    [SerializeField] GameObject redAnimationPrefab;
    [SerializeField] GameObject blueAnimationPrefab;
    [SerializeField] GameObject yellowAnimationPrefab;
    [SerializeField] GameObject purpleAnimationPrefab;
    [SerializeField] GameObject greenAnimationPrefab;

    [Header("Electric Effect")]
    [SerializeField] Color redLightningGlowColor;
    [SerializeField] Color blueLightningGlowColor;
    [SerializeField] Color greenLightningGlowColor;
    [SerializeField] Color purpleLightningGlowColor;
    [SerializeField] Color yellowLightningGlowColor;
    
    [SerializeField] LightningBolt2D electricVFXPrefab;
    [SerializeField] float electricVFXDestroyTime;

    [Header("Destroy Effect")]
    [SerializeField] GameObject greenCandyEffectedDestroyEffects;
    [SerializeField] GameObject redCandyEffectedDestroyEffects;
    [SerializeField] GameObject blueCandyEffectedDestroyEffects;
    [SerializeField] GameObject purpleCandyEffectedDestroyEffects;
    [SerializeField] GameObject yellowCandyEffectedDestroyEffects;

    Dictionary<ColorType, GameObject> colorAnimationPrefabs = new();
    Dictionary<ColorType, GameObject> colorDestroyEffectPrefabs = new();
    Dictionary<ColorType, Color> electricGlowColors = new();

    private void Start()
    {
        colorDestroyEffectPrefabs.Add(ColorType.Red, redCandyEffectedDestroyEffects);
        colorDestroyEffectPrefabs.Add(ColorType.Blue, blueCandyEffectedDestroyEffects);
        colorDestroyEffectPrefabs.Add(ColorType.Green, greenCandyEffectedDestroyEffects);
        colorDestroyEffectPrefabs.Add(ColorType.Yellow, yellowCandyEffectedDestroyEffects);
        colorDestroyEffectPrefabs.Add(ColorType.Purple, purpleCandyEffectedDestroyEffects);

        colorAnimationPrefabs.Add(ColorType.Red, redAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Blue, blueAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Green, greenAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Yellow, yellowAnimationPrefab);
        colorAnimationPrefabs.Add(ColorType.Purple, purpleAnimationPrefab);

        electricGlowColors.Add(ColorType.Red, redLightningGlowColor);
        electricGlowColors.Add(ColorType.Blue, blueLightningGlowColor);
        electricGlowColors.Add(ColorType.Green, greenLightningGlowColor);
        electricGlowColors.Add(ColorType.Yellow, yellowLightningGlowColor);
        electricGlowColors.Add(ColorType.Purple, purpleLightningGlowColor);
    }

    protected override void Clear(PieceType clearingPieceType)
    {
        GamePlayManager.manager.Server_AddScore();

        Instantiate(colorAnimationPrefabs[piece.ColorComponent.Color], transform);
        piece.ColorComponent.SetSpriteColor(new(0, 0, 0, 0));

        StartCoroutine(StartClearing());
    }

    IEnumerator StartClearing()
    {
        yield return new WaitForSeconds(clearingStartTime);

        List<Vector2> destroyedPieces = new();

        for (int i = 0; i < 10; i++)
        {
            int randX;
            int randY;

            int iterationCheck = 0;
            
            do
            {
                iterationCheck++;

                randX = Grid.grid.GenerateRandom(0, Grid.width);
                randY = Grid.grid.GenerateRandom(0, Grid.height);
                if (iterationCheck >= 100)
                {
                    Debug.Log("Check Electric, Iteration Check Triggered");
                    break;
                }

            } while ((Grid.grid.pieces[randX, randY] == null || destroyedPieces.Contains(new Vector2(randX, randY)) || (Grid.grid.pieces[randX, randY] != null && Grid.grid.pieces[randX, randY].Type == PieceType.EMPTY)|| (Grid.grid.pieces[randX, randY] != null && Grid.grid.pieces[randX, randY].IsClearable() && Grid.grid.pieces[randX, randY].ClearableComponent.IsBeingCleared)) && iterationCheck < 100);

            if (iterationCheck < 100)
            {
                LightningBolt2D electricGO = Instantiate(electricVFXPrefab.gameObject).GetComponent<LightningBolt2D>();
                electricGO.SetPositions(piece.transform.position, Grid.grid.pieces[randX, randY].transform.position);

                Destroy(electricGO.gameObject, electricVFXDestroyTime);

                SoundManager.manager.PlaySoundSeperately(electricHitSound, electricHitSoundVolume);
                yield return new WaitForSeconds(timeBetweenClearingEachGem);

                GameObject candyDestroyEffect = Grid.grid.pieces[randX, randY].IsColored() ? colorDestroyEffectPrefabs[Grid.grid.pieces[randX, randY].ColorComponent.Color] : null;

                if(candyDestroyEffect!=null)
                    Instantiate(candyDestroyEffect, Grid.grid.pieces[randX, randY].transform.position, Quaternion.identity);

                Grid.grid.ClearPiece(randX, randY, false, PieceType.ELECTRIC);
                destroyedPieces.Add(new(randX, randY));
            }
        }
        yield return new WaitForSeconds(electricVFXDestroyTime);

        Destroy(gameObject);
    }
}
