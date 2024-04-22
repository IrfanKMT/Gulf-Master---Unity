using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CandyContainer", menuName = "Game/CandyContainer")]
public class CandyContainer : ScriptableObject
{
    public List<PiecePrefab> piecePrefabs;
    [Space]
    public List<ClearablePiece> clearablePieces;
    [Space]
    public List<MovablePiece> movablePieces;

    private GameObject obj;
    private GamePiece pooledObject;
    private GamePiece InstantiatedPiece;

    /// <summary>
    /// Resets Lists
    /// </summary>
    public void ClearData()
    {
        movablePieces.Clear();
        clearablePieces.Clear();
    }

    /// <summary>
    /// Adds Pices in list
    /// </summary>
    /// <param name="pieceObj"></param>
    public void AddPices(GameObject pieceObj)
    {
        if (pieceObj.TryGetComponent(out ClearablePiece clearblePice))
        {
            if (!clearablePieces.Contains(clearblePice))
            {
                clearablePieces.Add(clearblePice);
            }
        }

        if (pieceObj.TryGetComponent(out MovablePiece movePieces))
        {
            if (!movablePieces.Contains(movePieces))
            {
                movablePieces.Add(movePieces);
            }
        }
    }
    /// <summary>
    /// Removes Destroyed Candys
    /// </summary>
    public void RemoveDeletedPices()
    {
        //Debug.Log("Removed From Here");
        clearablePieces.RemoveAll(item => item == null);
        movablePieces.RemoveAll(item => item == null);
    }
}
