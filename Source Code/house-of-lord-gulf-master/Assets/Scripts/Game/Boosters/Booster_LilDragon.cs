using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Booster_LilDragon : Booster
{
    #region Client Callbacks

    public override void Boost()
    {
        Grid.grid.Grid_OnCandiesSwapped += Grid_OnCandiesSwapped;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandiesSwapped -= Grid_OnCandiesSwapped;
    }

    #endregion

    private void Grid_OnCandiesSwapped(GamePiece piece1, GamePiece piece2)
    {
        //if (ReconnectionHandler.handler.wait) return;
        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
        BoosterManager.manager.isBoosterWorking = true;

        Grid.grid.Grid_OnCandiesSwapped -= Grid_OnCandiesSwapped;

        var matches1 = Grid.grid.GetMatch(piece1, piece1.X, piece1.Y);
        var matches2 = Grid.grid.GetMatch(piece2, piece2.X, piece2.Y);

        if (matches1 != null)
            DestroyBlock(matches1);
        else if (matches2 != null)
            DestroyBlock(matches2);
    }

    private void DestroyBlock(List<GamePiece> matches)
    {
        if (AreMatchesVertical(matches))
        {
            foreach (var item in matches)
                for (int x = -3; x <= 3; x++)
                    if (item.X + x >= 0 && item.X + x < Grid.width)
                        Grid.grid.ForceClearPiece(item.X + x, item.Y);
        }
        else
        {
            foreach (var item in matches)
                for (int y = -3; y <= 3; y++)
                    if (item.Y + y >= 0 && item.Y + y < Grid.height)
                        Grid.grid.ForceClearPiece(item.X, item.Y + y);
        }

        Grid.grid.beingDestroyedByBooster = true;
        BoosterManager.manager.isBoosterWorking = false;

        StartCoroutine(DestroyAfterGridIsFilled());
    }

    IEnumerator DestroyAfterGridIsFilled()
    {
        yield return new WaitWhile(Grid.grid.IsAnyGemMovingOrClearing);
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(() => Grid.grid.isFilling);
        yield return null;
        yield return new WaitWhile(Grid.grid.IsAnyGemMovingOrClearing);
        yield return new WaitWhile(() => Grid.grid.isFilling);
        Destroy(gameObject);
    }

    bool AreMatchesVertical(List<GamePiece> pieces)
    {
        GamePiece firstPiece = pieces[0];
        bool result = false;

        for (int i = 0; i < pieces.Count; i++)
            if(pieces[i].Y!= firstPiece.Y)
                result = true;

        return result;
    }
}
