public class Perk_Detonator : Perk
{
    public override void Boost()
    {
        PerksManager.manager.isPerkWorking = true;

        for (int x = 0; x < Grid.width; x++)
        {
            for (int y = 0; y < Grid.height; y++)
            {
                GamePiece piece = Grid.grid.pieces[x, y];
                if(piece!=null)
                    if(piece.Type == PieceType.ROW_CLEAR || piece.Type == PieceType.COLUMN_CLEAR || piece.Type == PieceType.ELECTRIC || piece.Type == PieceType.BOMB)
                        Grid.grid.ClearPiece(x,y);
            }
        }

        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
