using AssetKits.ParticleImage;
using UnityEngine;
using System.Collections;

public class Perk_BombDelivery : Perk
{
    [SerializeField] ParticleImage bombParticleImage;

    public override void Boost()
    {
        StartCoroutine(Coroutine_Boost());
    }

    IEnumerator Coroutine_Boost()
    {
        PerksManager.manager.isPerkWorking = true;

        var particle = Instantiate(bombParticleImage, Grid.grid.GetWorldPosition(0, Grid.height - 1), Quaternion.identity).GetComponent<ParticleImage>();
        particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.transform.localScale = new Vector3(2, 2, 2);

        GameObject targetGO = new("Target");
        var target = targetGO.AddComponent<RectTransform>();
        targetGO.transform.position = Grid.grid.GetWorldPosition(3, 3);
        targetGO.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.attractorTarget = target;

        yield return new WaitWhile(() => !particle.isStopped);

        Destroy(particle.gameObject);
        Destroy(target.gameObject);

        Grid.grid.ClearBombAdjacent(3, 3, true);
        Grid.grid.StartFillingGrid(true);

        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
