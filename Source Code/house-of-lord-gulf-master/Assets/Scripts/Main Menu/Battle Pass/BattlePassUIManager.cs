using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections;
using static UnityEngine.GraphicsBuffer;

public class BattlePassUIManager : MonoBehaviour
{
    public static BattlePassUIManager manager;

    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform contentPanel;
    [SerializeField] float scrollSpeed = 10;

    [Header("UI")]
    [SerializeField] RectTransform bpAvatarImageHolder;
    [SerializeField] Image bpAvatarImage;
    [SerializeField] TMP_Text playerLevelText;
    [SerializeField] TMP_Text timeRemainingTxt;

    [Header("Rewards")]
    [SerializeField] List<RewardItem> rewardItemsGO;

    // Animations Names should be as 1,2,3,4,5....30
    [Header("Avatar Animation")]
    [SerializeField] Animator avatarAnimator;

    internal Season season = null;
    DateTime endDate;
    bool startCounting = false;
    bool bpInitialized = false;
    bool calculating = false;
    private TimeSpan timeLeft;
    private float target;
    [Space]
    public float m_value_to_minues;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += () => ProfileFetcher.FetchAndSetAvatarImage(PlayerData.PlayfabID, bpAvatarImage);
        ProfileManager.manager.OnLoadingNewProfileOrUpdatingProfile += (playfabID) => ProfileFetcher.FetchAndSetAvatarImage(PlayerData.PlayfabID, bpAvatarImage);
        BattlePassManager.manager.OnPlayerDataUpdated += InitializeBattlePass;
    }

    private void Update()
    {
        if (!calculating) return;
        if (startCounting)
        {
            //Debug.Log("Calculating");
            timeLeft = endDate - DateTime.UtcNow;
            //Debug.Log(endDate + "    " + DateTime.UtcNow);
            //Debug.Log(timeLeft.Days);
            //Debug.Log(timeLeft.Hours);
            //Debug.Log(timeLeft.Minutes);
            //Debug.Log(timeLeft.Seconds);
            timeRemainingTxt.text = timeLeft.Days.ToString() + " : " + timeLeft.Hours.ToString() + " : " + timeLeft.Minutes.ToString() + " : " + timeLeft.Seconds;
            if (timeLeft.TotalMilliseconds < 0)
                UpdateBattlePass();
        }
    }

    public void UpdateCounter(int status)
    {
        if (status == 0)
        {
            calculating = false;
            startCounting=false;
        }
        else
        {
            calculating = true;
            startCounting=true;
        }
    }


    public void InitializeBattlePass()
    {
        BattlePassData battlePass = BattlePassManager.manager.battlePassData;
        BattlePassUserData userData = BattlePassManager.manager.battlePassUserData;
        playerLevelText.text = "Player Level : " + userData.playerLevel.ToString();

        if (battlePass.seasons.Count <= 0)
        {
            Debug.LogError("There are no available seasons. Please add more seasons.");
            return;
        }

        Season currentSeason = null;

        foreach(Season season in battlePass.seasons)
        {
            DateTime seasonEndTime = new(season.endYear, season.endMonth, season.endDate);

            if(seasonEndTime> DateTime.UtcNow)
            {
                currentSeason = season;
                break;
            }
        }

        if (currentSeason != null)
        {
            bool vfxShown = false;
            season = currentSeason;
            BattlePassManager.manager.CheckAndRenewBattlePass();

            if (currentSeason.rewards.Count != 10)
            {
                Debug.LogError("Reward Count is not 10, BP is not loaded");
                return;
            }

            for(int i=0; i<10; i++)
            {
                Reward reward = currentSeason.rewards[i];
                rewardItemsGO[i].InitializeReward(reward, userData.playerLevel >= reward.level, userData.collectedLevels.Contains(reward.level), !vfxShown && userData.playerLevel <= reward.level);

                if (!vfxShown)
                    vfxShown = userData.playerLevel <= reward.level;
            }


            endDate = new(currentSeason.endYear, currentSeason.endMonth, currentSeason.endDate);
            Debug.Log(endDate);
            startCounting = true;
        }
        else
        {
            Debug.Log("All seasons are expired, Please add a new season");
        }

        if (!bpInitialized)
            AnimateAvatarImageToNextLevel();

        bpInitialized = true;
    }

    private void UpdateBattlePass()
    {
        InitializeBattlePass();
        startCounting = false;
    }

    public void AnimateAvatarImageToNextLevel()
    {
        StopAllCoroutines();
        StartCoroutine(Coroutine_AnimateAvatarImage());
    }

    IEnumerator Coroutine_AnimateAvatarImage()
    {
        // We are using tempUserData instead of battlePassData cause tempUserData is instantly updated as soon as player wons a game
        print("Animating BP Avatar To Level : " + BattlePassManager.manager.tempUserData.playerLevel.ToString());
        yield return new WaitWhile(() => !avatarAnimator.gameObject.activeInHierarchy);
        avatarAnimator.Play(BattlePassManager.manager.tempUserData.playerLevel.ToString());

        yield return new WaitForSeconds(0.5f);
        SnapToProfile();
    }

    public void SnapToProfile()
    {
        target = 1 - ((contentPanel.rect.height / 2 - bpAvatarImageHolder.localPosition.y) / contentPanel.rect.height);
        target -= m_value_to_minues;
        Debug.Log(target);
        scrollRect.normalizedPosition = new(0, target);
        //StartCoroutine(Coroutine_ScrollToProfile());
    }

    IEnumerator Coroutine_ScrollToProfile()
    {
        yield return new WaitForSeconds(0.1f);
        var target = 1 - ((contentPanel.rect.height / 2 - bpAvatarImageHolder.localPosition.y) / contentPanel.rect.height);
        while (Mathf.Abs(scrollRect.normalizedPosition.y - target) > 0.02f)
        {
            scrollRect.normalizedPosition = new(0, Mathf.Lerp(scrollRect.normalizedPosition.y, target, scrollSpeed * Time.deltaTime));
            yield return null;
        }
    }

    public void OnGameOver_GoToProfile()
    {
        var target = 1 - ((contentPanel.rect.height / 2 - bpAvatarImageHolder.localPosition.y) / contentPanel.rect.height);
        scrollRect.normalizedPosition = new(0, target);
        AnimateAvatarImageToNextLevel();
    }
}
