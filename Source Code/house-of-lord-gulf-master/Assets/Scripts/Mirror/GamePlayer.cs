using System.Collections.Generic;
using System.Collections;
using Mirror;
using UnityEngine;
using PlayFab.MultiplayerModels;
using PlayFab;

public class GamePlayer : NetworkBehaviour
{
    [SyncVar] internal string playfabID;
    [SyncVar] internal string boosterID;
    [SyncVar] internal string perk1ID;
    [SyncVar] internal string perk2ID;
    [SyncVar] internal bool isPlayer1;
    [SyncVar] internal bool isReconnecting = false;
    [SyncVar] internal string teamName;
    [SyncVar] internal bool team1p1;
    [SyncVar] internal bool team1p2;
    [SyncVar] internal bool team2p1;
    [SyncVar] internal bool team2p2;

    int randomLeaderboardPlayerGender; //Enum Gender is of base type int
    int randomLeaderboardPlayerIndex;

    #region Override Functions

    public override void OnStartClient()
    {
        if (isPlayer1 && isLocalPlayer)
        {
            // Player 1 sets these variables
            Vector2Int leaderboardData = LeaderboardsUIManager.manager.GetRandomLeaderboardPlayer();
            randomLeaderboardPlayerGender = leaderboardData.x;
            randomLeaderboardPlayerIndex = leaderboardData.y;

            MatchMakingUIManager.manager.OnLeaderboardDataRecieved(randomLeaderboardPlayerGender, randomLeaderboardPlayerIndex);
            StartCoroutine(WaitWhileGamePlayUIManagerIsNull());

            Cmd_SetRandomLeaderboardData(randomLeaderboardPlayerGender, randomLeaderboardPlayerIndex);
        }

        switch (MatchMakingManager.manager.GetMatchType())
        {
            case MatchType.TwoPlayer:
                Debug.Log("Two Player");

                if (GameObject.FindGameObjectsWithTag("Player").Length == 2)
                {
                    MatchMakingUIManager.manager.OnAllPlayersSpawned();
                }
                break;
            case MatchType.FourPlayer:

                //Debug.Log("Four Player");
                if (GameObject.FindGameObjectsWithTag("Player").Length == 4)
                {
                    MatchMakingUIManager.manager.OnAllPlayersSpawned();
                }
                break;
        }

    }

    public override void OnStartServer()
    {
        Debug.Log("OnStartServer");

        if (isReconnecting)
        {
            Grid.grid.Server_GridFilled("game player");
            BoosterManager.manager.Server_Reconnection_DestroyAllBoosters(isPlayer1);
            PerksManager.manager.Server_Reconnection_DestroyAllPerks(isPlayer1);
        }

        if (!isPlayer1)
        {
            Debug.Log("I am not player 1 so i am sending data to first player");
            Rpc_Player2_SetRandomLeaderboardData(GameNetworkManager.manager.randomLeaderboardPlayerGender, GameNetworkManager.manager.randomLeaderboardPlayerIndex);
        }
        else
        {
            Debug.Log("I am Player One");
        }
    }

    #endregion

    #region Public Functions

    IEnumerator WaitWhileGamePlayUIManagerIsNull()
    {
        yield return new WaitWhile(() => GameplayUIManager.manager == null);
        GameplayUIManager.manager.InitializeRandomPlayerFromTop20Player((Gender)randomLeaderboardPlayerGender, randomLeaderboardPlayerIndex);
    }

    [Client]
    public void Grid_OnCandyClicked(GamePiece piece)
    {
        Cmd_ClickCandy(piece.X, piece.Y);
    }

    [Client]
    public void Grid_OnCandiesPathMade(List<GamePiece> pieces)
    {
        List<Vector2Int> positions = new();
        foreach (var item in pieces)
            positions.Add(new(item.X, item.Y));

        Cmd_CandiesPathMade(positions.ToArray());
    }

    [Client]
    public void Grid_MovePieces(float swipeAngle, GamePiece piece)
    {
        //Debug.Log("piecename   " + piece.gameObject.name);
        if (Grid.grid.isGridSynced)
            Cmd_MovePieces(swipeAngle, piece.X, piece.Y);
    }

    [Client]
    public void UseBooster()
    {
        if (!Grid.grid.isFilling && Grid.grid.gridFilled && !BoosterManager.manager.isBoosterWorking && !BoosterManager.manager.IsAnyBoosterActive() && !Grid.grid.IsAnyGemMovingOrClearing())
            Cmd_UseBooster();
    }

    [Client]
    public void UseBooster(string playfabID)
    {
        if (!Grid.grid.isFilling && Grid.grid.gridFilled && !BoosterManager.manager.isBoosterWorking && !BoosterManager.manager.IsAnyBoosterActive() && !Grid.grid.IsAnyGemMovingOrClearing())
            Cmd_UseBooster(playfabID);
    }

    [Client]
    public void UsePerk(bool isPerk1)
    {
        Cmd_UsePerk(isPerk1);
    }

    [Client]
    public void LeaveGame()
    {
        Debug.Log("I leave game  " + playfabID + "    " + teamName);
        Cmd_LeaveGame(playfabID, teamName);
    }

    #endregion

    #region Commands

    #region Booster

    [Command]
    private void Cmd_UseBooster()
    {
        BoosterManager.manager.Server_UseBooster(isPlayer1);
    }

    [Command]
    private void Cmd_UseBooster(string playfabID)
    {
        BoosterManager.manager.Server_UseBooster(isPlayer1, playfabID);
    }

    #endregion

    #region Perks

    [Command]
    private void Cmd_UsePerk(bool isPerk1)
    {
        if (isPlayer1 == GamePlayManager.manager.Server_IsPlayer1Turn())
        {
            PerksManager.manager.Server_UsePerk(isPlayer1, isPerk1);
        }
    }

    #endregion

    #region Grid

    [Command]
    private void Cmd_ClickCandy(int x, int y)
    {
        if (GamePlayManager.manager.isGameOver) return;

        GamePiece piece = Grid.grid.pieces[x, y];
        if (GamePlayManager.manager.Server_IsPlayer1Turn() == isPlayer1 && piece != null)
        {
            Grid.grid.Invoke_CandyClickedEvent(piece);
            Rpc_ClickCandy(x, y);
        }
    }

    [Command]
    private void Cmd_CandiesPathMade(Vector2Int[] positions)
    {
        if (GamePlayManager.manager.isGameOver) return;
        if (GamePlayManager.manager.Server_IsPlayer1Turn() == isPlayer1 && positions != null && positions.Length > 0)
        {
            Grid.grid.Invoke_CandyPathMadeEvent(positions);
            Rpc_CandyPathMade(positions);
        }
    }

    [Command]
    private void Cmd_MovePieces(float swipeAngle, int pressedPieceX, int pressedPieceY)
    {
        if (GamePlayManager.manager.isGameOver) return;
        if (Grid.grid.IsBoosterActive()) return;


//        MatchType mt = new MatchType();

//#if !UNITY_SERVER
//        mt = GamePlayManager.manager.LocalmatchType;
//#else
//        mt = GamePlayManager.manager.matchtype;
//#endif

        switch (GamePlayManager.manager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (!(GamePlayManager.manager.Server_IsPlayer1Turn() == isPlayer1) || !GamePlayManager.manager.Server_AnyMovesLeft() || Grid.grid.IsAnyGemMovingOrClearing() || Grid.grid.isSwappingPiece || !Grid.grid.gridFilled || !TurnTimer.timer.CanSwap(isPlayer1))
                {
                    Debug.Log("Returned From Here :" + playfabID);
                    return;
                }
                break;
            case MatchType.FourPlayer:

                Debug.Log(" isPlayer1 " + isPlayer1 + " player1partner   " + team1p1);
                if (!GamePlayManager.manager.Server_IsMyTurn(playfabID) || !GamePlayManager.manager.Server_AnyMovesLeft() || Grid.grid.IsAnyGemMovingOrClearing() || Grid.grid.isSwappingPiece || !Grid.grid.gridFilled || !TurnTimer.timer.CanSwap(isPlayer1))
                {
                    Debug.Log("Returned From Here :" + playfabID);
                    Debug.Log("Grid.grid.IsAnyGemMovingOrClearing() :" + Grid.grid.IsAnyGemMovingOrClearing());
                    Debug.Log("Grid.grid.isSwappingPiece :" + Grid.grid.isSwappingPiece);
                    Debug.Log("Grid.grid.gridFilled :" + Grid.grid.gridFilled);
                    Debug.Log("Server_IsMyTurn :" + GamePlayManager.manager.Server_IsMyTurn(playfabID));
                    Debug.Log("Server_AnyMovesLeft :" +GamePlayManager.manager.Server_AnyMovesLeft());
                    Debug.Log("CanSwap :" + TurnTimer.timer.CanSwap(isPlayer1));
                    return;
                }
                break;
        }

        Debug.Log("This is running :" + playfabID);
        if (Grid.grid.Server_GetGridDataInString(out string gridData))
        {
            int seed = Random.Range(0, 99999);

            Grid.grid.SetSeed(seed);

            int x = pressedPieceX;
            int y = pressedPieceY;

            GamePiece adjacentPiece = null;

            if (swipeAngle > -45 && swipeAngle <= 45 && x < Grid.width - 1)
            {
                //Right Swipe
                adjacentPiece = Grid.grid.pieces[x + 1, y];
                Grid.grid.swapDir = SwapDirection.Right;
            }
            else if (swipeAngle > 45 && swipeAngle <= 135 && y >= 1)
            {
                //Up Swipe
                adjacentPiece = Grid.grid.pieces[x, y - 1];
                Grid.grid.swapDir = SwapDirection.Up;
            }
            else if ((swipeAngle > 135 || swipeAngle <= -135) && x >= 1)
            {
                //Left Swipe
                adjacentPiece = Grid.grid.pieces[x - 1, y];
                Grid.grid.swapDir = SwapDirection.Left;
            }
            else if ((swipeAngle < -45 || swipeAngle >= -135) && y < Grid.height - 1)
            {
                //Down Swipe
                adjacentPiece = Grid.grid.pieces[x, y + 1];
                Grid.grid.swapDir = SwapDirection.Down;
            }

            if (adjacentPiece != null)
            {
                if ((pressedPieceX == adjacentPiece.X && Mathf.Abs(pressedPieceY - adjacentPiece.Y) == 1) || (pressedPieceY == adjacentPiece.Y && Mathf.Abs(pressedPieceX - adjacentPiece.X) == 1))
                {
                    Grid.grid.pressedPiece = Grid.grid.pieces[x, y];
                    Grid.grid.releasedPiece = adjacentPiece;
                    Grid.grid.SwapPieces(Grid.grid.pressedPiece, Grid.grid.releasedPiece);
                    Rpc_MovePieces(gridData, seed, (int)Grid.grid.swapDir, new Vector2Int(Grid.grid.pressedPiece.X, Grid.grid.pressedPiece.Y), new Vector2Int(Grid.grid.releasedPiece.X, Grid.grid.releasedPiece.Y));
                }
            }
        }
    }

    #endregion

    #region Leaving Game

    [Command]
    private void Cmd_LeaveGame(string playfabID, string teamName)
    {
        StartCoroutine(StopServerAfterSendingRPC(playfabID, teamName));
    }

    IEnumerator StopServerAfterSendingRPC(string playfabID, string teamName)
    {
        Rpc_OtherUserLeftGame(playfabID, teamName);
        yield return new WaitForSeconds(350);
        Debug.Log("Server Quit After Game Player Leave Game");
        GameNetworkManager.manager.StopServer();
        Application.Quit();
    }

    #endregion

    #endregion

    #region RPCs

    #region Grid

    [ClientRpc]
    private void Rpc_ClickCandy(int x, int y)
    {
        GamePiece piece = Grid.grid.pieces[x, y];
        Grid.grid.Invoke_CandyClickedEvent(piece);
    }

    [ClientRpc]
    private void Rpc_CandyPathMade(Vector2Int[] positions)
    {
        Grid.grid.Invoke_CandyPathMadeEvent(positions);
    }

    [ClientRpc]
    private void Rpc_MovePieces(string gridData, int seed, int swapDir, Vector2Int pressedPiecePos, Vector2Int releasedPiecePos)
    {
        string clientGridData = Grid.grid.Client_GetGridDataInString();

        if (clientGridData != gridData)
        {
            Grid.grid.Client_SetGridDataAndSetSeed(seed, gridData, () =>
            {
                Grid.grid.swapDir = (SwapDirection)swapDir;
                Grid.grid.pressedPiece = Grid.grid.pieces[pressedPiecePos.x, pressedPiecePos.y];
                Grid.grid.releasedPiece = Grid.grid.pieces[releasedPiecePos.x, releasedPiecePos.y];
                Grid.grid.SwapPieces(Grid.grid.pressedPiece, Grid.grid.releasedPiece);
            });
            return;
        }

        Grid.grid.SetSeed(seed);
        Grid.grid.swapDir = (SwapDirection)swapDir;
        Grid.grid.pressedPiece = Grid.grid.pieces[pressedPiecePos.x, pressedPiecePos.y];
        Grid.grid.releasedPiece = Grid.grid.pieces[releasedPiecePos.x, releasedPiecePos.y];
        Grid.grid.SwapPieces(Grid.grid.pressedPiece, Grid.grid.releasedPiece);
    }

    #endregion

    #region Leaving Game

    // This will not be sent to the owner, that means it will reach to other user only
    [ClientRpc]
    private void Rpc_OtherUserLeftGame(string playfabIDOfUserLeft ,string teamNameOfUserLeft)
    {
        Debug.Log("I leave game  " + playfabIDOfUserLeft + "    " + teamNameOfUserLeft);

        switch (GamePlayManager.manager.matchtype)
        {
            case MatchType.TwoPlayer:
                if (playfabIDOfUserLeft == PlayerData.PlayfabID)
                    GamePlayManager.manager.Client_GameOver(false, true);
                else
                    GamePlayManager.manager.Client_GameOver(true);
                break;

            case MatchType.FourPlayer:

                //FIND OPPONENTS
                TeamType t = TeamType.TeamA;

                if (teamNameOfUserLeft == "team1")
                {
                    t = TeamType.TeamA;
                }
                else
                {
                    t = TeamType.TeamB;
                }

                if (playfabIDOfUserLeft == PlayerData.PlayfabID)
                {
                    Debug.Log("My game Over from here");
                    GamePlayManager.manager.Client_GameOver(false, true);
                }
                else if (t==GamePlayManager.manager.serverteamtype)
                {
                    Debug.Log("My game Over from here i am teammate");
                    GamePlayManager.manager.Client_GameOver(false, true);
                }
                else
                {
                    Debug.Log("My game Over from here i am opponent");
                    GamePlayManager.manager.Client_GameOver(true,false);
                }
                break;
        }
    }

    #endregion

    #endregion

    #region Sync Random Leaderboard Player

    [Command]
    private void Cmd_SetRandomLeaderboardData(int randomLeaderboardPlayerGender, int randomLeaderboardPlayerIndex)
    {
        GameNetworkManager.manager.randomLeaderboardPlayerGender = randomLeaderboardPlayerGender;
        GameNetworkManager.manager.randomLeaderboardPlayerIndex = randomLeaderboardPlayerIndex;
    }

    [ClientRpc]
    private void Rpc_Player2_SetRandomLeaderboardData(int randomLeaderboardPlayerGender, int randomLeaderboardPlayerIndex)
    {
        if (!isPlayer1)
        {
            MatchMakingUIManager.manager.OnLeaderboardDataRecieved(randomLeaderboardPlayerGender, randomLeaderboardPlayerIndex);
            GameplayUIManager.manager.InitializeRandomPlayerFromTop20Player((Gender)randomLeaderboardPlayerGender, randomLeaderboardPlayerIndex);
        }
    }

    #endregion
}
