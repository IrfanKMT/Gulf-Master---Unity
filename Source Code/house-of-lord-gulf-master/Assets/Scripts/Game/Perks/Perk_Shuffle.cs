public class Perk_Shuffle : Perk
{
    public override void Boost()
    {
        PerksManager.manager.isPerkWorking = true;
        Grid.grid.ShuffleBoard();
        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
