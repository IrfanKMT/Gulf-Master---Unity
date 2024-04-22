using UnityEngine;
using System.Collections;

public class Booster_FireCracker : Booster
{
    [Tooltip("1st Element Is StartPos, 2nd Element is Control Point For Start Pos Curve, 3rd Element is Control Point For End Pos Curve, 4th Point is End Pos")]
    [SerializeField] Transform[] bezierPaths;

    [SerializeField] float movingAnimationStartTime = 0.15f;
    [SerializeField] float movementSpeed = 5;

    [SerializeField] GameObject blastVFX;

    Vector2 path1 = Vector2.zero;
    Vector2 path2 = Vector2.zero;
    Vector2 path3= Vector2.zero;
    Vector2 path4;

    int randX;
    int randY;
    float tParam = 0;

    public override void Init()
    {
        BoosterManager.manager.isBoosterWorking = true;

        bezierPaths[0].SetParent(null);
        bezierPaths[3].SetParent(null);

        randX = GenerateRandom(1, Grid.width - 1);
        randY = GenerateRandom(1, Grid.height - 1);

        path1 = bezierPaths[0].position;
        path4 = Grid.grid.GetWorldPosition(randX, randY);
        bezierPaths[3].position = path4;

        if (randX < 4)
        {
            path2 = new Vector2(-Mathf.Abs(bezierPaths[1].position.x), bezierPaths[1].position.y);
            path3 = new Vector2(-Mathf.Abs(bezierPaths[2].position.x), bezierPaths[2].position.y);
        }
        else
        {
            path2 = new Vector2(Mathf.Abs(bezierPaths[1].position.x), bezierPaths[1].position.y);
            path3 = new Vector2(Mathf.Abs(bezierPaths[2].position.x), bezierPaths[2].position.y);
        }

        StartCoroutine(StartAnimating());
    }

    IEnumerator StartAnimating()
    {
        yield return new WaitForSeconds(movingAnimationStartTime);
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

        foreach (var path in bezierPaths)
            Destroy(path.gameObject);

        FireBooster();
    }

    private void FireBooster()
    {
        Instantiate(blastVFX, Grid.grid.GetWorldPosition(randX, randY), Quaternion.identity);

        Grid.grid.ForceClearPiece(randX + 1, randY);
        Grid.grid.ForceClearPiece(randX - 1, randY);
        Grid.grid.ForceClearPiece(randX, randY + 1);
        Grid.grid.ForceClearPiece(randX, randY - 1);
        Grid.grid.ForceClearPiece(randX, randY);

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.StartFillingGrid();

        Grid.grid.beingDestroyedByBooster = true;
        BoosterManager.manager.isBoosterWorking = false;

        Destroy(gameObject);
    }
}
