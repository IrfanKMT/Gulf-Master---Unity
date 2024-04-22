using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetKits.ParticleImage;
using System.Linq;

public class Booster_MagicWand : Booster
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject candyDestroyEffect;

    [SerializeField] private ParticleImage candyAttractorVFX;
    [SerializeField] private Transform candyAttractorSpawnPoint;

    private int numberOfAttractorActive = 0;

    public override void Init()
    {
        Grid.grid.Grid_OnCandyClicked += OnCandyClicked;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= OnCandyClicked;
    }


    private void OnCandyClicked(GamePiece piece)
    {
        //if (ReconnectionHandler.handler.wait) return;

        Grid.grid.Grid_OnCandyClicked -= OnCandyClicked;

        animator.SetTrigger("Sneeze");
        StartCoroutine(MagicWandBoost(piece));
    }

    IEnumerator MagicWandBoost(GamePiece piece)
    {
        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
        BoosterManager.manager.isBoosterWorking = true;

        Grid.grid.Grid_OnCandyClicked -= OnCandyClicked;

        if (piece != null)
        {
            List<Vector2Int> positions = new();
            ColorType color = piece.ColorComponent.Color;

            for (int i = 0; i < Grid.width; i++)
                for (int j = Grid.height - 1; j >= 0; j--)
                    if (Grid.grid.pieces[i, j] != null)
                        if (Grid.grid.pieces[i, j].IsColored())
                            if (Grid.grid.pieces[i, j].ColorComponent.Color == color)
                                positions.Add(new(i, j));

            foreach (var item in positions)
            {
                ParticleImage particle = Instantiate(candyAttractorVFX.gameObject, candyAttractorSpawnPoint.transform.position, Quaternion.identity).GetComponent<ParticleImage>();
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
        }

        Grid.grid.beingDestroyedByBooster = true;

        yield return new WaitWhile(() => numberOfAttractorActive > 0);
        yield return new WaitForSeconds(1);

        Grid.grid.StartFillingGrid();
        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    IEnumerator WaitBeforeDesrtoyingAttractorParticle(ParticleImage attractor, GameObject particle, GameObject target, int i, int j)
    {
        numberOfAttractorActive++;
        yield return new WaitWhile(()=>!attractor.isStopped);
        Instantiate(candyDestroyEffect, Grid.grid.GetWorldPosition(i,j), Quaternion.identity);
        Destroy(particle);
        Destroy(target);
        Grid.grid.ForceClearPiece(i, j);
        numberOfAttractorActive--;
    }
}
