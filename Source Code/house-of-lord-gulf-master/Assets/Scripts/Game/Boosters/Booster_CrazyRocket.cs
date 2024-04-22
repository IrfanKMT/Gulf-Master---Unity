using UnityEngine;
using System.Collections;

public class Booster_CrazyRocket : Booster
{
    [Tooltip("1st Element Is StartPos, 2nd Element is Control Point For Start Pos Curve, 3rd Element is Control Point For End Pos Curve, 4th Point is End Pos")]
    [SerializeField] private Transform[] bezierPaths;

    [SerializeField] private float movingAnimationStartTime = 0.15f;
    [SerializeField] private float movementSpeed = 5;

    [SerializeField] private GameObject blastVFX;

    [Header("Gizmo")]
    [SerializeField] private float circleSize;

    private Vector2 path1;
    private Vector2 path2;
    private Vector2 path3;
    private Vector2 path4;

    private int randX;
    private int randY;
    private float tParam = 0;

    public override void Init()
    {
        bezierPaths[0].SetParent(null);
        bezierPaths[3].SetParent(null);

        BoosterManager.manager.isBoosterWorking = true;

        randX = GenerateRandom(0, Grid.width);
        randY = GenerateRandom(0, Grid.height);

        path1 = bezierPaths[0].position;
        path4 = Grid.grid.GetWorldPosition(randX,randY);
        bezierPaths[3].position = path4;

        if (randX < 4)
        {
            path2 = new Vector2(- Mathf.Abs(bezierPaths[1].position.x), bezierPaths[1].position.y);
            path3 = new Vector2(- Mathf.Abs(bezierPaths[2].position.x), bezierPaths[2].position.y);
        }
        else
        {
            path2 = new Vector2(Mathf.Abs(bezierPaths[1].position.x), bezierPaths[1].position.y);
            path3 = new Vector2(Mathf.Abs(bezierPaths[2].position.x), bezierPaths[2].position.y);
        }

        Invoke(nameof(StartAnimating), movingAnimationStartTime);
    }

    private void StartAnimating()
    {
        StartCoroutine(FollowBezierCurve());
    }

    IEnumerator FollowBezierCurve()
    {
        while (tParam < 1)
        {
            tParam += Time.deltaTime * movementSpeed;
            transform.position = Mathf.Pow(1 - tParam, 3) * path1 + 3 * Mathf.Pow(1 - tParam, 2) * tParam * path2 + 3 * (1 - tParam) * Mathf.Pow(tParam, 2) * path3 + Mathf.Pow(tParam, 3) * path4;
            yield return new WaitForEndOfFrame();
        }
        FireBooster();
    }

    private void FireBooster()
    {
        Instantiate(blastVFX, Grid.grid.GetWorldPosition(randX, randY), blastVFX.transform.rotation);

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.ClearBombAdjacent(randX, randY, true);
        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;

        foreach (var path in bezierPaths)
            Destroy(path.gameObject);

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        path1 = bezierPaths[0].position;
        path2 = bezierPaths[1].position;
        path3 = bezierPaths[2].position;
        path4 = bezierPaths[3].position;

        for (float t = 0; t <= 1f; t += 0.05f)
            Gizmos.DrawSphere(Mathf.Pow(1 - t, 3) * path1 + 3 * Mathf.Pow(1 - t, 2) * t * path2 + 3 * (1 - t) * Mathf.Pow(t, 2) * path3 + Mathf.Pow(t, 3) * path4, circleSize);

        Gizmos.DrawLine(new Vector2(bezierPaths[0].position.x, bezierPaths[0].position.y), new Vector2(bezierPaths[1].position.x, bezierPaths[1].position.y));
        Gizmos.DrawLine(new Vector2(bezierPaths[2].position.x, bezierPaths[2].position.y), new Vector2(bezierPaths[3].position.x, bezierPaths[3].position.y));
    }
}
