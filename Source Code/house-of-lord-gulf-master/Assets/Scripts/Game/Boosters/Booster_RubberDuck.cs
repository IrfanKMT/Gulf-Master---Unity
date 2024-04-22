using UnityEngine;
using System.Collections;

public class Booster_RubberDuck : Booster
{
    [SerializeField] GameObject duckGO;

    [SerializeField] float timeToStartMoving = 0.5f;
    [SerializeField] float duckSpeed = 1;

    private bool startMoving = false;
    private int randRow;
    private Transform duckTransform;
    private bool destroyedAllPieces = false;

    public override void Init()
    {
        BoosterManager.manager.isBoosterWorking = true;
        randRow = GenerateRandom(0, Grid.height);

        duckTransform = Instantiate(duckGO, Grid.grid.GetWorldPosition(Grid.width-1, randRow), Quaternion.identity).transform;
        Invoke(nameof(StartMoving), timeToStartMoving);
    }

    private void StartMoving()
    {
        startMoving = true;
    }

    private void Update()
    {
        if (!startMoving) return;
        Vector2 target = Grid.grid.GetWorldPosition(0, randRow) + new Vector2(-2, 0);

        duckTransform.position = Vector2.MoveTowards(duckTransform.position, target, duckSpeed * Time.deltaTime);

        if (Vector2.Distance(duckTransform.position, target) < 4f && !destroyedAllPieces)
            StartCoroutine(DestroyPieces());
        else if (Vector2.Distance(duckTransform.position, target) < 0.25f)
        {
            Destroy(duckTransform.gameObject);
            Destroy(gameObject);
        }
    }

    IEnumerator DestroyPieces()
    {
        for (int i = Grid.width - 1; i >= 0; i--)
        {
            yield return new WaitForSeconds(0.1f);
            Grid.grid.ForceClearPiece(i, randRow);
        }

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();
        BoosterManager.manager.isBoosterWorking = false;

        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;
        destroyedAllPieces = true;
    }
}
