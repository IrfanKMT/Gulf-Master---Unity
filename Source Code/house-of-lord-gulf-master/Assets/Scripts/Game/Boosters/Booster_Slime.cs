using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class Booster_Slime : Booster
{
    [SerializeField] GameObject slimeSpreadGO;
    [SerializeField] float spreadSizeIncreaseSpeed = 0.6f;
    [SerializeField] float spreadSpeed = 0.4f;

    #region Client Callbacks

    public override void Init()
    {
        Grid.grid.Grid_OnCandyClicked += StartBooster;
    }

    private void OnDisable()
    {
        Grid.grid.Grid_OnCandyClicked -= StartBooster;
    }

    #endregion

    #region Boosting

    private void StartBooster(GamePiece piece)
    {
        StartCoroutine(StartSpreading(piece.X, piece.Y));
    }

    IEnumerator StartSpreading(int x, int y)
    {
        BoosterManager.manager.isBoosterWorking = true;
        Grid.grid.Grid_OnCandyClicked -= StartBooster;

        //Run Loop 13 times
        int randX = x;
        int randY = y;
        int tempX = randX;
        int tempY = randY;
        bool start = true;

        List<Vector2Int> usedPieces = new();
        for (int i = 0; i < 13; i++)
        {
            int count = 0;

            if (!start)
            {
                do
                {
                    bool increaseVertical = GenerateRandom(0, 2) == 1;
                    tempX = randX;
                    tempY = randY;

                    if (increaseVertical)
                        tempY = GenerateRandom(0, 2) == 1 ? randY - 1 : randY + 1;
                    else
                        tempX = GenerateRandom(0, 2) == 1 ? randX - 1 : randX + 1;

                    count++;

                    if (count > 100)
                        break;

                } while (usedPieces.Contains(new(tempX, tempY)) || tempX < 0 || tempX > Grid.width - 1 || tempY < 0 || tempY > Grid.height - 1);
            }

            if (count < 100)
            {
                randX = tempX;
                randY = tempY;

                usedPieces.Add(new(randX, randY));
                Vector2 pos = Grid.grid.GetWorldPosition(randX, randY);
                GameObject slimeSpread = Instantiate(slimeSpreadGO, pos, Quaternion.identity);
                slimeSpread.transform.localScale = Vector3.zero;
                slimeSpread.transform.SetParent(Grid.grid.pieces[randX, randY].transform);
                slimeSpread.transform.DOScale(Vector3.one, spreadSizeIncreaseSpeed);

                yield return new WaitForSeconds(spreadSpeed);
                start = false;
            }
        }

        yield return new WaitForSeconds(1);

        foreach (var item in usedPieces)
            Grid.grid.ForceClearPiece(item.x, item.y);

        BoosterManager.manager.Server_SetBoosterCollectedCandyToZero();

        Grid.grid.StartFillingGrid();
        Grid.grid.beingDestroyedByBooster = true;

        BoosterManager.manager.isBoosterWorking = false;
        Destroy(gameObject);
    }
    #endregion
}
