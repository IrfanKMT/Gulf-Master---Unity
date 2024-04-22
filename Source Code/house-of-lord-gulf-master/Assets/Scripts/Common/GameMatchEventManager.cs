// This script handles won and lost match events
//Functions are called from GamePlayManager script

using System;

public class GameMatchEventManager
{
    public static event Action OnStartGame;
    public static event Action OnWonGame;
    public static event Action OnLostGame;

    public static void GamePlayManager_GameWon()
    {
        OnWonGame?.Invoke();
    }

    public static void GamePlayManager_GameLost()
    {
        OnLostGame?.Invoke();
    }

    public static void GamePlayManager_GameStarted()
    {
        OnStartGame?.Invoke();
    }
}
