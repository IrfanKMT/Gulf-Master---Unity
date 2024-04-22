using UnityEngine;

public class Booster : MonoBehaviour
{
    [SerializeField] private bool boostInstantly = false;
    [SerializeField] private bool playSoundInstantly = false;
    [SerializeField] internal AudioClip[] boosterSFXs;

    private System.Random randomGenerator;

    internal int seed;
    internal bool isPlayer1Booster;
    internal string boosterID;

    #region Initialization

    public void InitializeBooster(int seed, bool isPlayer1, string id)
    {
        isPlayer1Booster = isPlayer1;
        this.seed = seed;
        randomGenerator = new(seed);

        boosterID = id;

        if(boostInstantly)
            Boost();

        if (playSoundInstantly)
            SoundManager.manager.PlaySoundSeperately(boosterSFXs[GenerateRandom(0, boosterSFXs.Length)]);

        Init();
    }

    #endregion

    #region Helper Functions

    protected int GenerateRandom(int minInclusive, int maxExclusive)
    {
        return randomGenerator.Next(minInclusive, maxExclusive);
    }

    #endregion

    #region Virtual Functions

    /// <summary>
    /// Used as start function for sub/child classes
    /// </summary>
    public virtual void Init() { }

    /// <summary>
    /// Callled via Animation Event
    /// </summary>
    public virtual void Boost() { }

    #endregion
}
