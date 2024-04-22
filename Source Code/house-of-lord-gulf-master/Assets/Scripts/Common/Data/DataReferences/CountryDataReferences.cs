using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountryDataReferences : MonoBehaviour
{
    public static CountryDataReferences reference;

    [SerializeField] List<Sprite> countries;
    [SerializeField] List<Sprite> countriesLeaderboardFlags;

    private void Awake()
    {
        reference = this;
    }

    public Sprite GetCountryFromIndex(int index)
    {
        return countries[index-1]; // -1 because the first element in the enum is None, while the first element in Countries sprites list is a sprite
    }

    public Sprite GetCountryLeaderboardFlagFromIndex(int index)
    {
        return countriesLeaderboardFlags[index - 1]; // -1 because the first element in the enum is None, while the first element in Countries sprites list is a sprite
    }
}
