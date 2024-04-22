using UnityEngine;
using System.Collections;
using System;

public class MovablePiece : MonoBehaviour
{
    public GamePiece piece;
    private IEnumerator moveCoroutine;
    internal bool moving = false;

    //private void Awake()
    //{
    //    piece = GetComponent<GamePiece>();
    //}

    public void MovePiece(int newX, int newY, bool shufflingOrSwapping = false, bool showHand = false)
    {
        moving = true;

        if (moveCoroutine!= null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = MoveCoroutine(newX, newY, shufflingOrSwapping, showHand);
        StartCoroutine(moveCoroutine);
    }

    private IEnumerator MoveCoroutine(int newX, int newY, bool shufflingOrSwapping, bool showHand)
    {
        try
        {
            if (showHand)
            {
                Grid.grid.handGO.transform.SetParent(transform);
                Grid.grid.handGO.transform.localPosition = new Vector3(0.32f, -0.32f, 0);
                Grid.grid.handGO.SetActive(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Moveable Piece Error : Error Occured In MoveCoroutine While Using Hand. \nError Message : " + e.Message + "\nError StackTrace : " + e.StackTrace);
        }

        piece.gameObject.name = "Piece(" + newX + "," + newY + ")";

        piece.X = newX;
        piece.Y = newY;

        Vector3 endPos = piece.GridRef.GetWorldPosition(newX, newY);
        float originalDistanceBetweenStartAndEndPos = Vector2.Distance(piece.transform.position, endPos);
        float mutliplier = shufflingOrSwapping ? 5 : Mathf.Clamp(newY + 0.6f, 2, 5);
        while (Vector2.Distance(piece.transform.position, endPos) > 0.01f)
        {
            float animationValue = Grid.grid.gemMovementCurve.Evaluate((originalDistanceBetweenStartAndEndPos - Vector2.Distance(piece.transform.position, endPos)) / originalDistanceBetweenStartAndEndPos);
            piece.transform.position = Vector2.Lerp(piece.transform.position, endPos, Grid.grid.gemsFallingSpeed * mutliplier * Time.deltaTime * animationValue);

            if (Vector2.Distance(piece.transform.position, endPos) < Grid.grid.gemsStoppingDistance)
                if (showHand)
                    Grid.grid.handGO.transform.SetParent(null);

            yield return 0;
        }

        try
        {
            if (showHand)
            {
                Grid.grid.handGO.transform.SetParent(null);
                Grid.grid.handGO.SetActive(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Moveable Piece Error : Error Occured In MoveCoroutine While Using Hand. \nError Message : " + e.Message + "\nError StackTrace : " + e.StackTrace);
        }

        piece.transform.position = endPos;
        moving = false;
    }
}

