using UnityEngine;
using Mirror;

public class GamePiece : MonoBehaviour
{
    #region Variables

    public bool destroyFirst = false; //If true, this piece's Clearable Script's Clear Function will be called before any other gem's clear fucntion is called, Can be used in special pieces such as mushroom

    private int x;
    private int y;

    public int X
    {
        get { return x; }
        set
        {
            if (IsMovable())
                x = value;
        }
    }
    public int Y
    {
        get { return y; }
        set
        {
            if (IsMovable())
                y = value;
        }
    }

    private PieceType type;
    public PieceType Type
    {
        get { return type; }
    }

    private Grid grid;
    public Grid GridRef
    {
        get { return grid; }
    }

    public MovablePiece movableComponent;
    public MovablePiece MovableComponent
    {
        get { return movableComponent; }
    }

    public ColorPiece colorComponent;
    public ColorPiece ColorComponent
    {
        get { return colorComponent; }
    }

    public ClearablePiece clearableComponent;
    public ClearablePiece ClearableComponent
    {
        get { return clearableComponent; }
    }

    #endregion

    //private void Awake()
    //{
    //    movableComponent = GetComponent<MovablePiece>();
    //    colorComponent = GetComponent<ColorPiece>();
    //    clearableComponent = GetComponent<ClearablePiece>();
    //}

    public void Initialize(int x, int y, Grid grid, PieceType type)
    {
        this.x = x;
        this.y = y;
        this.grid = grid;
        this.type = type;

    }

    #region Helper Functions

    public bool IsMovable()
    {
        return movableComponent != null;
    }

    public bool IsColored()
    {
        return colorComponent != null;
    }

    public bool IsClearable()
    {
        return clearableComponent != null;
    }

    #endregion

    #region Input Handling

    [ClientCallback]
    private void OnMouseDown()
    {
        grid.PressPiece(this);
    }

    [ClientCallback]
    private void OnMouseOver()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //Debug.Log(gameObject.name);
            grid.ClickCandy(this);
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            //Debug.Log(gameObject.name);
            grid.OnMouseOverCandy(this);
        }
    }

    #endregion
}
