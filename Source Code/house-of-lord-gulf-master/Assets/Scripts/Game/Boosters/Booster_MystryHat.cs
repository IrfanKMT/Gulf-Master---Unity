using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetKits.ParticleImage;

public class Booster_MystryHat : Booster
{
    #region Variables

    [SerializeField] private GameObject aniamtedGO;
    [SerializeField] private float timeBetweenCandySpawns = 0.5f;

    [SerializeField] private ParticleImage candyMovementVFX;
    [SerializeField] private Transform candySpawnPoint;

    [SerializeField] private Material specialCandyMaterial;

    private bool boosted = false;
    private readonly List<GamePiece> boostedPieces = new();

    #endregion

    #region Overriden Functions

    public override void Init()
    {
        BoosterManager.manager.isBoosterWorking = true;
        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
    }

    public override void Boost()
    {
        if (!boosted)
        {
            boosted = true;
            StartCoroutine(BoostCoroutine());
        }
    }

    #endregion

    #region Coroutines

    IEnumerator BoostCoroutine()
    {
        //Generate 3 random special candies at 3 random places with 3 random colors
        List<Vector2> usedPieces = new();

        for (int i = 0; i < 3; i++)
        {
            int randX;
            int randY;
            GamePiece randPiece;

            int checkIteration = 0;
            do
            {
                randX = GenerateRandom(0, Grid.width);
                randY = GenerateRandom(0, Grid.height);

                randPiece = Grid.grid.pieces[randX, randY] == null ? null : Grid.grid.pieces[randX, randY];
                checkIteration++;
            } while ((randPiece == null || randPiece.Type != PieceType.NORMAL || usedPieces.Contains(new Vector2(randX, randY))) && checkIteration < 100);

            usedPieces.Add(new Vector2(randX, randY));
            StartCoroutine(Coroutine_AddSpecialPiece(randX, randY, i == 2));
            yield return new WaitForSeconds(timeBetweenCandySpawns);
        }
    }

    IEnumerator Coroutine_AddSpecialPiece(int randX, int randY, bool isLastSpecialPiece)
    {
        //Generating Random Piece Type between 2 to 6 enum
        ColorType color = Grid.grid.pieces[randX, randY].ColorComponent.Color;
        GameObject oldPiece = Grid.grid.pieces[randX, randY].gameObject;
        GamePiece newPiece = Grid.grid.SpawnNewPiece(randX, randY, (PieceType)GenerateRandom(2, 6));
        newPiece.ColorComponent.SetColor(color);

        ParticleImage particle = Instantiate(candyMovementVFX.gameObject, candySpawnPoint.transform.position, Quaternion.identity).GetComponent<ParticleImage>();
        GameObject targetGO = new("Target");

        var target = targetGO.AddComponent<RectTransform>();
        targetGO.transform.position = Grid.grid.GetWorldPosition(randX, randY);
        targetGO.transform.SetParent(GameplayUIManager.manager.gameplayUI);

        particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.transform.localScale = new(2, 2, 2);
        particle.texture = newPiece.ColorComponent.candySprite.sprite.texture;
        particle.attractorTarget = target;

        // Hide The Newly Spawned Candy
        newPiece.ColorComponent.candySprite.color = new(1, 1, 1, 0);

        yield return new WaitWhile(()=>!particle.isStopped);

        Destroy(particle.gameObject);
        Destroy(target.gameObject);
        Destroy(oldPiece);

        newPiece.ColorComponent.candySprite.color = new(1, 1, 1, 1);
        newPiece.ColorComponent.SetSpriteMaterial(specialCandyMaterial);

        boostedPieces.Add(newPiece);

        if (isLastSpecialPiece)
        {
            Grid.grid.Grid_OnCandyDestroyed += OnCandyDestroyed;
            GamePlayManager.manager.OnTurnChanged += OnTurnChanged;

            BoosterManager.manager.isBoosterWorking = false;

            aniamtedGO.SetActive(false);
            Grid.grid.StartFillingGrid();
        }
    }

    #endregion

    #region Event Functions

    private void OnCandyDestroyed(GamePiece obj, TeamType t = TeamType.None)
    {
        if (boostedPieces.Contains(obj))
            boostedPieces.Remove(obj);
    }

    private void OnTurnChanged(bool isMyTurn)
    {
        foreach (var item in boostedPieces)
        {
            Destroy(Grid.grid.pieces[item.X, item.Y].gameObject);
            GamePiece newPiece = Grid.grid.SpawnNewPiece(item.X, item.Y, PieceType.NORMAL);
            newPiece.ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(item.ColorComponent.Color);
        }

        GamePlayManager.manager.OnTurnChanged -= OnTurnChanged;

        Destroy(gameObject);
    }

    #endregion
}
