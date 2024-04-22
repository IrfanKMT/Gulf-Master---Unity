using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class Grid : NetworkBehaviour
{
    #region Variables

    public static Grid grid;

    public const int width = 7;
    public const int height = 7;

    public bool isAnyGemMoving;

    [SerializeField] GameModeType gameMode;

    [Tooltip("The minimum value should be 1, because the y value is directly multiplied into the current speed of the gem")]
    public AnimationCurve gemMovementCurve;

    [Range(0f, 0.1f)] public float gemsStoppingDistance = 0.09f;
    public float clearingTime = 0.2f;
    public float gemsFallingSpeed = 1.2f;
    public float timeBeforeClearingPiece = 1f;
    [SerializeField] float timeBetweenClearingCandyForLineBombs = 0.1f;

    [SerializeField] List<PiecePrefab> piecePrefabs = new();
    [SerializeField] GameObject backgroundTileBlue;
    [SerializeField] GameObject backgroundTilePurple;

    public GameObject handGO;
    [Space]
    //Holds Refrence to all genrated candy 
    //So that we don't have to find by findobjectoftypes
    [Header("CandyContainer")]
    public CandyContainer candyContainer;
    [Space]
    [Header("Script Refrence")]
    public GamePlayManager gamePlayManager;

    internal bool isFilling = false;
    internal bool gridFilled = false;
    internal bool beingDestroyedByBooster = false;
    internal bool isSwappingPiece = false;
    internal bool swappingBack = false;

    internal Dictionary<PieceType, GameObject> piecePrefabsDict = new();
    internal GamePiece[,] pieces;

    // Swap Vars
    internal GamePiece pressedPiece;
    internal GamePiece releasedPiece;
    internal SwapDirection swapDir;
    internal int numberOfLineBombs = 0; // Dont start refilling until number of row/column bombs is 0

    Vector2 firstTouchPosition = Vector2.zero;
    Vector2 finalTouchPosition = Vector2.zero;

    int seeds;
    protected System.Random randomGenerator;

    private bool isAlreadyFillingGrid = false;
    internal bool isGridSynced;
    private bool IsPlayer1
    {
        get
        {
            var localPlayer = GamePlayManager.manager.LocalPlayer;
            if (localPlayer != null)
                return localPlayer.isPlayer1;
            else
                return true;
        }

        set { }
    }

    private bool IsPlayer1Partner
    {
        get
        {
            GamePlayer[] gp = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);

            foreach (GamePlayer player in gp)
            {
                if (player.team1p2 && player.isLocalPlayer)
                {
                    Debug.Log("Return From Here");
                    return true;
                }
            }
            return false;
        }
        set { }
    }

    // For Pathway Boosters
    public const int maxPathwayCandies = 9;
    private readonly List<GamePiece> swipedCandyPath = new();
    IEnumerator sendGridDataRPCCoroutine;

    private Camera MainCam;

    #endregion

    #region Events

    public event Action<GamePiece, TeamType> Grid_OnCandyDestroyed;
    public event Action<GamePiece> Grid_OnCandyClicked;
    public event Action<GamePiece, GamePiece> Grid_OnCandiesSwapped;
    public event Action<List<GamePiece>> Grid_OnCandiesPathMade;
    public event Action OnExtraMoveGained;

    #endregion

    #region Unity Functions

    protected virtual void Awake()
    {
        grid = this;
        MainCam = Camera.main;
    }

    [ClientCallback]
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            firstTouchPosition = MainCam.ScreenToWorldPoint(Input.mousePosition);
            //Debug.Log("hear  "+ MainCam.ScreenToWorldPoint(Input.mousePosition));
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            //Debug.Log("ReleasePiece");
            ReleasePiece();
            if (swipedCandyPath.Count > 0)
            {
                //Done on GamePlayer.cs
                gamePlayManager.LocalPlayer.Grid_OnCandiesPathMade(swipedCandyPath);
                swipedCandyPath.Clear();
            }
        }
    }

    #endregion

    #region Initialization

    public void Initialize(int seed, bool spawnInitialPieces, bool setSeed)
    {


        pieces = new GamePiece[width, height];

        if (setSeed)
        {
            seeds = seed;
            print("Generator init : Seed set to " + seeds);
            randomGenerator = new(seed);
        }
        // Initialize Dictionary
        foreach (var item in piecePrefabs)
            if (!piecePrefabsDict.ContainsKey(item.type))
                piecePrefabsDict.Add(item.type, item.prefab);

        // Instantiate Background Tiles
        bool useBlueTile = UnityEngine.Random.Range(0, 2) == 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject bg = Instantiate(useBlueTile ? backgroundTileBlue : backgroundTilePurple, GetWorldPosition(x, y), Quaternion.identity);
                bg.transform.SetParent(transform);
                useBlueTile = !useBlueTile;
            }
        }

        // Instantiate Pieces
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                SpawnNewPiece(x, y, PieceType.EMPTY);

        if (spawnInitialPieces)
            StartFillingGrid(false);
    }

    #endregion

    #region Spawning New Piece

    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(transform.position.x - width / 2f + x, transform.position.y + height / 2f - y);
    }

    public GamePiece SpawnNewPiece(int x, int y, PieceType type)
    {
        GameObject piece = Instantiate(piecePrefabsDict[type], GetWorldPosition(x, y), Quaternion.identity);
        piece.transform.SetParent(transform);
        piece.name = type.ToString() + " (" + x + "," + y + ")";
        pieces[x, y] = piece.GetComponent<GamePiece>();
        pieces[x, y].Initialize(x, y, this, type);

        return pieces[x, y];
    }

    #endregion

    #region Filling Board

    public void StartFillingGrid(bool isPerkUsed = false)
    {


        StartCoroutine(Fill(false, false, isPerkUsed));
    }

    private IEnumerator Fill(bool madeSpecialPieceAlready = false, bool swappedCandy = false, bool isPerkUsed = false, bool isLittleDragonBoosterActive = false)
    {
        if (isAlreadyFillingGrid) yield break;
        isAlreadyFillingGrid = true;

        isFilling = true;

        yield return new WaitWhile(IsAnyGemMovingOrClearing);
        yield return new WaitWhile(() => numberOfLineBombs != 0);




        bool madeAnySpecialPieces = false;
        bool needsRefill = true;

        while (needsRefill)
        {
            yield return new WaitWhile(IsAnyGemClearing);
            yield return new WaitForSeconds(clearingTime);
            while (FillStep())
                yield return new WaitForSeconds(clearingTime);

            yield return new WaitWhile(IsAnyGemMoving);

            var clearingData = ClearAllValidMatches();
            needsRefill = clearingData.x == 1;

            if (!madeAnySpecialPieces)
                madeAnySpecialPieces = clearingData.y == 1;
        }

        Debug.Log("IsPlayer1  " + IsPlayer1);
        Debug.Log("IsPlayer1Partner  " + IsPlayer1Partner);

        isFilling = false;

        if (gameMode == GameModeType.Mushroom_Mode)
            if (!gridFilled)
                GetComponent<Grid_MushroomMode>().SpawnMushrooms();

        gridFilled = true;
        isGridSynced = false;

        if (!IsDeadlocked())
            Server_GridFilled("Fill");

        if (!madeAnySpecialPieces && !isLittleDragonBoosterActive && !madeSpecialPieceAlready && swappedCandy)
            gamePlayManager.Server_DecreaseMovesCounter();
        else if (isPerkUsed && madeAnySpecialPieces) // && !IsAnyBoosterWorking()) dont add this cause players can still use MystryHat booster and perk to get extra move 
            gamePlayManager.Server_IncreaseMovesCounter();


        if (IsDeadlocked())
            ShuffleBoard();

        isAlreadyFillingGrid = false;
    }

    private bool FillStep()
    {
        bool movedPiece = false;

        // 0 = top, so Y starts from top
        for (int y = height - 2; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                GamePiece piece = pieces[x, y];

                if (piece.IsMovable())
                {
                    GamePiece pieceBelow = pieces[x, y + 1];
                    if (pieceBelow.Type == PieceType.EMPTY)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.MovableComponent.MovePiece(x, y + 1);
                        pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.EMPTY);
                        movedPiece = true;
                    }
                }
            }
        }

        //Checking If first row is empty
        for (int x = 0; x < width; x++)
        {
            GamePiece piece = pieces[x, 0];

            if (piece.Type == PieceType.EMPTY)
            {
                Destroy(piece.gameObject);

                GameObject newPiece = Instantiate(piecePrefabsDict[PieceType.NORMAL], GetWorldPosition(x, -1), Quaternion.identity);
                newPiece.transform.SetParent(transform);

                GamePiece spawnedPiece = newPiece.GetComponent<GamePiece>();
                spawnedPiece.Initialize(x, -1, this, PieceType.NORMAL);
                spawnedPiece.MovableComponent.MovePiece(x, 0);

                List<ColorType> usableColors = new();
                if (x > 0)
                {
                    foreach (ColorType color in Enum.GetValues(typeof(ColorType)))
                    {
                        if (color == ColorType.Count)
                            continue;

                        //Left
                        if (pieces[x - 1, 0] != null)
                            if (pieces[x - 1, 0].ColorComponent != null)
                            {
                                switch (gamePlayManager.matchtype)
                                {
                                    case MatchType.TwoPlayer:
                                        if (pieces[x - 1, 0].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Blue && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x - 1, 0].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Red && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x - 1, 0].ColorComponent.Color == color)
                                            continue;
                                        break;

                                    case MatchType.FourPlayer:
                                        if (pieces[x - 1, 0].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Blue && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x - 1, 0].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Red && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x - 1, 0].ColorComponent.Color == color)
                                            continue;
                                        break;
                                }


                            }

                        //Bottom Left
                        if (gridFilled)
                            if (pieces[x - 1, 1] != null)
                                if (pieces[x - 1, 1].ColorComponent != null)
                                {
                                    switch (gamePlayManager.matchtype)
                                    {
                                        case MatchType.TwoPlayer:
                                            if (pieces[x - 1, 1].ColorComponent.Color == ColorType.Red)
                                            {
                                                if (color == ColorType.Red && !IsPlayer1)
                                                    continue;
                                                else if (color == ColorType.Blue && IsPlayer1)
                                                    continue;
                                            }
                                            else if (pieces[x - 1, 1].ColorComponent.Color == ColorType.Blue)
                                            {
                                                if (color == ColorType.Blue && !IsPlayer1)
                                                    continue;
                                                else if (color == ColorType.Red && IsPlayer1)
                                                    continue;
                                            }
                                            else if (pieces[x - 1, 1].ColorComponent.Color == color)
                                                continue;
                                            break;

                                        case MatchType.FourPlayer:
                                            if (pieces[x - 1, 1].ColorComponent.Color == ColorType.Red)
                                            {
                                                if (color == ColorType.Red && !IsPlayer1 && !IsPlayer1Partner)
                                                    continue;
                                                else if (color == ColorType.Blue && (IsPlayer1 || IsPlayer1Partner))
                                                    continue;
                                            }
                                            else if (pieces[x - 1, 1].ColorComponent.Color == ColorType.Blue)
                                            {
                                                if (color == ColorType.Blue && !IsPlayer1 && !IsPlayer1Partner)
                                                    continue;
                                                else if (color == ColorType.Red && (IsPlayer1 || IsPlayer1Partner))
                                                    continue;
                                            }
                                            else if (pieces[x - 1, 1].ColorComponent.Color == color)
                                                continue;
                                            break;
                                    }


                                }


                        // Bottom
                        if (pieces[x, 1] != null)
                            if (pieces[x, 1].ColorComponent != null)
                            {
                                switch (gamePlayManager.matchtype)
                                {
                                    case MatchType.TwoPlayer:
                                        if (pieces[x, 1].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Blue && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Red && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == color)
                                            continue;
                                        break;

                                    case MatchType.FourPlayer:
                                        if (pieces[x, 1].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Blue && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Red && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == color)
                                            continue;
                                        break;
                                }


                            }

                        usableColors.Add(color);
                    }
                }
                else
                {
                    foreach (ColorType color in Enum.GetValues(typeof(ColorType)))
                    {
                        if (color == ColorType.Count)
                            continue;

                        // Bottom
                        if (pieces[x, 1] != null)

                            switch (gamePlayManager.matchtype)
                            {
                                case MatchType.TwoPlayer:
                                    if (pieces[x, 1].ColorComponent != null)
                                    {
                                        if (pieces[x, 1].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Blue && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1)
                                                continue;
                                            else if (color == ColorType.Red && IsPlayer1)
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == color)
                                            continue;
                                    }
                                    break;

                                case MatchType.FourPlayer:
                                    if (pieces[x, 1].ColorComponent != null)
                                    {
                                        if (pieces[x, 1].ColorComponent.Color == ColorType.Red)
                                        {
                                            if (color == ColorType.Red && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Blue && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == ColorType.Blue)
                                        {
                                            if (color == ColorType.Blue && !IsPlayer1 && !IsPlayer1Partner)
                                                continue;
                                            else if (color == ColorType.Red && (IsPlayer1 || IsPlayer1Partner))
                                                continue;
                                        }
                                        else if (pieces[x, 1].ColorComponent.Color == color)
                                            continue;
                                    }
                                    break;
                            }



                        usableColors.Add(color);
                    }
                }
                int randomColor = randomGenerator.Next(0, usableColors.Count);
                spawnedPiece.ColorComponent.SetColor(usableColors[randomColor]);

                //Adding Candy In List
                candyContainer.AddPices(spawnedPiece.gameObject);

                pieces[x, 0] = spawnedPiece;
                movedPiece = true;
            }
        }

        return movedPiece;
    }

    #endregion

    #region Input Handling

    [Client]
    public void ClickCandy(GamePiece candy)
    {
        // Done By GamePlayer.cs
        if (candy != null)
            gamePlayManager.LocalPlayer.Grid_OnCandyClicked(candy);
    }

    [Client]
    public void PressPiece(GamePiece piece)
    {
        // Done By GamePlayer.cs
        //if (gamePlayManager.Client_IsMyTurn() && gamePlayManager.Client_AnyMovesLeft() && !isFilling && !IsBoosterActive() && !IsAnyGemMovingOrClearing() && !isSwappingPiece)

        //Debug.Log("Client_IsMyTurn  " + gamePlayManager.Client_IsMyTurn() + " Client_AnyMovesLeft " + gamePlayManager.Client_AnyMovesLeft() + "  isFilling " + isFilling + " IsAnyGemMovingOrClearing  " + IsAnyGemMovingOrClearing() + "  isSwappingPiece " + isSwappingPiece);


        if (gamePlayManager.Client_IsMyTurn() && gamePlayManager.Client_AnyMovesLeft() && !isFilling && !IsAnyGemMovingOrClearing() && !isSwappingPiece)
            pressedPiece = piece;
    }

    [Client]
    private void ReleasePiece()
    {
        finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //Debug.Log(gamePlayManager.Client_IsMyTurn());
        //Debug.Log(gamePlayManager.Client_AnyMovesLeft());
        //Debug.Log(isFilling);
        //Debug.Log(IsAnyGemMovingOrClearing());
        //Debug.Log(pressedPiece);

        // Done By GamePlayer.cs
        //if (gamePlayManager.Client_IsMyTurn() && gamePlayManager.Client_AnyMovesLeft() && !isFilling && !IsBoosterActive() && !IsAnyGemMovingOrClearing() && pressedPiece!=null && !isSwappingPiece)
        //Debug.Log("Client_IsMyTurn  " + gamePlayManager.Client_IsMyTurn());
        //Debug.Log("Client_AnyMovesLeft  " + gamePlayManager.Client_AnyMovesLeft());
        if (gamePlayManager.Client_IsMyTurn() && gamePlayManager.Client_AnyMovesLeft() && !isFilling && !IsAnyGemMovingOrClearing() && pressedPiece != null)
            if (Vector2.Distance(finalTouchPosition, firstTouchPosition) < 4f && Vector2.Distance(finalTouchPosition, firstTouchPosition) > 0.3f)
                gamePlayManager.LocalPlayer.Grid_MovePieces(Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI, pressedPiece);
    }

    [ClientCallback]
    public void OnMouseOverCandy(GamePiece piece)
    {
        if (!IsPathwayBoosterActive()) return;

        if (!swipedCandyPath.Contains(piece) && swipedCandyPath.Count < maxPathwayCandies)
        {
            if (swipedCandyPath.Count > 0)
            {
                GamePiece lastPiece = swipedCandyPath.Last();

                if (Vector2.Distance(new Vector2(lastPiece.X, lastPiece.Y), new Vector2(piece.X, piece.Y)) > 1)
                    return;
            }
            swipedCandyPath.Add(piece);
        }
    }

    #endregion

    #region Event Invoking Functions

    public void Invoke_CandyClickedEvent(GamePiece piece)
    {
        Grid_OnCandyClicked?.Invoke(piece);
    }

    public void Invoke_CandyPathMadeEvent(Vector2Int[] positions)
    {
        List<GamePiece> piece = new();
        foreach (var item in positions)
            piece.Add(pieces[item.x, item.y]);

        Grid_OnCandiesPathMade?.Invoke(piece);
    }

    #endregion

    #region Swapping Pieces

    public bool IsAdjacent(GamePiece piece1, GamePiece piece2)
    {
        return (piece1.X == piece2.X && (int)Mathf.Abs(piece1.Y - piece2.Y) == 1) || (piece1.Y == piece2.Y && (int)Mathf.Abs(piece1.X - piece2.X) == 1);
    }

    public void SwapPieces(GamePiece piece1, GamePiece piece2)
    {
        //if (ReconnectionHandler.handler.wait || !ReconnectionHandler.handler.gameDataInitialized) return;
        if (piece1.IsMovable() && piece2.IsMovable())
        {
            isSwappingPiece = true;
            StartCoroutine(Coroutine_SwapPieces(piece1, piece2));
        }
    }

    IEnumerator Coroutine_SwapPieces(GamePiece piece1, GamePiece piece2)
    {
        int piece1X = piece1.X;
        int piece1Y = piece1.Y;

        int piece2X = piece2.X;
        int piece2Y = piece2.Y;

        pieces[piece1X, piece1Y] = piece2;
        pieces[piece2X, piece2Y] = piece1;

        piece1.MovableComponent.MovePiece(piece2X, piece2Y, true);
        piece2.MovableComponent.MovePiece(piece1X, piece1Y, true);

        if (GetMatch(piece1, piece2X, piece2Y) != null || GetMatch(piece2, piece1X, piece1Y) != null)
        {
            bool isLilDragonBoosterActive = IsLilDragonBoosterActive();
            beingDestroyedByBooster = false;

            piece1.MovableComponent.MovePiece(piece2X, piece2Y, true, !isLilDragonBoosterActive);

            Grid_OnCandiesSwapped?.Invoke(piece1, piece2);

            yield return new WaitWhile(IsAnyGemMovingOrClearing);
            yield return new WaitForSeconds(timeBeforeClearingPiece);

            var clearingData = ClearAllValidMatches();
            bool specialCandyMade = clearingData.y == 1;

            pressedPiece = null;
            releasedPiece = null;
            swapDir = SwapDirection.Null;

            if (specialCandyMade)
                OnExtraMoveGained?.Invoke();

            StartCoroutine(Fill(specialCandyMade, true, false, isLilDragonBoosterActive));
        }
        else
        {
            swappingBack = true;
            yield return new WaitWhile(IsAnyGemMovingOrClearing);

            pieces[piece1X, piece1Y] = piece1;
            pieces[piece2X, piece2Y] = piece2;

            piece1.MovableComponent.MovePiece(piece1X, piece1Y, true);
            piece2.MovableComponent.MovePiece(piece2X, piece2Y, true);

            yield return new WaitWhile(IsAnyGemMovingOrClearing);
        }

        swappingBack = false;
        isSwappingPiece = false;
    }

    #endregion

    #region Matches

    public List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
    {
        if (piece.IsColored())
        {
            ColorType color = piece.ColorComponent.Color;
            List<GamePiece> horPieces = new();
            List<GamePiece> verPieces = new();
            List<GamePiece> matchingPieces = new();

            // CHECK IF ANY HORIZONTAL MATCH FOUND
            #region Horizontal Check

            horPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < width; xOffset++)
                {
                    int x;

                    if (dir == 0) // LEFT
                        x = newX - xOffset;
                    else // RIGHT
                        x = newX + xOffset;

                    if (x < 0 || x >= width)
                        break;

                    if (pieces[x, newY].IsColored() && pieces[x, newY].ColorComponent.Color == color)
                        horPieces.Add(pieces[x, newY]);
                    else
                        break;

                }
            }

            if (horPieces.Count >= 3)
            {
                matchingPieces.AddRange(horPieces);

                // CHECK FOR L OR T SHAPE
                for (int i = 0; i < horPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < width; yOffset++)
                        {
                            int y;

                            if (dir == 0)
                                y = newY - yOffset;
                            else
                                y = newY + yOffset;

                            if (y < 0 || y >= height)
                                break;

                            if (pieces[horPieces[i].X, y].IsColored() && pieces[horPieces[i].X, y].ColorComponent.Color == color)
                                verPieces.Add(pieces[horPieces[i].X, y]);
                            else
                                break;
                        }
                    }

                    if (verPieces.Count < 2)
                    {
                        verPieces.Clear();
                    }
                    else
                    {
                        matchingPieces.AddRange(verPieces);
                        break;
                    }

                }
            }

            if (matchingPieces.Count >= 3)
                return matchingPieces;

            #endregion

            // CHECK IF ANY VERTICAL MATCH FOUND
            #region Vertical Check
            horPieces.Clear();
            verPieces.Clear();
            verPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < height; yOffset++)
                {
                    int y;

                    if (dir == 0) // LEFT
                        y = newY - yOffset;
                    else // RIGHT
                        y = newY + yOffset;

                    if (y < 0 || y >= height)
                        break;

                    if (pieces[newX, y].IsColored() && pieces[newX, y].ColorComponent.Color == color)
                        verPieces.Add(pieces[newX, y]);
                    else
                        break;

                }
            }

            if (verPieces.Count >= 3)
            {
                matchingPieces.AddRange(verPieces);

                // CHECK FOR L OR T SHAPE
                for (int i = 0; i < verPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < height; xOffset++)
                        {
                            int x;

                            if (dir == 0)
                                x = newX - xOffset;
                            else
                                x = newX + xOffset;

                            if (x < 0 || x >= width)
                                break;

                            if (pieces[x, verPieces[i].Y].IsColored() && pieces[x, verPieces[i].Y].ColorComponent.Color == color)
                                horPieces.Add(pieces[x, verPieces[i].Y]);
                            else
                                break;
                        }
                    }

                    if (horPieces.Count < 2)
                    {
                        horPieces.Clear();
                    }
                    else
                    {
                        matchingPieces.AddRange(horPieces);
                        break;
                    }

                }
            }

            if (matchingPieces.Count >= 3)
            {
                return matchingPieces;
            }

            #endregion
        }

        return null;
    }

    #endregion

    #region Clearing Pieces

    private bool IsColumnOrRow(List<GamePiece> pieces)
    {
        int hor = 0;
        int ver = 0;
        GamePiece mainPiece = pieces[0];

        foreach (var item in pieces)
        {
            if (item.ColorComponent.Color == mainPiece.ColorComponent.Color)
            {
                if (item.X == mainPiece.X)
                    hor++;
                else if (item.Y == mainPiece.Y)
                    ver++;
            }
        }

        bool isLightening = (hor == 4 && ver == 1) || (ver == 4 && hor == 1) || (ver == 5 && hor == 0) || (ver == 0 && hor == 5);
        return isLightening;
    }

    private Vector2 ClearAllValidMatches()
    {
        bool needsRefill = false;
        bool madeSpecialPiece = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pieces[x, y].IsClearable())
                {
                    var matches = GetMatch(pieces[x, y], x, y);
                    if (matches != null)
                    {
                        PieceType specialPieceType = PieceType.EMPTY;
                        GamePiece randomPiece = matches[0];

                        #region Variables for making sure not to destroy the newly formed bomb when a match already has a row/column bomb in it

                        bool doesMatchContainsLineBomb = matches.Where(i => i.Type == PieceType.ROW_CLEAR || i.Type == PieceType.COLUMN_CLEAR).Any();
                        bool isMatchVertical = matches.Where(i => i.X == matches[0].X).Count() == matches.Count;
                        GamePiece lineClearingBombInMatch = matches.Where(i => i.Type == (isMatchVertical ? PieceType.COLUMN_CLEAR : PieceType.ROW_CLEAR)).Any() ? matches.Where(i => i.Type == (isMatchVertical ? PieceType.COLUMN_CLEAR : PieceType.ROW_CLEAR)).First() : null;

                        if (!doesMatchContainsLineBomb)
                            lineClearingBombInMatch = null;

                        #endregion

                        int specialPieceX = randomPiece.X;
                        int specialPieceY = randomPiece.Y;

                        if (matches.Count == 4)
                        {
                            if (pressedPiece == null)
                            {
                                specialPieceType = PieceType.ROW_CLEAR;
                            }
                            else
                            {
                                if (swapDir == SwapDirection.Left || swapDir == SwapDirection.Right)
                                    specialPieceType = PieceType.COLUMN_CLEAR;
                                else
                                    specialPieceType = PieceType.ROW_CLEAR;
                            }
                        }
                        else if (matches.Count >= 5)
                        {
                            if (IsColumnOrRow(matches))
                                specialPieceType = PieceType.ELECTRIC;
                            else
                                specialPieceType = PieceType.BOMB;
                        }

                        /////////////// CHECKING IF MATCHES CONTAINS A PIECE WHICH NEED TO BE CLEARED FIRST, IF YES, THEN DESTROY IT FIRST

                        for (int i = 0; i < matches.Count; i++)
                        {
                            var item = matches[i];
                            if (item.destroyFirst)
                            {
                                matches.Remove(item);

                                if (ClearPiece(item.X, item.Y))
                                {
                                    needsRefill = true;

                                    if (item == pressedPiece || item == releasedPiece)
                                    {
                                        specialPieceX = item.X;
                                        specialPieceY = item.Y;
                                    }
                                }
                            }
                        }

                        foreach (var item in matches)
                        {
                            if (ClearPiece(item.X, item.Y))
                            {
                                needsRefill = true;

                                if (item == pressedPiece || item == releasedPiece)
                                {
                                    specialPieceX = item.X;
                                    specialPieceY = item.Y;
                                }
                            }
                        }
                        if (specialPieceType != PieceType.EMPTY)
                        {
                            madeSpecialPiece = true;
                            bool isFirstCandyInMatchColored = matches[0].IsColored();
                            ColorType firstCandyInMatchColor = matches[0].ColorComponent.Color;
                            StartCoroutine(Coroutine_CreateSpecialTypePieceAfterClearingLineBomb(specialPieceType, specialPieceX, specialPieceY, isFirstCandyInMatchColored, firstCandyInMatchColor, lineClearingBombInMatch));
                        }
                    }
                }
            }
        }

        return new Vector2(needsRefill ? 1 : 0, madeSpecialPiece ? 1 : 0);
    }

    private bool IsAnyCandyMatching()
    {
        bool result = false;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (pieces[x, y].IsClearable() && GetMatch(pieces[x, y], x, y) != null)
                    result = true;

        return result;
    }

    IEnumerator Coroutine_CreateSpecialTypePieceAfterClearingLineBomb(PieceType specialPieceType, int specialPieceX, int specialPieceY, bool isFirstCandyInMatchColored, ColorType firstCandyInMatchColor, GamePiece lineClearingBombInMatch)
    {
        if (lineClearingBombInMatch != null)
            yield return new WaitWhile(() => lineClearingBombInMatch != null);

        CreateSpecialTypePiece(specialPieceType, specialPieceX, specialPieceY, isFirstCandyInMatchColored, firstCandyInMatchColor);
    }

    private void CreateSpecialTypePiece(PieceType specialPieceType, int specialPieceX, int specialPieceY, bool isFirstCandyInMatchColored, ColorType firstCandyInMatchColor)
    {
        Destroy(pieces[specialPieceX, specialPieceY].gameObject);
        GamePiece newPiece = SpawnNewPiece(specialPieceX, specialPieceY, specialPieceType);

        if ((specialPieceType == PieceType.ROW_CLEAR || specialPieceType == PieceType.COLUMN_CLEAR || specialPieceType == PieceType.ELECTRIC || specialPieceType == PieceType.BOMB) && newPiece.IsColored() && isFirstCandyInMatchColored)
            newPiece.ColorComponent.SetColor(firstCandyInMatchColor);

        //Adding Special Candy In List
        candyContainer.AddPices(newPiece.gameObject);
        //Remove Destoyed Candy
        candyContainer.RemoveDeletedPices();
    }

    public bool ClearPiece(int x, int y, bool destroyedByBooster = false, PieceType clearingPieceType = PieceType.NORMAL)
    {
        if (pieces[x, y].IsClearable() && !pieces[x, y].ClearableComponent.IsBeingCleared)
        {
            if (!destroyedByBooster)
                Grid_OnCandyDestroyed?.Invoke(pieces[x, y], gamePlayManager.currentTurnPlayerteamtype);

            if (pieces[x, y].transform.childCount > 0)
            {
                pieces[x, y].transform.GetChild(0).gameObject.SetActive(false);
                pieces[x, y].transform.GetChild(0).SetParent(null);
            }

            pieces[x, y].ClearableComponent.ClearPiece(clearingPieceType);
            SpawnNewPiece(x, y, PieceType.EMPTY);
            return true;
        }

        return false;
    }

    public void ForceClearPiece(int x, int y, bool destroyedByPerks = false)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            try
            {
                if (pieces[x, y] == null) return;

                if (pieces[x, y].Type != PieceType.EMPTY)
                {
                    if (destroyedByPerks)
                    {
                        Grid_OnCandyDestroyed?.Invoke(pieces[x, y], gamePlayManager.currentTurnPlayerteamtype);
                    }

                    pieces[x, y].ClearableComponent.ClearPiece();
                    SpawnNewPiece(x, y, PieceType.EMPTY);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error Clearing Piece At Position : " + new Vector2(x, y).ToString() + "\nError Message : " + e.Message + "\nStack Trace : " + e.StackTrace);
            }
        }
    }

    public void ClearRow(int X, int Y) => StartCoroutine(Coroutine_ClearRow(X, Y));

    IEnumerator Coroutine_ClearRow(int X, int Y)
    {
        int leftCandy = X - 1;
        int rightCandy = X + 1;

        for (int x = 0; x < width; x++)
        {
            if (leftCandy >= 0 && leftCandy < width)
                ClearPiece(leftCandy, Y, false, PieceType.ROW_CLEAR);

            if (rightCandy >= 0 && rightCandy < width)
                ClearPiece(rightCandy, Y, false, PieceType.ROW_CLEAR);

            yield return new WaitForSeconds(timeBetweenClearingCandyForLineBombs);

            leftCandy--;
            rightCandy++;
        }

        StartFillingGrid();
    }

    public void ClearColumn(int X, int Y) => StartCoroutine(Coroutine_ClearColumn(X, Y));

    IEnumerator Coroutine_ClearColumn(int X, int Y)
    {
        int upCandy = Y - 1;
        int downCandy = Y + 1;

        for (int x = 0; x < width; x++)
        {
            if (upCandy >= 0 && upCandy < height)
                ClearPiece(X, upCandy, false, PieceType.COLUMN_CLEAR);

            if (downCandy >= 0 && downCandy < height)
                ClearPiece(X, downCandy, false, PieceType.COLUMN_CLEAR);

            yield return new WaitForSeconds(timeBetweenClearingCandyForLineBombs);

            upCandy--;
            downCandy++;
        }

        StartFillingGrid();
    }

    public void ClearBombAdjacent(int column, int row, bool clearItself = false)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if ((column + i) >= 0 && (column + i) < width && (row + j) >= 0 && (row + j) < height && !(i == 0 && j == 0))
                {
                    GamePiece piece = pieces[column + i, row + j];
                    if (piece.IsClearable())
                        ClearPiece(column + i, row + j, false, PieceType.BOMB);
                }
                else if (i == 0 && j == 0 && clearItself)
                {
                    ClearPiece(column, row, false, PieceType.BOMB);
                }
            }
        }

        //Add Last Pieces
        int a = column - 2;
        if (a >= 0 && a < width)
        {
            GamePiece piece = pieces[a, row];
            if (piece.IsClearable())
                ClearPiece(a, row, false, PieceType.BOMB);
        }

        int b = column + 2;
        if (b >= 0 && b < width)
        {
            GamePiece piece = pieces[b, row];
            if (piece.IsClearable())
                ClearPiece(b, row, false, PieceType.BOMB);
        }

        int k = row - 2;
        if (k >= 0 && k < height)
        {
            GamePiece piece = pieces[column, k];
            if (piece.IsClearable())
                ClearPiece(column, k, false, PieceType.BOMB);
        }

        int l = row + 2;
        if (l >= 0 && l < height)
        {
            GamePiece piece = pieces[column, l];
            if (piece.IsClearable())
                ClearPiece(column, l, false, PieceType.BOMB);
        }
    }

    #endregion

    #region Deadlocks

    private bool IsDeadlocked()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GamePiece piece = pieces[x, y];

                //////// Check Right
                if (x < width - 1)
                {
                    GamePiece pieceRight = pieces[x + 1, y];
                    if (piece.IsMovable() && pieceRight.IsMovable())
                    {
                        pieces[piece.X, piece.Y] = pieceRight;
                        pieces[pieceRight.X, pieceRight.Y] = piece;

                        if (GetMatch(piece, pieceRight.X, pieceRight.Y) != null || GetMatch(pieceRight, piece.X, piece.Y) != null)
                        {
                            pieces[piece.X, piece.Y] = piece;
                            pieces[pieceRight.X, pieceRight.Y] = pieceRight;
                            return false;
                        }
                        pieces[piece.X, piece.Y] = piece;
                        pieces[pieceRight.X, pieceRight.Y] = pieceRight;
                    }
                }

                //////// Check Up
                if (y > 0)
                {
                    GamePiece pieceUp = pieces[x, y - 1];
                    if (piece.IsMovable() && pieceUp.IsMovable())
                    {
                        pieces[piece.X, piece.Y] = pieceUp;
                        pieces[pieceUp.X, pieceUp.Y] = piece;

                        if (GetMatch(piece, pieceUp.X, pieceUp.Y) != null || GetMatch(pieceUp, piece.X, piece.Y) != null)
                        {
                            pieces[piece.X, piece.Y] = piece;
                            pieces[pieceUp.X, pieceUp.Y] = pieceUp;
                            return false;
                        }
                        pieces[piece.X, piece.Y] = piece;
                        pieces[pieceUp.X, pieceUp.Y] = pieceUp;
                    }
                }
            }
        }

        return true;
    }

    public void ShuffleBoard()
    {
        List<GamePiece> allPieces = new();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (pieces[x, y] != null)
                    allPieces.Add(pieces[x, y]);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int pieceToUse = randomGenerator.Next(0, allPieces.Count);

                int maxIterations = 0;
                while (GetMatch(allPieces[pieceToUse], x, y) != null && maxIterations < 100)
                {
                    pieceToUse = randomGenerator.Next(0, allPieces.Count);
                    maxIterations++;
                }

                GamePiece piece = allPieces[pieceToUse];
                piece.MovableComponent.MovePiece(x, y, true);
                pieces[x, y] = allPieces[pieceToUse];
                allPieces.Remove(allPieces[pieceToUse]);
            }
        }

        if (IsDeadlocked())
            ShuffleBoard();
        else if (!IsAnyCandyMatching())
            Server_GridFilled("Shuffle");
    }

    #endregion

    #region Helper Functions

    public void ClearGridData()
    {
        var allCandies = GameObject.FindGameObjectsWithTag("Candy");
        foreach (GameObject item in allCandies)
            Destroy(item);

        var allBoosters = GameObject.FindGameObjectsWithTag("Booster");
        foreach (GameObject item in allBoosters)
            Destroy(item);

        var allBGTiles = GameObject.FindGameObjectsWithTag("BG Tile");
        foreach (GameObject item in allBGTiles)
            Destroy(item);

        var allEmptySpace = GameObject.FindGameObjectsWithTag("Empty Space");
        foreach (GameObject item in allEmptySpace)
            Destroy(item);

        //Clear List
        candyContainer.ClearData();

        pieces = null;
        isFilling = false;
        gridFilled = false;
        pressedPiece = null;
        releasedPiece = null;
        firstTouchPosition = Vector2.zero;
        finalTouchPosition = Vector2.zero;
        swapDir = SwapDirection.Null;
        beingDestroyedByBooster = false;
        piecePrefabsDict.Clear();
        swipedCandyPath.Clear();
    }

    [Client]
    public string Client_GetGridDataInString()
    {
        // First Digit will tell the piece type
        // Second digit will tell the color
        // Seperated by "."

        string gridData = "";

        try
        {

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (pieces[x, y] == null)
                    {
                        Debug.LogError("Piece Is Null : " + new Vector2Int(x, y).ToString());
                        continue;
                    }

                    GamePiece piece = pieces[x, y];
                    string pieceType = ((int)piece.Type).ToString();

                    //If this is player 2, then switch red and blue colors, because Server's grid is like player 1's grid

                    if (piece.IsColored())
                    {
                        ColorType color = piece.ColorComponent.Color;

                        switch (gamePlayManager.matchtype)
                        {
                            case MatchType.TwoPlayer:
                                if (!IsPlayer1)
                                {
                                    if (color == ColorType.Red)
                                        color = ColorType.Blue;
                                    else if (color == ColorType.Blue)
                                        color = ColorType.Red;
                                }
                                break;

                            case MatchType.FourPlayer:
                                if (!IsPlayer1 && !IsPlayer1Partner)
                                {
                                    if (color == ColorType.Red)
                                        color = ColorType.Blue;
                                    else if (color == ColorType.Blue)
                                        color = ColorType.Red;
                                }
                                break;
                        }

                        string colorType = ((int)color).ToString();

                        string pieceData = pieceType + colorType;

                        if (x == 0 && y == 0)
                            gridData += pieceData;
                        else
                            gridData += "." + pieceData;
                    }
                    else
                    {
                        Debug.LogError("Piece Is Not Colored, Piece Type : " + pieces[x, y].Type.ToString());
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Grid Error: Error while generating Grid Data string in Client : Error Message : " + e.Message + "\n Error Stack Trace : " + e.StackTrace);
        }

        return gridData;
    }

    [Server]
    public bool Server_GetGridDataInString(out string data)
    {
        //Debug.LogWarning("Getting GRID Data....................................................................................................................");
        // First Digit will tell the piece type
        // Second digit will tell the color
        // Seperated by "."

        string gridData = "";

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pieces[x, y] != null)
                {
                    GamePiece piece = pieces[x, y];

                    if (piece.Type == PieceType.EMPTY)
                    {
                        Debug.LogError("Piece Type Is Empty, Cant Send Grid Data");
                        data = "";
                        return false;
                    }

                    string pieceType = ((int)piece.Type).ToString();

                    //If this is player 2, then switch red and blue colors, because Server's grid is like player 1's grid

                    if (piece.IsColored())
                    {
                        ColorType color = piece.ColorComponent.Color;
                        string colorType = ((int)color).ToString();

                        string pieceData = pieceType + colorType;

                        if (x == 0 && y == 0)
                            gridData += pieceData;
                        else
                            gridData += "." + pieceData;
                    }
                    else
                    {
                        print("Grid Error: Piece Not Colored");
                        data = "";
                        return false;
                    }
                }
                else
                {
                    print("Grid Error: Piece is null");
                    data = "";
                    return false;
                }
            }
        }

        if (gridData.Length == 146)
        {
            data = gridData;
            return true;
        }
        else
        {
            print("Grid Error: Length of grid is " + gridData.Length);
            data = "";
            return false;
        }
    }

    /// <summary>
    /// Parameter should be the Grid Data of server, Server's grid is like player 1's grid
    /// Also always set seed while setting grid data
    /// </summary>
    /// <param name="serverGridData"></param>
    IEnumerator clientSetGridDataCoroutine;

    public void Client_SetGridDataAndSetSeed(int seed, string serverGridData, Action callback = null, bool setSeed = true)
    {
        if (clientSetGridDataCoroutine != null)
        {
            print("Old Client_SetGridData coroutine was running, its stopped...");
            StopCoroutine(clientSetGridDataCoroutine);
        }

        clientSetGridDataCoroutine = WaitWhileAnyGemIsMovingOrClearingOrAnyBoosterOrPerkWorking(() =>
        {
            if (setSeed)
                SetSeed(seed);

            string[] piecesData = serverGridData.Split(".");

            if (piecesData.Length == (width * height))
            {
                int index = 0;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        string pieceData = piecesData[index];
                        if (pieceData.Length != 2)
                        {
                            Debug.LogError("Length Of Piece Data should be 2, but it is " + pieceData.Length);
                            callback?.Invoke();
                            return;
                        }

                        string pieceTypeStr = pieceData.Substring(0, 1);
                        string colorTypeStr = pieceData.Substring(1, 1);

                        PieceType pieceType = (PieceType)int.Parse(pieceTypeStr);
                        ColorType colorType = (ColorType)int.Parse(colorTypeStr);

                        switch (gamePlayManager.matchtype)
                        {
                            case MatchType.TwoPlayer:
                                if (!IsPlayer1)
                                {
                                    if (colorType == ColorType.Red)
                                        colorType = ColorType.Blue;
                                    else if (colorType == ColorType.Blue)
                                        colorType = ColorType.Red;
                                }
                                break;

                            case MatchType.FourPlayer:
                                if (!IsPlayer1 && !IsPlayer1Partner)
                                {
                                    if (colorType == ColorType.Red)
                                        colorType = ColorType.Blue;
                                    else if (colorType == ColorType.Blue)
                                        colorType = ColorType.Red;
                                }
                                break;
                        }

                        if (pieces[x, y] != null)
                            Destroy(pieces[x, y].gameObject);

                        SpawnNewPiece(x, y, pieceType).ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(colorType);
                        index++;
                    }
                }
            }
            else
            {
                Debug.LogError("Grid Data is invalid. Number of pieces in grid data is different than actual pieces on grid");
            }

            callback?.Invoke();
            clientSetGridDataCoroutine = null;
        });

        StartCoroutine(clientSetGridDataCoroutine);
    }

    //Is Booster Working only works well in server
    public bool IsBoosterActive()
    {
        bool result = false;

        if (GameObject.FindGameObjectsWithTag("Booster").Length > 0)
        {
            result = true;
            foreach (var item in GameObject.FindGameObjectsWithTag("Booster"))
                if (item.TryGetComponent(out Booster_LilDragon dragonBooster) || (item.TryGetComponent(out Booster_MystryHat mystryHat) && !BoosterManager.manager.isBoosterWorking)) // Little Dragon Booster Requires Swiping, Mystry Hat requires swiping
                    result = false;
        }

        return result;
    }

    private bool IsLilDragonBoosterActive()
    {
        if (GameObject.FindGameObjectsWithTag("Booster").Length > 0)
        {
            foreach (var item in GameObject.FindGameObjectsWithTag("Booster"))
            {
                if (item.TryGetComponent(out Booster_LilDragon _))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsPathwayBoosterActive()
    {
        if (GameObject.FindGameObjectsWithTag("Booster").Length > 0)
            foreach (var item in GameObject.FindGameObjectsWithTag("Booster"))
                if (item.TryGetComponent(out Booster_Pathway _))
                    return true;

        return false;
    }

    public bool IsAnyBoosterWorking()
    {
        return IsBoosterActive() && !IsLilDragonBoosterActive();
    }

    public bool Perk_IsAnyBoosterActive()
    {
        bool result = false;

        if (GameObject.FindGameObjectsWithTag("Booster").Length > 0)
        {
            result = true;
            foreach (var item in GameObject.FindGameObjectsWithTag("Booster"))
                if (item.TryGetComponent(out Booster_MystryHat mystryHat) && !BoosterManager.manager.isBoosterWorking) // Perks can still be used with MystryHat booster, after MystryHat's candies are spawned, isBoosterWorking is set to false
                    result = false;
        }

        return result;
    }

    public bool IsAnyGemMoving()
    {
        foreach (var item in candyContainer.movablePieces)
        {
            if (item != null)
            {
                if (item.moving)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Getiing candy from container
    /// </summary>
    /// <returns></returns>
    public bool IsAnyGemClearing()
    {
        /// Getiing clearablePieces from container
        foreach (var item in candyContainer.clearablePieces)
        {
            if (item != null)
            {
                if (item.IsBeingCleared)
                    return true;
            }
        }
        return false;
    }

    public bool IsAnyGemMovingOrClearing()
    {
        /// Getiing movablePieces from container
        foreach (var item in candyContainer.movablePieces)
        {
            if (item != null)
            {
                if (item.moving)
                    return true;
            }
        }

        /// Getiing clearablePieces from container
        foreach (var item in candyContainer.clearablePieces)
        {
            if (item != null)
            {
                if (item.IsBeingCleared)
                    return true;
            }
        }
        return false;
    }

    public int GenerateRandom(int min, int max)
    {
        if (randomGenerator == null) Debug.LogError("RANDOM GENERATOR IS NULL");

        return randomGenerator.Next(min, max);
    }

    public void SetSeed(int seed)
    {
        StartCoroutine(WaitWhileAnyGemIsMovingOrClearing(() => { seeds = seed; /*print("Generator Init : Seed Set to " + seeds)*/; randomGenerator = new(seed); }));
    }

    IEnumerator WaitWhileAnyGemIsMovingOrClearing(Action callback)
    {
        yield return new WaitWhile(IsAnyGemMovingOrClearing);
        callback();
    }

    IEnumerator WaitWhileAnyGemIsMovingOrClearingOrAnyBoosterOrPerkWorking(Action callback)
    {
        yield return new WaitWhile(IsAnyGemMovingOrClearing);
        yield return new WaitWhile(() => BoosterManager.manager.isBoosterWorking);
        yield return new WaitWhile(() => PerksManager.manager.isPerkWorking);
        yield return new WaitWhile(() => isFilling);
        callback();
    }

    private bool GridData_IsAnyBoosterWorking()
    {
        bool result = false;

        var boosters = GameObject.FindGameObjectsWithTag("Booster");

        if (boosters.Length > 0)
        {
            result = true;
            foreach (var item in boosters)
                if (item.TryGetComponent(out Booster_MystryHat mystryHat) && !BoosterManager.manager.isBoosterWorking) // Little Dragon Booster Requires Swiping, Mystry Hat requires swiping
                    result = false;
        }

        return result;
    }

    #endregion

    #region Syncing

    [ServerCallback]
    public void Server_GridFilled(string caller)
    {
        //print("------Caller : " + caller);
        if (gridFilled)
        {
            if (sendGridDataRPCCoroutine != null)
                StopCoroutine(sendGridDataRPCCoroutine);

            sendGridDataRPCCoroutine = Coroutine_SendGridDataAfterGridFilled(caller);
            StartCoroutine(sendGridDataRPCCoroutine);
        }
        else
            Rpc_MakeGridSynced("grid not filled");
    }

    IEnumerator Coroutine_SendGridDataAfterGridFilled(string caller)
    {
        if (IsAnyGemMovingOrClearing())
            yield return new WaitWhile(IsAnyGemMovingOrClearing);

        if (GridData_IsAnyBoosterWorking())
            yield return new WaitWhile(GridData_IsAnyBoosterWorking);

        if (PerksManager.manager.isPerkWorking)
            yield return new WaitWhile(() => PerksManager.manager.isPerkWorking);

        if (IsAnyGemMovingOrClearing())
            yield return new WaitWhile(IsAnyGemMovingOrClearing);

        int seed = UnityEngine.Random.Range(0, 99999);

        if (Server_GetGridDataInString(out string serverGridData))
        {
            SetSeed(seed);
            Rpc_SetGrid_GridFilled(caller, serverGridData, seed);
        }
        else
        {
            //print("Server_GetGridDataInString Failed, so not sending back grid data");
            Rpc_MakeGridSynced("failed Server_GetGridDataInString");
        }

        sendGridDataRPCCoroutine = null;
    }

    [ClientRpc]
    private void Rpc_SetGrid_GridFilled(string caller, string gridData, int seed)
    {
        //print("Grid " + caller + " : Grid Data Recieved From Server\n" + gridData);
        StartCoroutine(WaitWhileAnyGemIsMovingOrClearingOrAnyBoosterOrPerkWorking(() =>
        {
            string localGridData = Client_GetGridDataInString();
            SetSeed(seed);

            if (localGridData != gridData)
                Client_SetGridDataAndSetSeed(seed, gridData, () => isGridSynced = true, false);
            else
                isGridSynced = true;
        }));
    }

    [ClientRpc]
    private void Rpc_MakeGridSynced(string caller)
    {
        print("GRID DATA FAILED TO RECIEVE FROM SERVER : " + caller);
        isGridSynced = true;
    }

    #endregion
}

#region Helper Enums And Structs

public enum PieceType : int
{
    EMPTY,
    NORMAL,
    ROW_CLEAR,
    COLUMN_CLEAR,
    ELECTRIC,
    BOMB,
    MUSHROOM
}

[Serializable]
public struct PiecePrefab
{
    public PieceType type;
    public GameObject prefab;
}

public enum SwapDirection
{
    Left,
    Right,
    Up,
    Down,
    Null
}

public enum GameModeType
{
    Normal,
    Mushroom_Mode
}

#endregion