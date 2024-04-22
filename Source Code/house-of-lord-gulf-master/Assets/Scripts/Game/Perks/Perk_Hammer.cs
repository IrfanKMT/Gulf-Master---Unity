public class Perk_Hammer : Perk
{
    public override void Boost()
    {
        Grid.grid.Grid_OnCandyClicked += OnCandyClicked;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= OnCandyClicked;
    }

    private void OnCandyClicked(GamePiece obj)
    {
        PerksManager.manager.isPerkWorking = true;
        SoundManager.manager.PlaySoundSeperately(perkSFX[GenerateRandom(0, perkSFX.Length)]);

        Grid.grid.ForceClearPiece(obj.X, obj.Y, true);
        Grid.grid.StartFillingGrid(true);

        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
