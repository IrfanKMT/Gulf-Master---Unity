using UnityEngine;

public class Perk : MonoBehaviour
{
    private System.Random randomGenerator;

    [SerializeField] private bool playSFXInstantly = false;
    [SerializeField] protected AudioClip[] perkSFX;
    private BoosterAndPerkItem perkData;

    internal bool isPlayer1Perk;
    internal bool isPerk1;
    internal int seed;
    internal string perkID;

    internal void InitializePerk(int seed, BoosterAndPerkItem item, bool isPlayer1Perk, bool isPerk1)
    {
        randomGenerator = new(seed);
        perkData = item;
        perkID = item.itemId;
        this.seed = seed;
        this.isPlayer1Perk = isPlayer1Perk;
        this.isPerk1 = isPerk1;

        Boost();

        if (playSFXInstantly)
            SoundManager.manager.PlaySoundSeperately(perkSFX[GenerateRandom(0, perkSFX.Length)]);
    }

    protected int GenerateRandom(int minInclusive, int maxExclusive) { return randomGenerator.Next(minInclusive, maxExclusive); }

    public virtual void Boost() {}

    public virtual void CancelPerk()
    {
        if (perkData.isPerkCancellable)
        {
            PerksManager.manager.isPerkWorking = false;
            Destroy(gameObject);
        }
    }
}
