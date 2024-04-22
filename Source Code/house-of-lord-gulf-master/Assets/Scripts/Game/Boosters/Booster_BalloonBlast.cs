using UnityEngine;
using System.Collections.Generic;

public class Booster_BalloonBlast : Booster
{
    [SerializeField] GameObject rocketGO;
    [SerializeField] private float rocketSpeed = 5f;
    [SerializeField] private float movingAnimationStartTime = 0.15f;

    private readonly List<GameObject> rockets = new();
    private readonly List<int> usedColumns = new();
    private bool startMovingBalloons = false;
    private bool stop = false;

    #region Unity Functions

    private void Update()
    {
        if (!startMovingBalloons) return;
        if (stop) return;

        foreach (var rocket in rockets)
            if (Vector2.Distance(rocket.transform.position, new Vector2(rocket.transform.position.x, Grid.grid.GetWorldPosition(0, 0).y)) < 0.5f)
                BoostBalloon();
            else
                rocket.transform.position = Vector3.MoveTowards(rocket.transform.position, new Vector3(rocket.transform.position.x, Grid.grid.GetWorldPosition(0, 0).y, rocket.transform.position.y), rocketSpeed * Time.deltaTime);
    }

    #endregion

    #region Overriden Functions

    public override void Init()
    {
        BoosterManager.manager.isBoosterWorking = true;

        for (int i = 0; i < 3; i++)
        {
            int randX;
            do randX = GenerateRandom(0, Grid.width);
            while (usedColumns.Contains(randX));

            usedColumns.Add(randX);

            GameObject rocket = Instantiate(rocketGO, transform);
            rocket.transform.position = Grid.grid.GetWorldPosition(randX, Grid.height-1);
            rockets.Add(rocket);
        }
        // After 0.15f, The balloon animation start
        Invoke(nameof(StartAnimating), movingAnimationStartTime);
    }

    #endregion

    private void BoostBalloon()
    {
        stop = true;

        foreach (var x in usedColumns)
            for (int y = 0; y < Grid.height; y++)
                Grid.grid.ForceClearPiece(x, y);

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }

    //This Func Is Called From Animation Event
    public void StartAnimating()
    {
        startMovingBalloons = true;
    }
}
