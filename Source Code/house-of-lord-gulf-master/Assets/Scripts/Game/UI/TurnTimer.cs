using UnityEngine;
using System;
using Mirror;

public class TurnTimer : NetworkBehaviour
{
    public static TurnTimer timer;

    [SerializeField, Range(10,120)] int turnTimer = 20;

    [SerializeField, Range(0.1f, 1f)] float gracePeriodAfterGridIsFilled = 0.5f;
    [SerializeField, Range(0.1f, 2f)] float gracePeriodAfterTurnChanged = 2f;
    [SerializeField, Range(0.1f, 2f)] float gracePeriodAfterRoundChanged = 1f;
    [SerializeField, Range(1f, 5f)] float timerSmoothSpeed = 5;

    //Timer runs out 2 seconds earlier for local players
    private int localTurnTimer;

    //Synced every 1 second
    [SyncVar] internal double timerSecond;

    float gracePeriodTimerGridFilled;
    float gracePeriodTimerTurnChanged;
    float gracePeriodTimerRoundChanged;
    bool resetted = false;
    bool timerPaused = false;
    [Space]
    public GamePlayManager gamePlayManager;
    public GameplayUIManager gamePlayUIManager;

    private Grid grid;

    #region Unity Functions

    private void Awake()
    {
        timer = this;
        grid=FindObjectOfType<Grid>();
    }

    private void Start()
    {
        gamePlayManager.OnWonGame += () => timerSecond = 0;
        gamePlayManager.OnLostGame += () => timerSecond = 0;
        gamePlayManager.OnTurnChanged += x=> ResetTimer();
        gamePlayManager.OnRoundChanged+= (x) => gracePeriodTimerRoundChanged = gracePeriodAfterRoundChanged;

        if (isServerOnly)
        {
            Debug.Log("Localtimer filler  " + localTurnTimer + "     " + turnTimer);
            localTurnTimer = turnTimer;
        }
    }

    private void Update()
    {
        if (!isServer)
        {
            if(gamePlayManager.Client_IsMyTurn())
                localTurnTimer = turnTimer - 2;
            else
                localTurnTimer = turnTimer;
        }

        RunTimer();
    }

    #endregion

    #region Timer Functions

    private void RunTimer()
    {
        if (gamePlayManager.isGameOver) return;

        if (resetted)
        {
            if (gamePlayUIManager.SetTimerSliderValue(0, localTurnTimer, timerSmoothSpeed))
                resetted = false;

            timerPaused = true;
            return;
        }

        if (BoosterManager.manager.isBoosterWorking || (grid.isSwappingPiece && !grid.swappingBack) || grid.IsAnyGemClearing()) return;

        #region Grace Period Timer

        #region Grid Filling Grace Time

        if (grid.isFilling)
        {
            gracePeriodTimerGridFilled = gracePeriodAfterGridIsFilled;
            timerPaused = true;
            return;
        }

        if (!grid.isFilling && gracePeriodTimerGridFilled > 0)
        {
            gracePeriodTimerGridFilled -= Time.deltaTime;
            timerPaused = true;
            return;
        }

        #endregion

        #region Turn Changed Grace Time

        if (gracePeriodTimerTurnChanged > 0)
        {
            gracePeriodTimerTurnChanged -= Time.deltaTime;
            timerPaused = true;
            return;
        }

        #endregion

        #region Turn Changed Grace Time

        if (gracePeriodTimerRoundChanged > 0)
        {
            gracePeriodTimerRoundChanged -= Time.deltaTime;
            timerPaused = true;
            return;
        }

        #endregion

        #endregion

        timerSecond += Time.deltaTime;
        gamePlayUIManager.SetTimerSliderValue(timerSecond, localTurnTimer, timerSmoothSpeed);

        timerPaused = false;

        if (timerSecond >= localTurnTimer)
        {
            //Debug.Log("Time Complete");
            if(GameNetworkManager.manager.mode == NetworkManagerMode.ServerOnly)
            {
                Debug.Log("Only Called On Server");
                gamePlayManager.Server_OnTimerCompleted();
            }
        }
    }

    public void ResetTimer()
    {
        //Debug.Log("Reset Timer");
        timerSecond = 0;
        gracePeriodTimerTurnChanged = gracePeriodAfterTurnChanged;
        resetted = true;
    }

    public bool CanSwap(bool isPlayer1)
    {
        int timer = turnTimer;

        if (isPlayer1 == gamePlayManager.Server_IsPlayer1Turn())
            timer -= 2;

        return timerSecond < timer && !timerPaused;
    }

    #endregion
}
