using UnityEngine;

public class Booster_LaserBeam : Booster
{
    [SerializeField] private GameObject blastVFX;

    #region Client Callbacks

    public override void Boost()
    {
        Grid.grid.Grid_OnCandyClicked += LaserBeamBoost;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= LaserBeamBoost;
    }

    #endregion

    #region Boost Function

    private void LaserBeamBoost(GamePiece piece)
    {
        int x = piece.X;
        int y = piece.Y;
        BoosterManager.manager.isBoosterWorking = true;

        //GFX and SFX
        Instantiate(blastVFX, Grid.grid.GetWorldPosition(x, y), Quaternion.identity);
        SoundManager.manager.PlaySoundSeperately(boosterSFXs[GenerateRandom(0, boosterSFXs.Length)]);

        for (int i = 0; i < Grid.width; i++)
        {
            if (i == x)
                continue;

            Grid.grid.ForceClearPiece(i, y);
        }

        for (int i = 0; i < Grid.height; i++)
        {
            if (i == y)
                continue;

            Grid.grid.ForceClearPiece(x, i);
        }

        Grid.grid.ForceClearPiece(x, y);
        Grid.grid.Grid_OnCandyClicked -= LaserBeamBoost;

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    #endregion
}
