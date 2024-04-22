using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;

public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager manager;
    public static int endGameTimer = 300;

    /*[Header("Won Panel References")]
    [SerializeField] Image wonPanelAvatarImage;
    [SerializeField] TMP_Text wonNameText;*/

    [Space]
    public GameObject twoPlayersWinPanel;
    public GameObject fourPlayersWinPanel;

    [Header("Won Panel References")]
    public GameWinnerPlayersData twoPlayersData;
    [Space]
    public List<GameWinnerPlayersData> fourPlayersData;

    [Header("Win/Lost Panel")]
    [SerializeField] Button closeGameOverPanelBtn;
    [SerializeField] Button sendFriendReqToWinnerBtn;
    [SerializeField] TMP_Text timeoutText;

    [Header("VFX")]
    [SerializeField] ParticleSystemRenderer[] confettiVFX;
    [SerializeField] Color[] vfxRandomColors;

    // We set these variables from the function, and by taking reference from GameMode.cs script
    private TMP_Text endGameTimerText;
    private Button endGameButton;
    private GameObject endGamePanel;
    bool isEndGameTimerPanelInitialized = false;

    private void Awake()
    {
        manager = this;
    }

    public void InitializeEndGameTimerPanel(GameMode gameMode)
    {
        endGameTimerText = gameMode.endGameTimerText;
        endGameButton = gameMode.endGameButton;
        endGamePanel = gameMode.endGamePanel;
        isEndGameTimerPanelInitialized = true;
    }

    public void InitializeGameOverMenu(string winnerPlayfabID, bool won, string winnerTeammatePlayfabID = "")
    {
        twoPlayersWinPanel.SetActive(false);
        fourPlayersWinPanel.SetActive(false);

        Color randColor = vfxRandomColors[UnityEngine.Random.Range(0, vfxRandomColors.Length)];
        foreach (var item in confettiVFX)
            item.material.SetVector("_EmissionColor",randColor*3);

        if (!winnerPlayfabID.Equals(PlayerData.PlayfabID))
        {
            sendFriendReqToWinnerBtn.interactable = true;
            sendFriendReqToWinnerBtn.onClick.RemoveAllListeners();
            sendFriendReqToWinnerBtn.onClick.AddListener(() =>
            {
                sendFriendReqToWinnerBtn.interactable = false;
                FriendManager.manager.SendFriendRequestViaPlayfab(winnerPlayfabID, () => { }, () => sendFriendReqToWinnerBtn.interactable = true);
            });
        }
        else
        {
            sendFriendReqToWinnerBtn.interactable = false;
        }

        if(!isEndGameTimerPanelInitialized)
        {
            Debug.LogError("Error While Initializing Game Over: End Game Timer Panel not initialized, Reference Not Found");
            return;
        }

        endGameButton.onClick.RemoveAllListeners();
        endGameButton.onClick.AddListener(() =>
        {
            StopAllCoroutines();
            UIManager.manager.ClosePanel(endGamePanel, () =>
            {
                //Disconnect From Server
                GameNetworkManager.manager.LeaveGame();

                switch (MatchMakingManager.manager.matchType)
                {
                    case MatchType.TwoPlayer:
                        twoPlayersWinPanel.SetActive(true);
                        ProfileFetcher.FetchAndSetAvatarImage(winnerPlayfabID, twoPlayersData.wonPanelAvatarImage);
                        ProfileFetcher.FetchAndSetUserNameWithoutTag(winnerPlayfabID, twoPlayersData.wonNameText);
                        break;

                    case MatchType.FourPlayer:
                        fourPlayersWinPanel.SetActive(true);

                        ProfileFetcher.FetchAndSetAvatarImage(winnerPlayfabID, fourPlayersData[0].wonPanelAvatarImage);
                        ProfileFetcher.FetchAndSetUserNameWithoutTag(winnerPlayfabID, fourPlayersData[0].wonNameText);

                        ProfileFetcher.FetchAndSetAvatarImage(winnerTeammatePlayfabID, fourPlayersData[1].wonPanelAvatarImage);
                        ProfileFetcher.FetchAndSetUserNameWithoutTag(winnerTeammatePlayfabID, fourPlayersData[1].wonNameText);
                        break;
                }

                UIManager.manager.OpenPanel(UIManager.manager.gameOverPanel);
                closeGameOverPanelBtn.onClick.RemoveAllListeners();
                closeGameOverPanelBtn.onClick.AddListener(()=> OnClick_CloseGameOverMenu(won));
                StartCoroutine(Coroutine_StartTimeoutTimer());
            });
        });

        StartCoroutine(Coroutine_StartEndGameTimer());
    }

    public void OnClick_CloseGameOverMenu(bool openBattlePassWindow)
    {
        if(openBattlePassWindow)
            UIManager.manager.ClosePanel(UIManager.manager.gameOverPanel, () => UIManager.manager.OpenPanel(UIManager.manager.battlePassPanel, () => BattlePassUIManager.manager.OnGameOver_GoToProfile()));
        else
            UIManager.manager.ClosePanel(UIManager.manager.gameOverPanel);
    }

    IEnumerator Coroutine_StartEndGameTimer()
    {
        UIManager.manager.OpenPanel(endGamePanel);
        int timeLeftToEndGame = endGameTimer;

        while (timeLeftToEndGame > 0)
        {
            yield return new WaitForSeconds(1);
            endGameTimerText.text = Mathf.Floor(timeLeftToEndGame / 60).ToString("00") + ":" + Mathf.FloorToInt(timeLeftToEndGame % 60).ToString("00");
            timeLeftToEndGame--;
        }

        endGameButton.onClick.Invoke();
    }

    IEnumerator Coroutine_StartTimeoutTimer()
    {
        isEndGameTimerPanelInitialized = false;
        closeGameOverPanelBtn.interactable = false;

        int wait = 3;
        while (wait > 0)
        {
            timeoutText.text = wait.ToString();
            wait--;
            yield return new WaitForSeconds(1);
        }
        timeoutText.text = "";
        closeGameOverPanelBtn.interactable = true;
    }
}


[Serializable]
public class GameWinnerPlayersData
{
    public Image wonPanelAvatarImage;
    public TMP_Text wonNameText;
}
