using UnityEngine;
using System.Collections.Generic;

public class Perk_Lightening : Perk
{
    public override void Boost()
    {
        List<Vector2> usedPositions = new();
        PerksManager.manager.isPerkWorking = false;

        int randX;
        int randY;
        GamePiece randPiece;
        int count = 0;

        // Generate Rocket
        do
        {
            randX = GenerateRandom(0, Grid.width);
            randY = GenerateRandom(0, Grid.height);
            randPiece = Grid.grid.pieces[randX, randY];
            count++;
        } while (usedPositions.Contains(new Vector2(randX,randY)) && count < 100);

        if (randPiece != null)
        {
            Destroy(randPiece.gameObject);
            ColorType color = (ColorType)GenerateRandom(0, 5);

            if (GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1)
            {
                if (color == ColorType.Blue) color = ColorType.Red;
                else if (color == ColorType.Red) color = ColorType.Blue;
            }

            Grid.grid.SpawnNewPiece(randX, randY, GenerateRandom(0,2) == 0 ? PieceType.ROW_CLEAR : PieceType.COLUMN_CLEAR).ColorComponent.SetColor(color);
            usedPositions.Add(new Vector2(randX,randY));
        }

        // Generate Bomb
        do
        {
            randX = GenerateRandom(0, Grid.width);
            randY = GenerateRandom(0, Grid.height);
            randPiece = Grid.grid.pieces[randX, randY];
            count++;
        } while (usedPositions.Contains(new Vector2(randX, randY)) && count < 100);

        if (randPiece != null)
        {
            Destroy(randPiece.gameObject);
            ColorType color = (ColorType)GenerateRandom(0, 5);

            if (GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1)
            {
                if (color == ColorType.Blue) color = ColorType.Red;
                else if (color == ColorType.Red) color = ColorType.Blue;
            }

            Grid.grid.SpawnNewPiece(randX, randY, PieceType.BOMB).ColorComponent.SetColor(color);
            usedPositions.Add(new Vector2(randX,randY));
        }

        // Generate Electric
        do
        {
            randX = GenerateRandom(0, Grid.width);
            randY = GenerateRandom(0, Grid.height);
            randPiece = Grid.grid.pieces[randX, randY];
            count++;
        } while (usedPositions.Contains(new Vector2(randX, randY)) && count < 100);

        if (randPiece != null)
        {
            Destroy(randPiece.gameObject);
            ColorType color = (ColorType)GenerateRandom(0, 5);

            if (GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1)
            {
                if (color == ColorType.Blue) color = ColorType.Red;
                else if (color == ColorType.Red) color = ColorType.Blue;
            }

            Grid.grid.SpawnNewPiece(randX, randY, PieceType.ELECTRIC).ColorComponent.SetColor(color);
            usedPositions.Add(new Vector2(randX, randY));
        }

        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
