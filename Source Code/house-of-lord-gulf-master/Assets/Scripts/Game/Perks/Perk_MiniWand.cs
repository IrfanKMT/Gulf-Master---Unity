using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssetKits.ParticleImage;
using UnityEngine;

public class Perk_MiniWand : Perk
{
    [SerializeField] ParticleImage candyAttractorVFX;
    int numberOfAttractorActive = 0;

    public override void Boost()
    {
        StartCoroutine(Coroutine_Boost());
    }

    IEnumerator Coroutine_Boost()
    {
        PerksManager.manager.isPerkWorking = true;

        ColorType color = (ColorType)GenerateRandom(0, 5);
        if ((GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1) || GameNetworkManager.manager.mode == Mirror.NetworkManagerMode.ServerOnly)
        {
            if (color == ColorType.Red) color = ColorType.Blue;
            else if (color == ColorType.Blue) color = ColorType.Red;
        }

        List<Vector2Int> positions = new();
        for (int x = 0; x < Grid.width; x++)
        {
            for (int y = 0; y < Grid.height; y++)
            {
                GamePiece piece = Grid.grid.pieces[x, y];
                if (piece != null)
                    if (piece.ColorComponent != null)
                        if (piece.ColorComponent.Color == color)
                            positions.Add(new(x, y));
            }
        }

        foreach (var item in positions)
        {
            ParticleImage particle = Instantiate(candyAttractorVFX.gameObject, Grid.grid.GetWorldPosition(0, Grid.height - 1), Quaternion.identity).GetComponent<ParticleImage>();
            GameObject targetGO = new("Target");
            var target = targetGO.AddComponent<RectTransform>();
            targetGO.transform.position = Grid.grid.GetWorldPosition(item.x, item.y);
            targetGO.transform.SetParent(GameplayUIManager.manager.gameplayUI);

            particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
            particle.transform.localScale = Vector3.one;
            particle.attractorTarget = target;

            yield return new WaitForSeconds(0.1f);

            StartCoroutine(WaitBeforeDesrtoyingAttractorParticle(particle, particle.gameObject, target.gameObject, item.x, item.y));
        }

        yield return new WaitWhile(() => numberOfAttractorActive > 0);
        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }

    IEnumerator WaitBeforeDesrtoyingAttractorParticle(ParticleImage attractor, GameObject particle, GameObject target, int i, int j)
    {
        numberOfAttractorActive++;

        yield return new WaitWhile(() => !attractor.isStopped);

        Destroy(particle);
        Destroy(target);

        Grid.grid.ForceClearPiece(i, j);

        numberOfAttractorActive--;
    }
}
