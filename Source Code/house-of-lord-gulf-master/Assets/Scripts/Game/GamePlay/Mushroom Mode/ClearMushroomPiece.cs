public class ClearMushroomPiece : ClearablePiece
{
    protected override void Clear(PieceType clearingPieceType)
    {
        GamePlayManager.manager.Server_AddScoreMultiplier();
        base.Clear(clearingPieceType);
    }
}
