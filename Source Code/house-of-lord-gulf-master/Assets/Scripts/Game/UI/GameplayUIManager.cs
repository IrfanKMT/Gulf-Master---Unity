using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System;
using Edgegap;
using System.IO;
using System.Linq;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager manager;

    [Header("GamePlay UI")]
    public Transform gameplayUI;

    [Header("Emoji-Chat Canvas")]
    public GameObject emojiChatCanvas;

    [Header("Random Player From Top 20 Players")]
    [SerializeField] LeaderboardItem randomPlayerFromTop20PlayersItem;

    [Space]
    public GameObject twoPlayersUI;
    public GameObject fourPlayersUI;

    [Space]
    [Header("Four Players UI")]
    public List<PlayersInGameUiDetails> fourPlayerDetails;
    [Space]
    [Header("Two Players UI")]
    public List<PlayersInGameUiDetails> twoPlayerDetails;

    /*[Header("Player 1 UI")]
    [SerializeField] Image player1AvatarImage;
    [SerializeField] TMP_Text player1Name;


    [Header("Player 2 UI")]
    [SerializeField] Image player2AvatarImage;
    [SerializeField] TMP_Text player2Name;


    [Header("Player 1 Booster")]
    [SerializeField] Button player1BoosterButton;
    [SerializeField] Image player1BoosterImage;
    [SerializeField] Image[] player1BoosterFillCandiesImages;
    [SerializeField] GameObject player1BoosterActiveVFX;

    [Header("Player 2 Booster")]
    [SerializeField] Button player2BoosterButton;
    [SerializeField] Image player2BoosterImage;
    [SerializeField] Image[] player2BoosterFillCandiesImages;
    [SerializeField] GameObject player2BoosterActiveVFX;*/

    [Header("Booster Candy Sprites")]
    [SerializeField] Sprite blueCandySprite;
    [SerializeField] Sprite grayBlueCandySprite;
    [SerializeField] Sprite redCandySprite;
    [SerializeField] Sprite grayRedCandySprite;

    /*[Header("Moves Counter Player 1")]
    [SerializeField] GameObject player1MoveCounter1;
    [SerializeField] GameObject player1MoveCounter2;

    [Header("Moves Counter Player 2")]
    [SerializeField] GameObject player2MoveCounter1;
    [SerializeField] GameObject player2MoveCounter2;*/

    [Header("Score")]
    [SerializeField] TMP_Text player1Score;
    [SerializeField] TMP_Text player1ScoreMultiplier;
    [SerializeField] TMP_Text player2Score;
    [SerializeField] TMP_Text player2ScoreMultiplier;

    [Header("Round Counter")]
    [SerializeField] Image[] roundCounters;

    [Header("Turn Counter")]
    public Slider timerSlider;
    public Slider timerSliderFourPlayer;

    [Header("Perk Images")]
    [SerializeField] Image ourPerk1Image;
    [SerializeField] Image ourPerk2Image;
    [SerializeField] GameObject ourPerk1HighlightVFX;
    [SerializeField] GameObject ourPerk2HighlightVFX;
    [SerializeField] Image opponentPerk1Image;
    [SerializeField] Image opponentPerk2Image;
    [SerializeField] GameObject opponentPerk1HighlightVFX;
    [SerializeField] GameObject opponentPerk2HighlightVFX;
    [SerializeField] Color usedPerkColor;

    [Header("Perk Buttons")]
    public Button perk1Button;
    public Button perk2Button;

    [Header("Scripts")]
    [SerializeField] PerksManager perksManager;
    [SerializeField] BoosterManager boosterManager;
    [SerializeField] GamePlayManager gamePlayManager;

    float localBoosterFillTarget;
    float other2BoosterFillTarget;
    float other3BoosterFillTarget;
    float other4BoosterFillTarget;

    //2 players mode opponent
    internal string opponentPlayfabID;
    //4 players mode opponents
    [Space]
    [Header("PLAYER PLAFAB ID STORED")]
    public List<string> myTeamPlayfabID;
    public List<string> opponentTeamPlayfabID;
    [Space]
    public List<PlayersInGameUiDetails> templist;

    private float valueToReach;

    #region Unity Functions

    private void Awake()
    {
        manager = this;

#if !UNITY_SERVER

        perksManager.OnPerksInitialized += InitializePerks;
        perksManager.OnPlayerUsedPerk += OnPlayerUsedPerk;

        boosterManager.OnBoostersInitialized += InitializeBoosters;
        boosterManager.OnBoosterCandiesChanged += UpdateBoosterCandyCount;

        gamePlayManager.OnRoundChanged += UpdateRoundsCounter;
        gamePlayManager.OnMoveCounterChanged += UpdateMovesCounter;
        gamePlayManager.OnScoresChanged += UpdateScores;
        gamePlayManager.OnScoresMultiplierChanged += UpdateScoresMultiplier;

        SetMovesCounterToDefault();

        //gamePlayManager.LocalmatchType = MatchMakingManager.manager.matchType;
        gamePlayManager.matchtype = MatchMakingManager.manager.matchType;
        gamePlayManager.serverteamtype = MatchMakingManager.manager.MyteamType;

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                twoPlayersUI.SetActive(true);
                break;

            case MatchType.FourPlayer:
                fourPlayersUI.SetActive(true);
                break;
        }

#endif
    }


    //    private void Update()
    //    {
    //#if !UNITY_SERVER
    //        player1BoosterActiveVFX.SetActive(player1BoosterButton.interactable == true);
    //        player2BoosterActiveVFX.SetActive(player2BoosterButton.interactable == true);
    //#endif
    //    }

    #endregion

    #region Initialize

    public IEnumerator SwapPlayers()
    {

        Debug.Log("Swaping Players");
        foreach (var item in fourPlayerDetails)
        {
            templist.Add(item);
        }


        List<GamePlayerData> data = new List<GamePlayerData>();
        data.Add(GameMode.gameMode.localPlayer1);
        data.Add(GameMode.gameMode.otherPlayer2);
        data.Add(GameMode.gameMode.otherPlayer3);
        data.Add(GameMode.gameMode.otherPlayer4);


        int myindex = 0;
        int mypartnerindex = 0;
        int opponent1 = 0;
        int opponent2 = 0;

        int a = 0;

        foreach (GamePlayerData player in data)
        {
            if (player.playfabID == PlayerData.PlayfabID)
            {
                myindex = a;
            }
            //PARTNER 
            if (player.playfabID == MatchMakingUIManager.manager.LocalTeam[1].playfabID)
            {
                mypartnerindex = a;
            }

            //OPPONENT 1
            if (player.playfabID == MatchMakingUIManager.manager.OpponentTeam[0].playfabID)
            {
                opponent1 = a;
            }

            //Oppponent 2
            if (player.playfabID == MatchMakingUIManager.manager.OpponentTeam[1].playfabID)
            {
                opponent2 = a;
            }
            a++;
        }
        Debug.Log("My Index is  " + myindex);
        Debug.Log("My Partner is  " + mypartnerindex);
        Debug.Log("My Opponent 1 is  " + opponent1);
        Debug.Log("My Opponent 2 is  " + opponent2);
        yield return null;
        fourPlayerDetails[0] = templist[myindex];
        fourPlayerDetails[1] = templist[mypartnerindex];
        fourPlayerDetails[2] = templist[opponent1];
        fourPlayerDetails[3] = templist[opponent2];

        //Change Color Of OPPONENT PLAYER CANDIES
        //Swap scores
        if (gamePlayManager.serverteamtype == TeamType.TeamB)
        {
            TMP_Text tempScoreTxt;
            tempScoreTxt = player1Score;
            player1Score = player2Score;
            player2Score = tempScoreTxt;

            TMP_Text tempScoreMultipierTxt;
            tempScoreMultipierTxt = player1ScoreMultiplier;
            player1ScoreMultiplier = player2ScoreMultiplier;
            player2ScoreMultiplier = tempScoreMultipierTxt;
        }

        //Swap booster candies images
    }
    public void InitializePlayers(string localPlayerPlayfabID, string otherPlayer2PlayfabID, string otherPlayer3PlayfabID, string otherPlayer4PlayfabID)
    {
        switch (gamePlayManager.matchtype)
        {
            //2 Players Mode
            case MatchType.TwoPlayer:
                ProfileFetcher.FetchAndSetUserNameWithoutTag(localPlayerPlayfabID, twoPlayerDetails[0].playerNameText);
                ProfileFetcher.FetchAndSetAvatarImage(localPlayerPlayfabID, twoPlayerDetails[0].playerAvtarImage);

                /*ProfileFetcher.FetchAndSetUserNameWithoutTag(localPlayerPlayfabID, player1Name);
                ProfileFetcher.FetchAndSetAvatarImage(localPlayerPlayfabID, player1AvatarImage);*/

                opponentPlayfabID = otherPlayer2PlayfabID;
                if (!string.IsNullOrEmpty(opponentPlayfabID))
                {
                    ProfileFetcher.FetchAndSetUserNameWithoutTag(opponentPlayfabID, twoPlayerDetails[1].playerNameText);
                    ProfileFetcher.FetchAndSetAvatarImage(opponentPlayfabID, twoPlayerDetails[1].playerAvtarImage);
                }
                else
                {
                    twoPlayerDetails[1].playerNameText.text = "Loading...";
                    twoPlayerDetails[1].playerAvtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
                }
                break;

            //4 Players Mode
            case MatchType.FourPlayer:
                SetFourTeamData(localPlayerPlayfabID, otherPlayer2PlayfabID, otherPlayer3PlayfabID, otherPlayer4PlayfabID);
                break;
        }


        player1Score.text = "00";
        player2Score.text = "00";
    }

    public void SetFourTeamData(string pid1, string pid2, string pid3, string pid4)
    {
        Debug.Log("Team Data Setup Done Here");
        ProfileFetcher.FetchAndSetUserNameWithoutTag(pid1, fourPlayerDetails[0].playerNameText);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(pid2, fourPlayerDetails[1].playerNameText);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(pid3, fourPlayerDetails[2].playerNameText);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(pid4, fourPlayerDetails[3].playerNameText);

        myTeamPlayfabID = new List<string>();
        opponentTeamPlayfabID = new List<string>();

        myTeamPlayfabID.Add(pid1);
        myTeamPlayfabID.Add(pid2);

        opponentTeamPlayfabID.Add(pid3);
        opponentTeamPlayfabID.Add(pid4);
    }


    public void SetMyAndOpponentTeamPlayersData(string playerPlayfabID, int index)
    {
        if (!string.IsNullOrEmpty(playerPlayfabID))
        {

            ProfileFetcher.FetchAndSetUserNameWithoutTag(playerPlayfabID, fourPlayerDetails[index].playerNameText);
            ProfileFetcher.FetchAndSetAvatarImage(playerPlayfabID, fourPlayerDetails[index].playerAvtarImage);
        }
        else
        {
            fourPlayerDetails[index].playerNameText.text = "Loading...";
            fourPlayerDetails[index].playerAvtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
        }
    }

    private void InitializeBoosters(BoosterAndPerkItem player1Booster, BoosterAndPerkItem player2Booster, BoosterAndPerkItem player3Booster, BoosterAndPerkItem player4Booster)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            switch (MatchMakingManager.manager.matchType)
            {
                case MatchType.TwoPlayer:
                    /*BoosterAndPerkItem localBooster = player1Booster;
                    BoosterAndPerkItem otherBooster = player2Booster;

                    if (gamePlayManager.LocalPlayer != null)
                    {
                        localBooster = gamePlayManager.LocalPlayer.isPlayer1 ? player1Booster : player2Booster;
                        otherBooster = !gamePlayManager.LocalPlayer.isPlayer1 ? player1Booster : player2Booster;
                    }*/

                    twoPlayerDetails[0].boosterImage.sprite = player1Booster.itemImage;
                    twoPlayerDetails[1].boosterImage.sprite = player2Booster.itemImage;
                    twoPlayerDetails[0].boosterBtn.onClick.RemoveAllListeners();
                    //player1BoosterButton.onClick.AddListener(BoosterManager.manager.UseBooster);

                    if (gamePlayManager.LocalPlayer != null)
                        twoPlayerDetails[0].boosterBtn.onClick.AddListener(gamePlayManager.LocalPlayer.UseBooster);

                    localBoosterFillTarget = 0;
                    other2BoosterFillTarget = 0;

                    twoPlayerDetails[0].boosterBtn.interactable = false;
                    twoPlayerDetails[1].boosterBtn.interactable = false;

                    twoPlayerDetails[0].boosterVFX.SetActive(false);
                    twoPlayerDetails[1].boosterVFX.SetActive(false);

                    for (int i = 0; i < twoPlayerDetails[0].playerBoosterFillerCandiesImages.Length; i++)
                    {
                        twoPlayerDetails[0].playerBoosterFillerCandiesImages[i].sprite = grayBlueCandySprite;
                        twoPlayerDetails[1].playerBoosterFillerCandiesImages[i].sprite = grayRedCandySprite;
                    }
                    break;

                case MatchType.FourPlayer:

                    SetMyAndOpponentTeamBooster(player1Booster, 0, true);
                    SetMyAndOpponentTeamBooster(player2Booster, 1, true);
                    SetMyAndOpponentTeamBooster(player3Booster, 2, false);
                    SetMyAndOpponentTeamBooster(player4Booster, 3, false);

                    if (PlayerData.PlayfabID == gamePlayManager.player1PlayfaID)
                    {
                        Debug.Log("My Booster Selected on inde  " + 0);
                        fourPlayerDetails[0].boosterBtn.onClick.RemoveAllListeners();
                        fourPlayerDetails[0].boosterBtn.onClick.AddListener(delegate { gamePlayManager.LocalPlayer.UseBooster(PlayerData.PlayfabID); });
                    }
                    else if (PlayerData.PlayfabID == gamePlayManager.player2PlayfaID)
                    {
                        Debug.Log("My Booster Selected on inde  " + 1);
                        fourPlayerDetails[1].boosterBtn.onClick.RemoveAllListeners();
                        fourPlayerDetails[1].boosterBtn.onClick.AddListener(delegate { gamePlayManager.LocalPlayer.UseBooster(PlayerData.PlayfabID); });
                    }
                    else if (PlayerData.PlayfabID == gamePlayManager.player3PlayfaID)
                    {
                        Debug.Log("My Booster Selected on inde  " + 2);
                        fourPlayerDetails[2].boosterBtn.onClick.RemoveAllListeners();
                        fourPlayerDetails[2].boosterBtn.onClick.AddListener(delegate { gamePlayManager.LocalPlayer.UseBooster(PlayerData.PlayfabID); });
                    }
                    else
                    {
                        Debug.Log("My Booster Selected on inde  " + 3);
                        fourPlayerDetails[3].boosterBtn.onClick.RemoveAllListeners();
                        fourPlayerDetails[3].boosterBtn.onClick.AddListener(delegate { gamePlayManager.LocalPlayer.UseBooster(PlayerData.PlayfabID); });
                    }
                    break;
            }
        }));
    }

    void SetMyAndOpponentTeamBooster(BoosterAndPerkItem playerBooster, int index, bool isLocalTeam)
    {
        fourPlayerDetails[index].boosterImage.sprite = playerBooster.itemImage;

        fourPlayerDetails[index].boosterBtn.interactable = false;
        fourPlayerDetails[index].boosterVFX.SetActive(false);

        if (isLocalTeam)
        {
            for (int i = 0; i < fourPlayerDetails[index].playerBoosterFillerCandiesImages.Length; i++)
            {
                fourPlayerDetails[index].playerBoosterFillerCandiesImages[i].sprite = grayBlueCandySprite;
            }
        }
        else
        {
            for (int i = 0; i < fourPlayerDetails[index].playerBoosterFillerCandiesImages.Length; i++)
            {
                fourPlayerDetails[index].playerBoosterFillerCandiesImages[i].sprite = grayRedCandySprite;
            }
        }

        /*localBoosterFillTarget = 0;
        otherBoosterFillTarget = 0;*/
    }

    private void InitializePerks(BoosterAndPerkItem player1Perk1, BoosterAndPerkItem player1Perk2,
                                 BoosterAndPerkItem player2Perk1, BoosterAndPerkItem player2Perk2,
                                 BoosterAndPerkItem player3Perk1, BoosterAndPerkItem player3Perk2,
                                 BoosterAndPerkItem player4Perk1, BoosterAndPerkItem player4Perk2)
    {
        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                twoPlayerDetails[0].perk1Image.sprite = player1Perk1.itemImage;
                twoPlayerDetails[0].perk1HighlightVFX.SetActive(false);
                twoPlayerDetails[0].perk2Image.sprite = player1Perk2.itemImage;
                twoPlayerDetails[0].perk2HighlightVFX.SetActive(false);

                twoPlayerDetails[1].perk1Image.sprite = player2Perk1.itemImage;
                twoPlayerDetails[1].perk1HighlightVFX.SetActive(false);
                twoPlayerDetails[1].perk2Image.sprite = player2Perk2.itemImage;
                twoPlayerDetails[1].perk2HighlightVFX.SetActive(false);
                break;

            case MatchType.FourPlayer:
                fourPlayerDetails[0].perk1Image.sprite = player1Perk1.itemImage;
                fourPlayerDetails[0].perk1HighlightVFX.SetActive(false);
                fourPlayerDetails[0].perk2Image.sprite = player1Perk2.itemImage;
                fourPlayerDetails[0].perk2HighlightVFX.SetActive(false);

                fourPlayerDetails[1].perk1Image.sprite = player2Perk1.itemImage;
                fourPlayerDetails[1].perk1HighlightVFX.SetActive(false);
                fourPlayerDetails[1].perk2Image.sprite = player2Perk2.itemImage;
                fourPlayerDetails[1].perk2HighlightVFX.SetActive(false);

                fourPlayerDetails[2].perk1Image.sprite = player3Perk1.itemImage;
                fourPlayerDetails[2].perk1HighlightVFX.SetActive(false);
                fourPlayerDetails[2].perk2Image.sprite = player3Perk2.itemImage;
                fourPlayerDetails[2].perk2HighlightVFX.SetActive(false);

                fourPlayerDetails[3].perk1Image.sprite = player4Perk1.itemImage;
                fourPlayerDetails[3].perk1HighlightVFX.SetActive(false);
                fourPlayerDetails[3].perk2Image.sprite = player4Perk2.itemImage;
                fourPlayerDetails[3].perk2HighlightVFX.SetActive(false);
                break;
        }

        /*ourPerk1Image.sprite = player1Perk1.itemImage;
        ourPerk2Image.sprite = player1Perk2.itemImage;

        opponentPerk1Image.sprite = player2Perk1.itemImage;
        opponentPerk2Image.sprite = player2Perk2.itemImage;

        ourPerk1HighlightVFX.SetActive(false);
        ourPerk2HighlightVFX.SetActive(false);

        opponentPerk1HighlightVFX.SetActive(false);
        opponentPerk2HighlightVFX.SetActive(false);*/

        perk1Button.interactable = true;
        perk2Button.interactable = true;

        perk1Button.onClick.RemoveAllListeners();
        perk2Button.onClick.RemoveAllListeners();

        perk1Button.onClick.AddListener(() => OnClick_Perk(true));
        perk2Button.onClick.AddListener(() => OnClick_Perk(false));
    }

    private void SetMovesCounterToDefault()
    {
        /*player1MoveCounter1.SetActive(true);
        player1MoveCounter2.SetActive(true);

        player2MoveCounter1.SetActive(true);
        player2MoveCounter2.SetActive(true);*/

        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                twoPlayerDetails[0].moveCounterObjects[0].SetActive(true);
                twoPlayerDetails[0].moveCounterObjects[1].SetActive(true);

                twoPlayerDetails[1].moveCounterObjects[0].SetActive(true);
                twoPlayerDetails[1].moveCounterObjects[1].SetActive(true);
                break;

            case MatchType.FourPlayer:
                fourPlayerDetails[0].moveCounterObjects[0].SetActive(true);
                fourPlayerDetails[0].moveCounterObjects[1].SetActive(true);

                fourPlayerDetails[1].moveCounterObjects[0].SetActive(true);
                fourPlayerDetails[1].moveCounterObjects[1].SetActive(true);

                fourPlayerDetails[2].moveCounterObjects[0].SetActive(true);
                fourPlayerDetails[2].moveCounterObjects[1].SetActive(true);

                fourPlayerDetails[3].moveCounterObjects[0].SetActive(true);
                fourPlayerDetails[3].moveCounterObjects[1].SetActive(true);
                break;
        }

        foreach (var item in roundCounters)
            item.color = new Color(1, 1, 1, 1);
    }

    public void InitializeRandomPlayerFromTop20Player(Gender gender, int randomLeaderboardPlayerIndex)
    {
        if (gender == 0) return;

        LeaderboardsManager.GetLeaderboardData((Countries)PlayerData.Country, gender == Gender.Male, 25, (res) =>
        {
            List<PlayerLeaderboardEntry> players = res.Leaderboard;

            if (players.Count == 0)
                randomPlayerFromTop20PlayersItem.ResetData();
            else
                randomPlayerFromTop20PlayersItem.SetupItem(players[randomLeaderboardPlayerIndex].PlayFabId, PlayerData.Country);
        },
        err => { });
    }

    #endregion

    #region Update UI

    private void UpdateMovesCounter(int player1MovesLeft, int player2MovesLeft, int player3MovesLeft = 0, int player4MovesLeft = 0)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            switch (gamePlayManager.matchtype)
            {
                case MatchType.TwoPlayer:
                    twoPlayerDetails[0].moveCounterObjects[0].SetActive(player1MovesLeft > 0);
                    twoPlayerDetails[0].moveCounterObjects[1].SetActive(player1MovesLeft > 1);

                    twoPlayerDetails[1].moveCounterObjects[0].SetActive(player2MovesLeft > 0);
                    twoPlayerDetails[1].moveCounterObjects[1].SetActive(player2MovesLeft > 1);
                    break;

                case MatchType.FourPlayer:
                    fourPlayerDetails[0].moveCounterObjects[0].SetActive(player1MovesLeft > 0);
                    fourPlayerDetails[0].moveCounterObjects[1].SetActive(player1MovesLeft > 1);

                    fourPlayerDetails[1].moveCounterObjects[0].SetActive(player2MovesLeft > 0);
                    fourPlayerDetails[1].moveCounterObjects[1].SetActive(player2MovesLeft > 1);

                    fourPlayerDetails[2].moveCounterObjects[0].SetActive(player3MovesLeft > 0);
                    fourPlayerDetails[2].moveCounterObjects[1].SetActive(player3MovesLeft > 1);

                    fourPlayerDetails[3].moveCounterObjects[0].SetActive(player4MovesLeft > 0);
                    fourPlayerDetails[3].moveCounterObjects[1].SetActive(player4MovesLeft > 1);
                    break;
            }

            /*int myMovesLeft = player1MovesLeft;
            int opponentMovesLeft = player2MovesLeft;

            if (gamePlayManager.LocalPlayer != null)
            {
                myMovesLeft = gamePlayManager.LocalPlayer.isPlayer1 ? player1MovesLeft : player2MovesLeft;
                opponentMovesLeft = !gamePlayManager.LocalPlayer.isPlayer1 ? player1MovesLeft : player2MovesLeft;
            }

            player1MoveCounter1.SetActive(myMovesLeft > 0);
            player1MoveCounter2.SetActive(myMovesLeft > 1);

            player2MoveCounter1.SetActive(opponentMovesLeft > 0);
            player2MoveCounter2.SetActive(opponentMovesLeft > 1);*/

        }));
    }

    private void UpdateRoundsCounter(int roundsLeft)
    {
        if (roundsLeft > 0)
        {
            foreach (var item in roundCounters)
                item.color = new Color(1, 1, 1, 0.6f);

            for (int i = 0; i < roundsLeft; i++)
                roundCounters[i].color = new Color(1, 1, 1, 1);
        }
        else
        {
            foreach (var item in roundCounters)
                item.color = new Color(1, 1, 1, 0.6f);

            roundCounters[^1].color = new Color(1, 1, 1, 1);
        }
    }

    private void UpdateScores(int p1Score, int p2Score)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            int myScore = p1Score;
            int otherScore = p2Score;

            switch (gamePlayManager.matchtype)
            {
                case MatchType.TwoPlayer:
                    if (gamePlayManager.LocalPlayer != null)
                    {
                        myScore = gamePlayManager.LocalPlayer.isPlayer1 ? p1Score : p2Score;
                        otherScore = !gamePlayManager.LocalPlayer.isPlayer1 ? p1Score : p2Score;
                    }
                    break;

                case MatchType.FourPlayer:
                    if (gamePlayManager.currentTurnPlayfabID == gamePlayManager.player1PlayfaID || gamePlayManager.currentTurnPlayfabID == gamePlayManager.player2PlayfaID)
                    {
                        myScore = p1Score;
                        otherScore = p2Score;
                    }
                    else if (gamePlayManager.currentTurnPlayfabID == gamePlayManager.player3PlayfaID || gamePlayManager.currentTurnPlayfabID == gamePlayManager.player4PlayfaID)
                    {
                        myScore = p2Score;
                        otherScore = p1Score;
                    }
                    break;
            }

            player1Score.text = myScore.ToString();
            player2Score.text = otherScore.ToString();
        }));
    }

    private void UpdateScoresMultiplier(int player1ScoreMul, int player2ScoreMul)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            int myScoreMultiplier = player1ScoreMul;
            int otherScoreMultiplier = player2ScoreMul;

            switch (gamePlayManager.matchtype)
            {
                case MatchType.TwoPlayer:
                    if (gamePlayManager.LocalPlayer != null)
                    {
                        myScoreMultiplier = gamePlayManager.LocalPlayer.isPlayer1 ? player1ScoreMul : player2ScoreMul;
                        otherScoreMultiplier = !gamePlayManager.LocalPlayer.isPlayer1 ? player1ScoreMul : player2ScoreMul;
                    }
                    break;

                case MatchType.FourPlayer:
                    if (gamePlayManager.currentTurnPlayfabID == gamePlayManager.player1PlayfaID || gamePlayManager.currentTurnPlayfabID == gamePlayManager.player2PlayfaID)
                    {
                        myScoreMultiplier = player1ScoreMul;
                        otherScoreMultiplier = player2ScoreMul;
                    }
                    else if (gamePlayManager.currentTurnPlayfabID == gamePlayManager.player3PlayfaID || gamePlayManager.currentTurnPlayfabID == gamePlayManager.player4PlayfaID)
                    {
                        myScoreMultiplier = player2ScoreMul;
                        otherScoreMultiplier = player1ScoreMul;
                    }
                    break;
            }

            if (player1ScoreMultiplier != null)
                player1ScoreMultiplier.text = myScoreMultiplier.ToString() + "x";
            if (player2ScoreMultiplier != null)
                player2ScoreMultiplier.text = otherScoreMultiplier.ToString() + "x";
        }));
    }

    /// <summary>
    /// Returns true when target is reached
    /// </summary>
    public bool SetTimerSliderValue(double target, double maxValue, float speed)
    {
        valueToReach = 0;
        if (maxValue > target)
            valueToReach = Mathf.Abs(1f - ((float)target / (float)maxValue));


        switch (gamePlayManager.matchtype)
        {
            case MatchType.TwoPlayer:
                timerSlider.value = Mathf.MoveTowards(timerSlider.value, valueToReach, Time.deltaTime * speed);
                return timerSlider.value == valueToReach;

            case MatchType.FourPlayer:
                timerSliderFourPlayer.value = Mathf.MoveTowards(timerSliderFourPlayer.value, valueToReach, Time.deltaTime * speed);
                return timerSliderFourPlayer.value == valueToReach;
        }

        return true;
    }

    private void UpdateBoosterCandyCount(int player1BoosterCandies, int player1MaxBoosterCandies, int player2BoosterCandies, int player2MaxBoosterCandies,
                                         int player3BoosterCandies, int player3MaxBoosterCandies, int player4BoosterCandies, int player4MaxBoosterCandies)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            int localPlayerBoosterCandies = player1BoosterCandies;
            int otherPlayer2BoosterCandies = player2BoosterCandies;

            localBoosterFillTarget = player1BoosterCandies / (float)player1MaxBoosterCandies;
            other2BoosterFillTarget = player2BoosterCandies / (float)player2MaxBoosterCandies;

            switch (gamePlayManager.matchtype)
            {
                case MatchType.TwoPlayer:



                    if (gamePlayManager.LocalPlayer != null)
                    {
                        localPlayerBoosterCandies = gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterCandies : player2BoosterCandies;
                        otherPlayer2BoosterCandies = !gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterCandies : player2BoosterCandies;

                        localBoosterFillTarget = gamePlayManager.LocalPlayer.isPlayer1 ? localBoosterFillTarget : other2BoosterFillTarget;
                        other2BoosterFillTarget = !gamePlayManager.LocalPlayer.isPlayer1 ? localBoosterFillTarget : other2BoosterFillTarget;
                    }

#if UNITY_EDITOR
                    twoPlayerDetails[0].boosterBtn.interactable = true;
#elif !UNITY_EDITOR
                    twoPlayerDetails[0].boosterBtn.interactable = localBoosterFillTarget >= 1;
#endif
                    twoPlayerDetails[1].boosterBtn.interactable = other2BoosterFillTarget >= 1;

                    ///ACTIVATING BOOOSTER HERE INSTED OF IN UPDATE
                    twoPlayerDetails[0].boosterVFX.SetActive(twoPlayerDetails[0].boosterBtn.interactable == true);
                    twoPlayerDetails[1].boosterVFX.SetActive(twoPlayerDetails[0].boosterBtn.interactable == true);

                    for (int i = 1; i < twoPlayerDetails[0].playerBoosterFillerCandiesImages.Length + 1; i++)
                    {
                        if (i <= localPlayerBoosterCandies)
                            twoPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = blueCandySprite;
                        else
                            twoPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = grayBlueCandySprite;

                        if (i <= otherPlayer2BoosterCandies)
                            twoPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = redCandySprite;
                        else
                            twoPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = grayRedCandySprite;
                    }
                    break;

                case MatchType.FourPlayer:

                    int otherPlayer3BoosterCandies = player3BoosterCandies;
                    int otherPlayer4BoosterCandies = player4BoosterCandies;

                    other3BoosterFillTarget = player3BoosterCandies / (float)player3MaxBoosterCandies;
                    other4BoosterFillTarget = player4BoosterCandies / (float)player4MaxBoosterCandies;

                    /*#if UNITY_EDITOR
                                        fourPlayerDetails[0].boosterBtn.interactable = true;
                    #elif !UNITY_EDITOR
                                        fourPlayerDetails[0].boosterBtn.interactable = localBoosterFillTarget >= 1;
                    #endif*/

                    fourPlayerDetails[0].boosterBtn.interactable = localBoosterFillTarget >= 1;
                    fourPlayerDetails[1].boosterBtn.interactable = other2BoosterFillTarget >= 1;
                    fourPlayerDetails[2].boosterBtn.interactable = other3BoosterFillTarget >= 1;
                    fourPlayerDetails[3].boosterBtn.interactable = other4BoosterFillTarget >= 1;

                    ///ACTIVATING BOOOSTER HERE INSTED OF IN UPDATE
                    fourPlayerDetails[0].boosterVFX.SetActive(fourPlayerDetails[0].boosterBtn.interactable == true);
                    fourPlayerDetails[1].boosterVFX.SetActive(fourPlayerDetails[1].boosterBtn.interactable == true);
                    fourPlayerDetails[2].boosterVFX.SetActive(fourPlayerDetails[2].boosterBtn.interactable == true);
                    fourPlayerDetails[3].boosterVFX.SetActive(fourPlayerDetails[3].boosterBtn.interactable == true);

                    switch (gamePlayManager.serverteamtype)
                    {
                        case TeamType.TeamA:
                            //ADDED LENGTH 8 SO THAT WE DON'T HAVE TO CATCH ARRAY LENGTH
                            //STARTING LOOP FROM 1 SO +1
                            for (int i = 1; i < 8; i++)
                            {
                                if (i <= localPlayerBoosterCandies)
                                    fourPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = blueCandySprite;
                                else
                                    fourPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = grayBlueCandySprite;

                                if (i <= otherPlayer2BoosterCandies)
                                    fourPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = blueCandySprite;
                                else
                                    fourPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = grayBlueCandySprite;

                                if (i <= otherPlayer3BoosterCandies)
                                    fourPlayerDetails[2].playerBoosterFillerCandiesImages[i - 1].sprite = redCandySprite;
                                else
                                    fourPlayerDetails[2].playerBoosterFillerCandiesImages[i - 1].sprite = grayRedCandySprite;

                                if (i <= otherPlayer4BoosterCandies)
                                    fourPlayerDetails[3].playerBoosterFillerCandiesImages[i - 1].sprite = redCandySprite;
                                else
                                    fourPlayerDetails[3].playerBoosterFillerCandiesImages[i - 1].sprite = grayRedCandySprite;
                            }
                            break;

                        case TeamType.TeamB:
                            //ADDED LENGTH 8 SO THAT WE DON'T HAVE TO CATCH ARRAY LENGTH
                            //STARTING LOOP FROM 1 SO +1
                            for (int i = 1; i < 8; i++)
                            {
                                if (i <= localPlayerBoosterCandies)
                                    fourPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = redCandySprite;
                                else
                                    fourPlayerDetails[0].playerBoosterFillerCandiesImages[i - 1].sprite = grayRedCandySprite;

                                if (i <= otherPlayer2BoosterCandies)
                                    fourPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = redCandySprite;
                                else
                                    fourPlayerDetails[1].playerBoosterFillerCandiesImages[i - 1].sprite = grayRedCandySprite;

                                if (i <= otherPlayer3BoosterCandies)
                                    fourPlayerDetails[2].playerBoosterFillerCandiesImages[i - 1].sprite = blueCandySprite;
                                else
                                    fourPlayerDetails[2].playerBoosterFillerCandiesImages[i - 1].sprite = grayBlueCandySprite;

                                if (i <= otherPlayer4BoosterCandies)
                                    fourPlayerDetails[3].playerBoosterFillerCandiesImages[i - 1].sprite = blueCandySprite;
                                else
                                    fourPlayerDetails[3].playerBoosterFillerCandiesImages[i - 1].sprite = grayBlueCandySprite;
                            }
                            break;
                    }



                    break;
            }

            /*float player1BoosterFillTarget = player1BoosterCandies / (float)player1MaxBoosterCandies;
        float player2BoosterFillTarget = player2BoosterCandies / (float)player2MaxBoosterCandies;

        int localPlayerBoosterCandies = player1BoosterCandies;
        int otherPlayerBoosterCandies = player2BoosterCandies;

        localBoosterFillTarget = player1BoosterCandies / (float)player1MaxBoosterCandies;
        otherBoosterFillTarget = player2BoosterFillTarget;

        if (gamePlayManager.LocalPlayer != null)
        {
            localPlayerBoosterCandies = gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterCandies : player2BoosterCandies;
            otherPlayerBoosterCandies = !gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterCandies : player2BoosterCandies;

            localBoosterFillTarget = gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterFillTarget : player2BoosterFillTarget;
            otherBoosterFillTarget = !gamePlayManager.LocalPlayer.isPlayer1 ? player1BoosterFillTarget : player2BoosterFillTarget;
        }



#if UNITY_EDITOR
        player1BoosterButton.interactable = true;
#elif !UNITY_EDITOR
        player1BoosterButton.interactable = localBoosterFillTarget >= 1;
#endif

        player2BoosterButton.interactable = otherBoosterFillTarget >= 1;

        Debug.Log("player1BoosterActiveVFX Status  " + player1BoosterButton.interactable + "    " + localBoosterFillTarget);
        Debug.Log("player2BoosterActiveVFX Status  " + player2BoosterButton.interactable + "    " + otherBoosterFillTarget);

        ///ACTIVATING BOOOSTER HERE INSTED OF IN UPDATE
        player1BoosterActiveVFX.SetActive(player1BoosterButton.interactable == true);
        player2BoosterActiveVFX.SetActive(player2BoosterButton.interactable == true);

        for (int i = 1; i < player1BoosterFillCandiesImages.Length + 1; i++)
        {
            if (i <= localPlayerBoosterCandies)
                player1BoosterFillCandiesImages[i - 1].sprite = blueCandySprite;
            else
                player1BoosterFillCandiesImages[i - 1].sprite = grayBlueCandySprite;

            if (i <= otherPlayerBoosterCandies)
                player2BoosterFillCandiesImages[i - 1].sprite = redCandySprite;
            else
                player2BoosterFillCandiesImages[i - 1].sprite = grayRedCandySprite;
        }*/
        }));
    }

    [ClientCallback]
    private void OnPlayerUsedPerk(bool isPlayer1Perk, bool isPerk1)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            if (isPlayer1Perk == gamePlayManager.LocalPlayer.isPlayer1)
            {
                if (isPerk1)
                {
                    //perk1Button.interactable = false;
                    ourPerk1Image.color = usedPerkColor;
                }
                else
                {
                    //perk2Button.interactable = false;
                    ourPerk2Image.color = usedPerkColor;
                }
            }
            else
            {
                if (isPerk1)
                    opponentPerk1Image.color = usedPerkColor;
                else
                    opponentPerk2Image.color = usedPerkColor;
            }
        }));
    }

    [Client]
    public void PerksManager_OnPerksUsedDataRecieved(bool p1p1, bool p1p2, bool p2p1, bool p2p2)
    {
        if (p1p1)
            OnPlayerUsedPerk(true, true);

        if (p1p2)
            OnPlayerUsedPerk(true, false);

        if (p2p1)
            OnPlayerUsedPerk(false, true);

        if (p2p2)
            OnPlayerUsedPerk(false, false);
    }

    #endregion

    #region Perks Management

    private void OnClick_Perk(bool isPerk1)
    {
        gamePlayManager.LocalPlayer.UsePerk(isPerk1);
    }

    // Called from PerksManager
    [ClientCallback]
    public void OnPerkSuccessfullyUsed(bool isPlayer1Perk, bool isPerk1, BoosterAndPerkItem perk)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            bool isMyPerk = gamePlayManager.LocalPlayer.isPlayer1 == isPlayer1Perk;

            Button button = isPerk1 ? perk1Button : perk2Button;

            //if (isMyPerk) button.onClick.RemoveAllListeners();

            if (perk.isPerkCancellable)
            {
                GameObject perkHighlightVFX = isMyPerk ? (isPerk1 ? ourPerk1HighlightVFX : ourPerk2HighlightVFX) : (isPerk1 ? opponentPerk1HighlightVFX : opponentPerk2HighlightVFX);
                perkHighlightVFX.SetActive(true);

                if (isMyPerk)
                    button.onClick.AddListener(() =>
                    {
                        perkHighlightVFX.SetActive(false);
                        perksManager.Cmd_CancelPerk();
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => OnClick_Perk(isPerk1));
                    });
            }
        }));
    }

    //Called From PerksManager, only called on other player
    [ClientCallback]
    public void HidePerksHighlight(bool isPlayer1Perk, bool isPerk1)
    {
        StartCoroutine(WaitForLocalPlayer(() =>
        {
            bool isMyPerk = gamePlayManager.LocalPlayer.isPlayer1 == isPlayer1Perk;
            GameObject perkHighlightVFX = isMyPerk ? (isPerk1 ? ourPerk1HighlightVFX : ourPerk2HighlightVFX) : (isPerk1 ? opponentPerk1HighlightVFX : opponentPerk2HighlightVFX);
            perkHighlightVFX.SetActive(false);
        }));

    }

    #endregion

    #region Helper Functions

    public Sprite GetAvatarSpriteOfOurPlayer()
    {
        return twoPlayerDetails[0].playerAvtarImage.sprite;
    }

    public Sprite GetAvatarSpriteOfOtherPlayer()
    {
        return twoPlayerDetails[1].playerAvtarImage.sprite;
    }

    public string GetOurPlayerName()
    {
        return twoPlayerDetails[0].playerNameText.text;
    }

    public string GetOtherPlayerName()
    {
        return twoPlayerDetails[1].playerNameText.text;
    }

    IEnumerator WaitForLocalPlayer(Action callback)
    {
        yield return new WaitWhile(() => gamePlayManager.LocalPlayer == null);
        callback();
    }
    #endregion
}

#region SERIALIZABLE CLASS
[Serializable]
public class PlayersInGameUiDetails
{
    public int indexid;
    public Image playerAvtarImage;
    public TextMeshProUGUI playerNameText;
    public GameObject playerTurnHighlighter;

    [Space]
    public Button boosterBtn;
    public Image boosterImage;
    public GameObject boosterVFX;
    [Space]
    public Image[] playerBoosterFillerCandiesImages;

    [Space]
    public List<GameObject> moveCounterObjects;

    [Space]
    public Image perk1Image;
    public GameObject perk1HighlightVFX;

    [Space]
    public Image perk2Image;
    public GameObject perk2HighlightVFX;
}
#endregion
