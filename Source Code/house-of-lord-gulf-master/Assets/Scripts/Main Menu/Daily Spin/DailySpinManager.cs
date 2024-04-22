using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using System;

public class DailySpinManager : MonoBehaviour
{
    [Header("Spinning")]
    [SerializeField] Transform spinWheel;
    [SerializeField] List<SpinRewards> rewards;
    [SerializeField, Range(1, 50f)] float rotationSpeed;
    [SerializeField] int minStopSpinningSeconds = 2;
    [SerializeField] int maxStopSpinningSeconds = 5;
    [SerializeField] Button spinButton;
    [SerializeField] float timeIntervalInSeconds;

    [Header("Reward Granted Panel")]
    [SerializeField] GameObject rewardGrantedPanel;
    [SerializeField] TMP_Text rewardGrantedPanelText;
    [SerializeField] Button duplicateRewardBtn;
    [SerializeField] TMP_Text timeRemainingText;

    DateTime timerStartTime;
    bool loaded = false;
    bool spinning = false;
    private bool timeCalculator;
    private TimeSpan timeRemaining;

    #region Unity Functions

    private void Start() => AuthenticationManager.manager.OnPlayerLoggedIn += LoadTimerDateTime;

    private void Update()
    {
        if (!loaded) return;
        if (!timeCalculator) return;
        timeRemaining = timerStartTime - DateTime.Now;
        //Debug.Log(timeRemaining);
        if (timeRemaining.TotalMilliseconds <= 0)
        {
            timeRemainingText.text = "Spin";
            spinButton.interactable = !spinning;
            if (spinning)
            {
                spinWheel.localEulerAngles = new Vector3(0, 0, spinWheel.localEulerAngles.z + (Time.deltaTime * rotationSpeed * 20));
                if (spinWheel.localEulerAngles.z >= 360)
                    spinWheel.localEulerAngles = Vector3.zero;
            }
        }
        else
        {
            spinButton.interactable = false;
            timeRemainingText.text = timeRemaining.Hours + " : " + timeRemaining.Minutes + " : " + timeRemaining.Seconds;
        }
    }

    #endregion

    #region Timer


    //Enables Calculation un update
    public void CalculateTimer()
    {
        timeCalculator = true;
    }

    //Disable Calculation un update
    public void DisbaleCalculations()
    {
        timeCalculator = false;
    }

    private void LoadTimerDateTime()
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("TimerData").Child(PlayerData.PlayfabID).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if(DBTask.IsFaulted)
            {
                Debug.LogError("Error while loading timer date and time : \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }
            else if (DBTask.IsCompleted)
            {
                DataSnapshot snapshot = DBTask.Result;
                if (!string.IsNullOrEmpty((string)snapshot.Value))
                {
                    if (DateTime.TryParse((string)snapshot.Value, out DateTime loadedDateTime))
                    {
                        timerStartTime = loadedDateTime;
                        spinButton.interactable = true;
                    }
                    else
                    {
                        Debug.LogError("Daily Spin Manager Error : Can not parse saved date time. \nTried Parsing : " + (string)snapshot.Value);
                        timerStartTime = DateTime.Now;
                        spinButton.interactable = false;
                    }
                }
                else
                {
                    timerStartTime = DateTime.Now;
                    spinButton.interactable = false;
                }
                loaded = true;
            }
        });
    }

    private void SaveTimerDateTime()
    {
        var DBTask = FirebaseDatabase.DefaultInstance.RootReference.Child("TimerData").Child(PlayerData.PlayfabID).SetValueAsync(timerStartTime.ToString());
        if (DBTask.Exception != null)
        {
            Debug.LogError("Error while blocking user: \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
        }
        else if (DBTask.IsCompleted)
        {
            Debug.Log("Data Saved");
        }
    }

    #endregion

    #region Spin Wheel

    public void SpinTheWheel()
    {
        spinButton.interactable = false;
        AdsManager.manager.ShowAd(() =>
        {
            spinning = true;
            rotationSpeed = 50f;
            spinWheel.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(0, 360));
            StartCoroutine(StopSpinningAfterSeconds());
        });
        
    }

    IEnumerator StopSpinningAfterSeconds()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(minStopSpinningSeconds, maxStopSpinningSeconds));

        bool stopped = false;
        while (!stopped)
        {
            rotationSpeed =  Mathf.Lerp(rotationSpeed, 0, Time.deltaTime);
            if (rotationSpeed<=0.1f)
            {
                SpinRewards closestReward = rewards[0];

                foreach (var reward in rewards)
                    if (Mathf.Abs(reward.rotation - Mathf.Abs(spinWheel.localEulerAngles.z)) < Mathf.Abs(closestReward.rotation - Mathf.Abs(spinWheel.localEulerAngles.z)))
                        closestReward = reward;

                yield return new WaitForSeconds(2);
                spinning = false;
                stopped = true;

                GrantReward(closestReward);
            }
            yield return null;
        }

        timerStartTime = DateTime.Now.AddSeconds(timeIntervalInSeconds);
        SaveTimerDateTime();
    }

    #endregion

    #region Reward Granted Panel

    private void GrantReward(SpinRewards reward)
    {
        BoosterAndPerkItem booster = BoosterAndPerksData.data.GetRandomItemFromGroup(reward.boosterItem);

        InventoryManager.manager.GrantItem(booster.itemId);
        UIAnimationManager.manager.PopUpPanel(rewardGrantedPanel);
        rewardGrantedPanelText.text = "You have got " + booster.itemName;
        spinButton.interactable = true;
        duplicateRewardBtn.onClick.RemoveAllListeners();
        duplicateRewardBtn.onClick.AddListener(()=>
        {
            AdsManager.manager.ShowAd(() =>
            {
                InventoryManager.manager.GrantItem(booster.itemId);
                OnClick_Close_RewardGrantedPanel();
            });
        });
    }

    public void OnClick_Close_RewardGrantedPanel()
    {
        UIAnimationManager.manager.PopDownPanel(rewardGrantedPanel);
    }

    #endregion

}

enum GameState
{
    WatchingFirstAd,
    ThrowingKnife,
    WatchingSecondAd
}

[Serializable]
public struct SpinRewards
{
    public float rotation;
    public ItemGroup boosterItem;
}