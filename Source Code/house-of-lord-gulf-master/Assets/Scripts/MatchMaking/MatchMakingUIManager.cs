using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using System;
using System.Linq;
using System.IO;
using PlayFab.MultiplayerAgent.Model;

public class MatchMakingUIManager : MonoBehaviour
{
    public static MatchMakingUIManager manager;

    [Header("Random Player From Top 20 Players")]
    [SerializeField] LeaderboardItem randomPlayerFromTop20PlayersItem;

    [Space]
    public GameObject mode2PlayersPanel;
    public GameObject mode4PlayersPanel;
    [Space]

    /*[Header("Player 1 Panel")]
    [SerializeField] Image player1AvatarImage;
    [SerializeField] TMP_Text player1NameTxt;

    [Header("Player 2 Panel")]
    [SerializeField] Image player2AvatarImage1;
    [SerializeField] Image player2AvatarImage2;
    [SerializeField] TMP_Text player2NameTxt;*/
    [SerializeField] RectTransform playerAvatarImageHolder;

    [Space]
    public List<MultiplePlayersData> fourPlayerData;
    [Space]
    public List<MultiplePlayersData> twoPlayerData;

    [Header("Buttons")]
    [SerializeField] Button leaveButton;

    [Header("Timer")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] float timeToWaitAfterAMatchIsFound = 2;

    [Header("Random People Images")]
    [SerializeField] List<Sprite> randomPeopleSprites;
    [SerializeField] float movingSpeed = 1;
    [SerializeField] float slowMovingSpeed = 1;

    [Header("Searching Lens")]
    [SerializeField] GameObject searchingLensGO;

    [Header("Sounds")]
    [SerializeField] AudioClip matchMakingBGSound;
    [SerializeField] AudioClip opponentFoundSound;

    public List<GamePlayer> LocalTeam;
    public List<GamePlayer> OpponentTeam;
    [Space]
    public List<TeamAssign> AllPlayers;
    //GamePlayer tempPlayer;
    private GamePlayer localplayer;
   

    IEnumerator flippingRandomPeopleImagesCoroutine;
    string opponentPlayfabID;

    bool gamestart = false;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        leaveButton.interactable = true;

        ProfileManager.manager.OnLoadingNewProfileOrUpdatingProfile += (playfabID) =>
        {
            if (PlayerData.PlayfabID.Equals(playfabID))
                UpdateLocaPlayerData();
        };

        MatchMakingManager.manager.OnMatchMakingStarted += OnMatchMakingStarted;
    }

    #endregion

    #region Event Callbacks
    /// <summary>
    /// Updating text value when panel enables
    /// </summary>
    public void ChangeTimerTextValueToNull()
    {
        timerText.text = "";

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                twoPlayerData[1].playerNameText.text = "Finding...";
                break;
            case MatchType.FourPlayer:
                fourPlayerData[1].playerNameText.text = "Finding...";
                fourPlayerData[2].playerNameText.text = "Finding...";
                fourPlayerData[3].playerNameText.text = "Finding...";
                break;
        }
        //player2NameTxt.text = "Finding...";
    }

    private void UpdateLocaPlayerData()
    {
        //Local player - 2 players
        ProfileFetcher.FetchAndSetAvatarImage(PlayerData.PlayfabID, twoPlayerData[0].avtarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(PlayerData.PlayfabID, twoPlayerData[0].playerNameText);

        //Local player - 4 players
        ProfileFetcher.FetchAndSetAvatarImage(PlayerData.PlayfabID, fourPlayerData[0].avtarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(PlayerData.PlayfabID, fourPlayerData[0].playerNameText);

        //ProfileFetcher.FetchAndSetAvatarImage(PlayerData.PlayfabID, player1AvatarImage);
        //ProfileFetcher.FetchAndSetUserNameWithoutTag(PlayerData.PlayfabID, player1NameTxt);
    }


    //Called from GamePlayer script
    public void OnAllPlayersSpawned()
    {
        gamestart = false;
        //Debug.Log("OnAllPlayersSpawned");
        leaveButton.interactable = false;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.FourPlayer:
                #region ASSIGN TEAM
                AllPlayers = new List<TeamAssign>();

                foreach (var item in players)
                {
                    GamePlayer tempPlayer = item.GetComponent<GamePlayer>();
                    TeamAssign TeamValue = new TeamAssign();

                    TeamValue.Player = tempPlayer;

                    if (tempPlayer.teamName == "team1")
                    {
                        if (tempPlayer.playfabID == PlayerData.PlayfabID)
                        {
                            Debug.Log("My Team is A");
                            localplayer = tempPlayer;
                            MatchMakingManager.manager.MyteamType = TeamType.TeamA;
                        }
                        TeamValue.TeamType = TeamType.TeamA;
                    }
                    else
                    {
                        if (tempPlayer.playfabID == PlayerData.PlayfabID)
                        {
                            Debug.Log("My Team is B");
                            localplayer = tempPlayer;
                            MatchMakingManager.manager.MyteamType = TeamType.TeamB;
                        }
                        TeamValue.TeamType = TeamType.TeamB;
                    }

                    AllPlayers.Add(TeamValue);
                }

                OpponentTeam = new List<GamePlayer>();

                Debug.Log("MyteamType  " + MatchMakingManager.manager.MyteamType);

                switch (MatchMakingManager.manager.MyteamType)
                {
                    case TeamType.TeamA:
                        LocalTeam[0] = localplayer;
                        foreach (var item in AllPlayers)
                        {
                            if (item.TeamType == TeamType.TeamA)
                            {
                                if (item.Player.playfabID != PlayerData.PlayfabID)
                                {
                                    LocalTeam[1] = item.Player;
                                }
                            }
                            else
                            {
                                OpponentTeam.Add(item.Player);
                            }
                        }
                        break;

                    case TeamType.TeamB:
                        LocalTeam[0] = localplayer;
                        foreach (var item in AllPlayers)
                        {
                            if (item.TeamType == TeamType.TeamB)
                            {
                                if (item.Player.playfabID != PlayerData.PlayfabID)
                                {
                                    LocalTeam[1] = item.Player;
                                }
                            }
                            else
                            {
                                OpponentTeam.Add(item.Player);
                            }
                        }
                        break;
                }

                #endregion
                break;
        }

        foreach (var item in players)
        {
            GamePlayer tempPlayer = item.GetComponent<GamePlayer>();

            string itemPlayfabID = tempPlayer.playfabID;

            if (!itemPlayfabID.Equals(PlayerData.PlayfabID))
            {
                opponentPlayfabID = itemPlayfabID;
                if (!gamestart)
                {
                    gamestart = true;
                    StartCoroutine(StartGame(itemPlayfabID));
                }
            }
        }
    }

    //Called from GamePlayer script
    public void OnLeaderboardDataRecieved(int gender, int index)
    {
        if (gender == 0) return;

        LeaderboardsManager.GetLeaderboardData((Countries)PlayerData.Country, (Gender)gender == Gender.Male, 25, (res) =>
        {
            List<PlayerLeaderboardEntry> players = res.Leaderboard;

            if (players.Count == 0)
                randomPlayerFromTop20PlayersItem.ResetData();
            else if (players.Count > index)
                randomPlayerFromTop20PlayersItem.SetupItem(players[index].PlayFabId, PlayerData.Country);
        },
        err => { });
    }

    private void OnMatchMakingStarted()
    {
        leaveButton.interactable = true;

        opponentPlayfabID = "";
        //searchingLensGO.SetActive(true);

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                twoPlayerData[1].searchingImage.SetActive(true);
                twoPlayerData[1].avtarImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];
                break;

            case MatchType.FourPlayer:
                fourPlayerData[1].searchingImage.SetActive(true);
                fourPlayerData[1].avtarImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];

                fourPlayerData[2].searchingImage.SetActive(true);
                fourPlayerData[2].avtarImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];

                fourPlayerData[3].searchingImage.SetActive(true);
                fourPlayerData[3].avtarImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];
                break;
        }


        //Image topImage = playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
        //topImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];

        if (flippingRandomPeopleImagesCoroutine != null)
            StopCoroutine(flippingRandomPeopleImagesCoroutine);

        flippingRandomPeopleImagesCoroutine = Coroutine_SwipeBetweenRandomPeopleImages();
        StartCoroutine(flippingRandomPeopleImagesCoroutine);

        SoundManager.manager.PlayBackgroundMusic(matchMakingBGSound);
    }

    #endregion

    #region Button Clicks

    public void OnClick_LeaveRoom()
    {
        timerText.text = "";
        //searchingLensGO.SetActive(false);
        leaveButton.interactable = false;
        randomPlayerFromTop20PlayersItem.ResetData();

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                mode2PlayersPanel.SetActive(false);
                twoPlayerData[1].searchingImage.SetActive(false);
                break;

            case MatchType.FourPlayer:
                mode4PlayersPanel.SetActive(false);
                fourPlayerData[1].searchingImage.SetActive(false);
                fourPlayerData[2].searchingImage.SetActive(false);
                fourPlayerData[3].searchingImage.SetActive(false);
                break;
        }

        if (flippingRandomPeopleImagesCoroutine != null)
            StopCoroutine(flippingRandomPeopleImagesCoroutine);

        SoundManager.manager.StopBackgroundMusic();
        MatchMakingManager.manager.RemovePlayerFromMatchmakingQueue();
        UIManager.manager.ClosePanel(UIManager.manager.matchmakingPanel, UIManager.manager.boosterPanel, SetMatchmakingDataToDefault);
    }

    #endregion

    #region Helper Functions

    void SetProfileAndText()
    {
        Debug.Log(LocalTeam.Count);
        Debug.Log(OpponentTeam.Count);
        fourPlayerData[1].searchingImage.SetActive(false);
        ProfileFetcher.FetchAndSetAvatarImage(LocalTeam[1].playfabID, fourPlayerData[1].avtarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(LocalTeam[1].playfabID, fourPlayerData[1].playerNameText);

        fourPlayerData[2].searchingImage.SetActive(false);
        ProfileFetcher.FetchAndSetAvatarImage(OpponentTeam[0].playfabID, fourPlayerData[2].avtarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(OpponentTeam[0].playfabID, fourPlayerData[2].playerNameText);


        fourPlayerData[3].searchingImage.SetActive(false);
        ProfileFetcher.FetchAndSetAvatarImage(OpponentTeam[1].playfabID, fourPlayerData[3].avtarImage);
        ProfileFetcher.FetchAndSetUserNameWithoutTag(OpponentTeam[1].playfabID, fourPlayerData[3].playerNameText);
    }

    IEnumerator StartGame(string otherPlayfabID)
    {

        //Debug.Log("StartGame  " + otherPlayfabID);

        yield return new WaitForSeconds(timeToWaitAfterAMatchIsFound);

        SoundManager.manager.StopBackgroundMusic();
        SoundManager.manager.PlaySoundSeperately(opponentFoundSound);

        yield return new WaitForSeconds(1);

        //searchingLensGO.SetActive(false);
        //ProfileFetcher.FetchAndSetUserNameWithoutTag(otherPlayfabID, player2NameTxt);

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                twoPlayerData[1].searchingImage.SetActive(false);
                ProfileFetcher.FetchAndSetUserNameWithoutTag(otherPlayfabID, twoPlayerData[1].playerNameText);
                ProfileFetcher.FetchAndSetAvatarImage(otherPlayfabID, twoPlayerData[1].avtarImage);
                break;

            case MatchType.FourPlayer:
                SetProfileAndText();
                break;
        }


        int timeToWait = 5;
        while (timeToWait > 0)
        {
            Debug.Log(timeToWait);
            yield return new WaitForSeconds(1);
            timerText.text = timeToWait.ToString();
            timeToWait--;
        }
        Debug.Log("Done Here");


        //MatchMaking manager starts the game after a fixed seconds
    }

    IEnumerator Coroutine_SwipeBetweenRandomPeopleImages()
    {
        float timeLeftToMatch = timeToWaitAfterAMatchIsFound; // Wait 2 seconds after finding a match to show the profile pic
        bool stop = false;

        Image tempImage = new GameObject("MatchMaking Temp Image Holder", typeof(Image)).GetComponent<Image>();
        bool settedProfileImg = false;

        tempImage.sprite = SpriteReferences.references.defaultAvatarSprite;
        tempImage.color = new(1, 1, 1, 0);

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:

                twoPlayerData[1].playerAvatarImageHolder.localPosition = new(0, 0, 0);

                while (true)
                {
                    if (twoPlayerData[1].playerAvatarImageHolder.localPosition.y <= -220)
                    {
                        if (stop && timeLeftToMatch <= 0)
                        {
                            flippingRandomPeopleImagesCoroutine = null;
                            Destroy(tempImage.gameObject);
                            yield break;
                        }

                        if (timeLeftToMatch > 0)
                        {
                            Image topImage = twoPlayerData[1].playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                            Image bottomImage = twoPlayerData[1].playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                            bottomImage.sprite = topImage.sprite;
                            topImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];
                        }
                        else
                        {
                            Image topImage = twoPlayerData[1].playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                            Image bottomImage = twoPlayerData[1].playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                            bottomImage.sprite = topImage.sprite;
                            topImage.sprite = tempImage.sprite;
                            stop = true;
                        }

                        twoPlayerData[1].playerAvatarImageHolder.localPosition = new(0, 0, 0);
                    }

                    if (!string.IsNullOrEmpty(opponentPlayfabID))
                    {
                        timeLeftToMatch -= Time.deltaTime;

                        if (!settedProfileImg)
                        {
                            ProfileFetcher.FetchAndSetAvatarImage(opponentPlayfabID, tempImage);
                            settedProfileImg = true;
                        }
                    }
                    twoPlayerData[1].playerAvatarImageHolder.localPosition = Vector3.MoveTowards(twoPlayerData[1].playerAvatarImageHolder.localPosition, new(twoPlayerData[1].playerAvatarImageHolder.localPosition.x, -220, twoPlayerData[1].playerAvatarImageHolder.localPosition.z), Time.deltaTime * (stop ? slowMovingSpeed : movingSpeed));
                    yield return null;
                }

            case MatchType.FourPlayer:

                for (int i = 1; i < fourPlayerData.Count; i++)
                {
                    fourPlayerData[i].playerAvatarImageHolder.localPosition = new(0, 0, 0);
                }
                while (true)
                {
                    for (int i = 1; i < fourPlayerData.Count; i++)
                    {
                        if (fourPlayerData[i].playerAvatarImageHolder.localPosition.y <= -220)
                        {
                            if (stop && timeLeftToMatch <= 0)
                            {
                                flippingRandomPeopleImagesCoroutine = null;
                                Destroy(tempImage.gameObject);
                                yield break;
                            }

                            if (timeLeftToMatch > 0)
                            {
                                Image topImage = fourPlayerData[i].playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                                Image bottomImage = fourPlayerData[i].playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                                bottomImage.sprite = topImage.sprite;
                                topImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];
                            }
                            else
                            {
                                Image topImage = fourPlayerData[i].playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                                Image bottomImage = fourPlayerData[i].playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                                bottomImage.sprite = topImage.sprite;
                                topImage.sprite = tempImage.sprite;
                                stop = true;
                            }
                            fourPlayerData[i].playerAvatarImageHolder.localPosition = new(0, 0, 0);
                        }

                        if (!string.IsNullOrEmpty(opponentPlayfabID))
                        {
                            timeLeftToMatch -= Time.deltaTime;

                            if (!settedProfileImg)
                            {
                                ProfileFetcher.FetchAndSetAvatarImage(opponentPlayfabID, tempImage);
                                settedProfileImg = true;
                            }
                        }
                        fourPlayerData[i].playerAvatarImageHolder.localPosition = Vector3.MoveTowards(fourPlayerData[i].playerAvatarImageHolder.localPosition, new(fourPlayerData[i].playerAvatarImageHolder.localPosition.x, -220, fourPlayerData[i].playerAvatarImageHolder.localPosition.z), Time.deltaTime * (stop ? slowMovingSpeed *2f : movingSpeed * 2f));
                        yield return null;
                    }
                }
        }

        /*playerAvatarImageHolder.localPosition = new(0, 0, 0);

        while (true)
        {
            if (playerAvatarImageHolder.localPosition.y <= -220)
            {
                if (stop && timeLeftToMatch <= 0)
                {
                    flippingRandomPeopleImagesCoroutine = null;
                    Destroy(tempImage.gameObject);
                    yield break;
                }

                if (timeLeftToMatch > 0)
                {
                    Image topImage = playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                    Image bottomImage = playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                    bottomImage.sprite = topImage.sprite;
                    topImage.sprite = randomPeopleSprites[UnityEngine.Random.Range(0, randomPeopleSprites.Count)];
                }
                else
                {
                    Image topImage = playerAvatarImageHolder.GetChild(0).GetComponent<Image>();
                    Image bottomImage = playerAvatarImageHolder.GetChild(1).GetComponent<Image>();
                    bottomImage.sprite = topImage.sprite;
                    topImage.sprite = tempImage.sprite;
                    stop = true;
                }

                playerAvatarImageHolder.localPosition = new(0, 0, 0);
            }

            if (!string.IsNullOrEmpty(opponentPlayfabID))
            {
                timeLeftToMatch -= Time.deltaTime;

                if (!settedProfileImg)
                {
                    ProfileFetcher.FetchAndSetAvatarImage(opponentPlayfabID, tempImage);
                    settedProfileImg = true;
                }
            }

            playerAvatarImageHolder.localPosition = Vector3.MoveTowards(playerAvatarImageHolder.localPosition, new(playerAvatarImageHolder.localPosition.x, -220, playerAvatarImageHolder.localPosition.z), Time.deltaTime * (stop ? slowMovingSpeed : movingSpeed));
            yield return null;
        }*/
    }

    public void SetMatchmakingDataToDefault()
    {
        /*player2NameTxt.text = "Finding...";
        player2AvatarImage1.sprite = SpriteReferences.references.defaultAvatarSprite;
        player2AvatarImage2.sprite = SpriteReferences.references.defaultAvatarSprite;*/

        switch (MatchMakingManager.manager.matchType)
        {
            case MatchType.TwoPlayer:
                twoPlayerData[1].playerNameText.text = "Finding...";
                twoPlayerData[1].avtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
                break;
            case MatchType.FourPlayer:
                fourPlayerData[1].playerNameText.text = "Finding...";
                fourPlayerData[1].avtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;

                fourPlayerData[2].playerNameText.text = "Finding...";
                fourPlayerData[2].avtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;

                fourPlayerData[3].playerNameText.text = "Finding...";
                fourPlayerData[3].avtarImage.sprite = SpriteReferences.references.defaultAvatarSprite;
                break;
        }
    }

    #endregion
}


[Serializable]
public class MultiplePlayersData
{
    public int playerId;
    public Image avtarImage;
    public RectTransform playerAvatarImageHolder;
    public TextMeshProUGUI playerNameText;
    public GameObject searchingImage;
}

public enum Teams
{
    None,
    TeamA,
    TeamB
}
