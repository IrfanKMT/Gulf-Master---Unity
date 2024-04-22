using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid_MushroomMode : Grid
{
    //[SerializeField] GamePlayManager gamePlayManager;
    [SerializeField] int mushroomPerTurn = 4;

    protected override void Awake()
    {
        base.Awake();
        gamePlayManager.OnTurnChanged += (x)=> OnTurnChanged();
    }

    private void OnTurnChanged()
    {
        if (!grid.gridFilled)
            return;
        SpawnMushrooms();
        Server_GridFilled("Mushroom");
    }

    public void SpawnMushrooms()
    {
        List<ClearMushroomPiece> spawnedMushrooms = GetComponentsInChildren<ClearMushroomPiece>().ToList();

        if (spawnedMushrooms.Count > 0)
        {
            foreach (var item in spawnedMushrooms)
            {
                ColorType color = item.piece.ColorComponent.Color;
                Destroy(pieces[item.piece.X, item.piece.Y].gameObject);
                GamePiece newPiece = SpawnNewPiece(item.piece.X, item.piece.Y, PieceType.NORMAL);
                newPiece.ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(color);
                //Adding Candy In List
                candyContainer.AddPices(newPiece.gameObject);
            }
        }

        candyContainer.RemoveDeletedPices();

        for (int i = 0; i < mushroomPerTurn; i++)
        {
            int randX;
            int randY;

            do
            {
                randX = GenerateRandom(0, width);
                randY = GenerateRandom(0, height);
            } while (pieces[randX, randY].Type == PieceType.MUSHROOM || pieces[randX, randY].Type != PieceType.NORMAL);

            ColorType color = pieces[randX, randY].ColorComponent.Color;
            Destroy(pieces[randX, randY].gameObject);
            GamePiece newPiece = SpawnNewPiece(randX, randY, PieceType.MUSHROOM);
            newPiece.ColorComponent.SetColor(color);
            candyContainer.AddPices(newPiece.gameObject);
            candyContainer.RemoveDeletedPices();
        }
    }
}