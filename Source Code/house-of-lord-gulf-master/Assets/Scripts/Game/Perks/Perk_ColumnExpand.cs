using UnityEngine;
using AssetKits.ParticleImage;
using System.Collections;

public class Perk_ColumnExpand : Perk
{
    [SerializeField] ParticleImage columnParticleImage;

    public override void Boost()
    {
        Grid.grid.Grid_OnCandyClicked += UseColumnExpandBooster;
    }
    
    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= UseColumnExpandBooster;
    }

    private void UseColumnExpandBooster(GamePiece candy)
    {
        StartCoroutine(UseColumnExpand(candy));
    }

    IEnumerator UseColumnExpand(GamePiece candyClicked)
    {
        PerksManager.manager.isPerkWorking = true;

        var particle = Instantiate(columnParticleImage, Grid.grid.GetWorldPosition(0, Grid.height - 1), Quaternion.identity).GetComponent<ParticleImage>();
        particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.transform.localScale = new Vector3(2, 2, 2);

        GameObject targetGO = new("Target");
        var target = targetGO.AddComponent<RectTransform>();
        targetGO.transform.position = Grid.grid.GetWorldPosition(candyClicked.X, candyClicked.Y);
        targetGO.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.attractorTarget = target;

        yield return new WaitWhile(() => !particle.isStopped);

        Destroy(particle.gameObject);
        Destroy(target.gameObject);

        // Destroys The Column
        for (int i = 0; i < Grid.height; i++)
            Grid.grid.ForceClearPiece(candyClicked.X, i, true);

        Grid.grid.StartFillingGrid(true);

        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }

}
