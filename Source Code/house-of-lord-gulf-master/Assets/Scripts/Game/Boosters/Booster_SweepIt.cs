using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Booster_SweepIt : Booster
{
    public override void Boost()
    {
        StartCoroutine(Sweep());
    }

    IEnumerator Sweep()
    {
        BoosterManager.manager.isBoosterWorking = true;

        ColorType randColor = (ColorType)GenerateRandom(0, 5);

        if ((GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1) || (GameNetworkManager.manager.mode == Mirror.NetworkManagerMode.ServerOnly))
        {
            if (randColor == ColorType.Blue)
                randColor = ColorType.Red;
            else if (randColor == ColorType.Red)
                randColor = ColorType.Blue;
        }

        List<GamePiece> allCandies = new();

        for (int i = 0; i < Grid.width; i++)
            for (int j = 0; j < Grid.height; j++)
                if (Grid.grid.pieces[i, j].ColorComponent.Color == randColor)
                    allCandies.Add(Grid.grid.pieces[i, j]);

        while (!AllCandyReachedEnd(allCandies))
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var item in allCandies)
                MoveCandy(item);
        }

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.StartFillingGrid();

        yield return new WaitForSeconds(1);
        yield return new WaitWhile(() => Grid.grid.isFilling);
        yield return new WaitForSeconds(1);

        Grid.grid.beingDestroyedByBooster = true;

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    void MoveCandy(GamePiece piece)
    {
        if (piece.X == Grid.width - 1)
            return;

        if (piece.ColorComponent.Color == Grid.grid.pieces[piece.X + 1, piece.Y].ColorComponent.Color)
            return;
        else
        {
            Vector2Int piece1Pos = new(piece.X,piece.Y);
            GamePiece piece1 = piece;
            GamePiece piece2 = Grid.grid.pieces[piece.X + 1, piece.Y];

            Grid.grid.pieces[piece1.X, piece1.Y] = piece2;
            Grid.grid.pieces[piece2.X, piece2.Y] = piece1;

            piece1.MovableComponent.MovePiece(piece2.X, piece2.Y);
            piece2.MovableComponent.MovePiece(piece1Pos.x, piece1Pos.y);
        }

    }

    bool AllCandyReachedEnd(List<GamePiece> candies)
    {
        bool result = true;
        foreach (var item in candies)
            if (!CandyReachedEnd(item))
                result = false;

        return result;
    }

    bool CandyReachedEnd(GamePiece piece)
    {
        if(piece.X == Grid.width - 1)
            return true;
        else
        {
            bool result = true;

            for (int i = piece.X; i < Grid.width; i++)
                if (Grid.grid.pieces[i, piece.Y].ColorComponent.Color != piece.ColorComponent.Color)
                    result = false;

            return result;
        }
    }
}
