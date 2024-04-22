using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Booster_Pathway : Booster
{
    #region Helper Functions

    public override void Boost()
    {
        Grid.grid.Grid_OnCandiesPathMade += OnCandiesPathMade;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandiesPathMade -= OnCandiesPathMade;
    }


    #endregion

    #region Boosting

    private void OnCandiesPathMade(List<GamePiece> pieces)
    {
        List<Vector2Int> positions = new();

        foreach (var item in pieces)
            positions.Add(new(item.X, item.Y));

        BoosterManager.manager.isBoosterWorking = true;
        StartCoroutine(DestroyPath(positions.ToArray()));
    }

    IEnumerator DestroyPath(Vector2Int[] positions)
    {
        if (positions.Length == Grid.maxPathwayCandies)
        {
            foreach (var item in positions)
            {
                Grid.grid.ForceClearPiece(item.x, item.y);
                Grid.grid.StartFillingGrid();
                yield return new WaitForSeconds(0.1f);
            }

            BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
        }

        Grid.grid.Grid_OnCandiesPathMade -= OnCandiesPathMade;
        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    #endregion
}
