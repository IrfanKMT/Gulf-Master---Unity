using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class LeaderboardsUIManager : MonoBehaviour
{
    public static LeaderboardsUIManager manager;

    public GameObject leaderboardPanel;

    [SerializeField] int playersToShow = 25;
    [SerializeField] Image countryFlagImage;
    [SerializeField] LeaderboardItem leaderboardItem;

    [Header("Leaderboard Item Holders")]
    [SerializeField] Transform maleLeaderboardItemHolder;
    [SerializeField] Transform femaleLeaderboardItemHolder;

    [Header("Top 3 Players")]
    [SerializeField] List<LeaderboardItem> top3MalePlayers;
    [SerializeField] List<LeaderboardItem> top3FemalePlayers;

    [Header("Random Players From Each Country's Leaderboard")]
    [SerializeField] float updateFrequency = 10;
    [SerializeField] LeaderboardItem bahrainRandomPlayerFromTop20PlayerLeaderboardItem;
    [SerializeField] LeaderboardItem kuwaitRandomPlayerFromTop20PlayerLeaderboardItem;
    [SerializeField] LeaderboardItem uaeRandomPlayerFromTop20PlayerLeaderboardItem;
    [SerializeField] LeaderboardItem saudiRandomPlayerFromTop20PlayerLeaderboardItem;
    [SerializeField] LeaderboardItem qatarRandomPlayerFromTop20PlayerLeaderboardItem;
    [SerializeField] LeaderboardItem omanRandomPlayerFromTop20PlayerLeaderboardItem;

    //Cache Leaderboard Data
    Dictionary<Countries, LeaderboardItem> leaderboardItemsWithTheirCountries = new();
    internal Dictionary<Countries, List<PlayerLeaderboardEntry>> maleLeaderboards = new();
    internal Dictionary<Countries, List<PlayerLeaderboardEntry>> femaleLeaderboards = new();
    bool initialized = false;

    #region Initialize

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        leaderboardItemsWithTheirCountries.Add(Countries.Bahrain, bahrainRandomPlayerFromTop20PlayerLeaderboardItem);
        leaderboardItemsWithTheirCountries.Add(Countries.Kuwait, kuwaitRandomPlayerFromTop20PlayerLeaderboardItem);
        leaderboardItemsWithTheirCountries.Add(Countries.UAE, uaeRandomPlayerFromTop20PlayerLeaderboardItem);
        leaderboardItemsWithTheirCountries.Add(Countries.Saudi, saudiRandomPlayerFromTop20PlayerLeaderboardItem);
        leaderboardItemsWithTheirCountries.Add(Countries.Qatar, qatarRandomPlayerFromTop20PlayerLeaderboardItem);
        leaderboardItemsWithTheirCountries.Add(Countries.Oman, omanRandomPlayerFromTop20PlayerLeaderboardItem);
        AuthenticationManager.manager.OnPlayerLoggedIn += () => InvokeRepeating(nameof(LoadAllLeaderboards), 0, 1800);
    }

    private void LoadAllLeaderboards()
    {
        maleLeaderboards.Clear();
        femaleLeaderboards.Clear();

        LeaderboardsManager.GetLeaderboardData(Countries.Bahrain, true, playersToShow, (res) => maleLeaderboards.Add(Countries.Bahrain,res.Leaderboard), err => { });;
        LeaderboardsManager.GetLeaderboardData(Countries.Bahrain, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.Bahrain,res.Leaderboard), err => { }); ;

        LeaderboardsManager.GetLeaderboardData(Countries.Kuwait, true, playersToShow, (res) => maleLeaderboards.Add(Countries.Kuwait, res.Leaderboard), err => { });
        LeaderboardsManager.GetLeaderboardData(Countries.Kuwait, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.Kuwait, res.Leaderboard), err => { });

        LeaderboardsManager.GetLeaderboardData(Countries.UAE, true, playersToShow, (res) => maleLeaderboards.Add(Countries.UAE, res.Leaderboard), err => { });
        LeaderboardsManager.GetLeaderboardData(Countries.UAE, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.UAE, res.Leaderboard), err => { });

        LeaderboardsManager.GetLeaderboardData(Countries.Saudi, true, playersToShow, (res) => maleLeaderboards.Add(Countries.Saudi, res.Leaderboard), err => { });
        LeaderboardsManager.GetLeaderboardData(Countries.Saudi, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.Saudi, res.Leaderboard), err => { });

        LeaderboardsManager.GetLeaderboardData(Countries.Qatar, true, playersToShow, (res) => maleLeaderboards.Add(Countries.Qatar, res.Leaderboard), err => { });
        LeaderboardsManager.GetLeaderboardData(Countries.Qatar, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.Qatar, res.Leaderboard), err => { });

        LeaderboardsManager.GetLeaderboardData(Countries.Oman, true, playersToShow, (res) => maleLeaderboards.Add(Countries.Oman, res.Leaderboard), err => { });
        LeaderboardsManager.GetLeaderboardData(Countries.Oman, false, playersToShow, (res) => femaleLeaderboards.Add(Countries.Oman, res.Leaderboard), err => { });

        if (!initialized)
        {
            initialized = true;
            StartCoroutine(WaitUntilLeaderboardIsLoaded());
        }
    }

    #endregion

    #region Button Clicks

    public void OnClick_Lobby_LeaderboardButton(int countryIndex)
    {
        Countries country = (Countries)countryIndex;
        LoadLeaderboard(country);
        UIManager.manager.OpenPanel(leaderboardPanel);
    }

    public void OnClick_Leaderboard_Back()
    {
        UIManager.manager.ClosePanel(leaderboardPanel);
    }

    public void OnClick_Leaderboard_Back(System.Action OnClose)
    {
        if (!leaderboardPanel.activeInHierarchy)
        {
            OnClose();
            return;
        }

        UIManager.manager.ClosePanel(leaderboardPanel, OnClose);
    }

    #endregion

    #region Loading Leaderboards List

    private void LoadLeaderboard(Countries country)
    {
        countryFlagImage.sprite = CountryDataReferences.reference.GetCountryLeaderboardFlagFromIndex((int)country);

        foreach (Transform item in maleLeaderboardItemHolder)
            Destroy(item.gameObject);

        foreach (Transform item in femaleLeaderboardItemHolder)
            Destroy(item.gameObject);

        foreach (var item in top3FemalePlayers)
            item.ResetData();

        foreach (var item in top3MalePlayers)
            item.ResetData();

        // Spawn Male Leaderboard
        List<PlayerLeaderboardEntry> leaderboard = maleLeaderboards[country];

        foreach (var item in leaderboard)
        {
            if (item.Position <= 2)
            {
                top3MalePlayers[item.Position].SetupItem(item.PlayFabId, (int)country);
            }
            else
            {
                LeaderboardItem spawnedItem = Instantiate(leaderboardItem.gameObject, maleLeaderboardItemHolder).GetComponent<LeaderboardItem>();
                spawnedItem.SetupItem(item.PlayFabId, (int)country);
            }
        }

        // Spawn Female Leaderboard
        List<PlayerLeaderboardEntry> fem_leaderboard = femaleLeaderboards[country];

        foreach (var item in fem_leaderboard)
        {
            if (item.Position <= 2)
            {
                top3FemalePlayers[item.Position].SetupItem(item.PlayFabId, (int)country);
            }
            else
            {
                LeaderboardItem spawnedItem = Instantiate(leaderboardItem.gameObject, femaleLeaderboardItemHolder).GetComponent<LeaderboardItem>();
                spawnedItem.SetupItem(item.PlayFabId, (int)country);
            }
        }
    }

    #endregion

    #region Loading Random Player From Top 20 Players In Lobby

    IEnumerator WaitUntilLeaderboardIsLoaded()
    {
        yield return new WaitWhile(()=>maleLeaderboards.Count<6 || femaleLeaderboards.Count<6);
        InvokeRepeating(nameof(ShowRandomPlayerOnTopOfEachCountry), 0, updateFrequency);
    }

    private void ShowRandomPlayerOnTopOfEachCountry()
    {
        foreach (Countries country in System.Enum.GetValues(typeof(Countries)))
        {
            if (country == Countries.None) continue;

            int randomGenderSeed = Random.Range(0, 2);
            bool isMalePlayer = randomGenderSeed == 0;

            if (maleLeaderboards[country].Count == 0)
                isMalePlayer = false;

            if (femaleLeaderboards[country].Count == 0)
                isMalePlayer = true;

            List<PlayerLeaderboardEntry> players = isMalePlayer? maleLeaderboards[country] : femaleLeaderboards[country];

            if (players.Count > 10)
                players = players.GetRange(0,10);

            LeaderboardItem randomPlayerFromTop20PlayersItem = leaderboardItemsWithTheirCountries[country];

            if (players.Count == 0)
            {
                randomPlayerFromTop20PlayersItem.ResetData();
                continue;
            }
            else
            {
                PlayerLeaderboardEntry player = players[Random.Range(0, players.Count)];
                randomPlayerFromTop20PlayersItem.SetupItem(player.PlayFabId, (int)country);
            }
        }

    }

    #endregion

    /// <summary>
    /// X value is gender, Y value is index
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetRandomLeaderboardPlayer()
    {
        var malePlayers = maleLeaderboards[(Countries)PlayerData.Country];
        var femalePlayers = femaleLeaderboards[(Countries)PlayerData.Country];

        if(malePlayers.Count > 25)
            malePlayers = malePlayers.GetRange(0, 25);

        if (femalePlayers.Count > 25)
            femalePlayers = femalePlayers.GetRange(0, 25);

        if(malePlayers.Count == 0 && femalePlayers.Count == 0)
        {
            return new(0, 0);
        }
        else if (malePlayers.Count!=0 && femalePlayers.Count == 0)
        {
            return new((int)Gender.Male, Random.Range(0, malePlayers.Count));
        }
        else if (femalePlayers.Count != 0 && malePlayers.Count == 0)
        {
            return new((int)Gender.Female, Random.Range(0, femalePlayers.Count));
        }
        else
        {
            Gender gender = (Gender)Random.Range(1, 3);
            return new((int)gender, Random.Range(0, gender == Gender.Male ? malePlayers.Count : femalePlayers.Count));
        }

    }
}
