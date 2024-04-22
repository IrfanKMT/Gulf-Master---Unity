using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Booster_PaintBucket : Booster
{
    [SerializeField] private GameObject redSplash;
    [SerializeField] private GameObject greenSplash;
    [SerializeField] private GameObject yellowSplash;
    [SerializeField] private GameObject purpleSplash;
    [SerializeField] private GameObject blueSplash;

    [SerializeField] private float lengthOfSplashAnimation = 0.25f;

    private readonly Dictionary<ColorType, GameObject> splashDict = new();

    public override void Boost()
    {
        StartCoroutine(DestroyPieces());
    }

    IEnumerator DestroyPieces()
    {
        splashDict.Add(ColorType.Red, redSplash);
        splashDict.Add(ColorType.Blue, blueSplash);
        splashDict.Add(ColorType.Green, greenSplash);
        splashDict.Add(ColorType.Purple, purpleSplash);
        splashDict.Add(ColorType.Yellow, yellowSplash);

        ColorType randColor = (ColorType)GenerateRandom(0, 5);

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
        BoosterManager.manager.isBoosterWorking = true;

        List<Vector2> paintedPieces = new();

        if ((GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1) || GameNetworkManager.manager.mode == Mirror.NetworkManagerMode.ServerOnly)
        {
            if (randColor == ColorType.Blue)
                randColor = ColorType.Red;
            else if (randColor == ColorType.Red)
                randColor = ColorType.Blue;
        }

        GameObject splashGO = splashDict[randColor];

        for (int i = 0; i < 6; i++) //Paint random colors to random 6 places
        {
            int x;
            int y;
            int countCheck = 0;
            do
            {
                countCheck++;
                x = GenerateRandom(0, Grid.width);
                y = GenerateRandom(0, Grid.height);
            } while ((Grid.grid.pieces[x, y].ColorComponent.Color == randColor || paintedPieces.Contains(new Vector2(x, y))) && countCheck < 100);

            GamePiece piece = Grid.grid.pieces[x, y];

            StartCoroutine(ChangeColorOfPiece(piece, randColor));

            Instantiate(splashGO, Grid.grid.GetWorldPosition(piece.X, piece.Y), Quaternion.identity).transform.SetParent(transform);

            SoundManager.manager.PlaySoundSeperately(boosterSFXs[GenerateRandom(0, boosterSFXs.Length)]);

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);

        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;
        BoosterManager.manager.isBoosterWorking = false;

        Destroy(gameObject);
    }

    IEnumerator ChangeColorOfPiece(GamePiece piece, ColorType randColor)
    {
        yield return new WaitForSeconds(lengthOfSplashAnimation);
        piece.ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(randColor);
    }
}
