using UnityEngine;

public class Perk_Teleport : Perk
{
    GamePiece piece = null;

    public override void Boost()
    {
        Grid.grid.Grid_OnCandyClicked += OnCandyClicked;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= OnCandyClicked;
    }

    private void OnCandyClicked(GamePiece clickedPiece)
    {
        if(piece == null)
        {
            piece = clickedPiece;
            return;
        }
        else if (clickedPiece != piece)
        {
            UseTeleport(piece, clickedPiece);
        }
    }

    private void UseTeleport(GamePiece piece1, GamePiece piece2)
    {
        PerksManager.manager.isPerkWorking = true;

        Vector2Int tempPos1 = new(piece1.X, piece1.Y);
        ColorType tempColor1 = piece1.ColorComponent.Color;
        PieceType tempType1 = piece1.Type;

        Vector2Int tempPos2 = new(piece2.X, piece2.Y);
        ColorType tempColor2 = piece2.ColorComponent.Color;
        PieceType tempType2 = piece2.Type;

        Destroy(piece1.gameObject);
        Grid.grid.SpawnNewPiece(tempPos1.x, tempPos1.y, tempType2).ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(tempColor2);

        Destroy(piece2.gameObject);
        Grid.grid.SpawnNewPiece(tempPos2.x, tempPos2.y, tempType1).ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(tempColor1);

        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
