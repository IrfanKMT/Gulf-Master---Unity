using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class BoosterAndPerksData : MonoBehaviour
{
    public static BoosterAndPerksData data;
    public static string randomBoosterId = "bst_random";

    [Header("Boosters")]
    [SerializeField] List<BoosterAndPerkItem> bronzeBoosters = new();
    [SerializeField] List<BoosterAndPerkItem> silverBoosters = new();
    [SerializeField] List<BoosterAndPerkItem> goldBoosters = new();
    [SerializeField] List<BoosterAndPerkItem> diamondBoosters = new();

    [Header("Perks")]
    [SerializeField] List<BoosterAndPerkItem> perks = new();

    private void Awake()
    {
        Debug.Log("BoosterAndPerksData Run");
        data = this;
    }

    public List<BoosterAndPerkItem> GetBoostersAndPerksFromGroup(ItemGroup group)
    {
        if (group == ItemGroup.BronzeBoosters)
            return bronzeBoosters;
        else if (group == ItemGroup.SilverBoosters)
            return silverBoosters;
        else if (group == ItemGroup.GoldBoosters)
            return goldBoosters;
        else if (group == ItemGroup.DiamondBoosters)
            return diamondBoosters;
        else
            return perks;
    }

    public List<BoosterAndPerkItem> GetAllItems()
    {
        List<BoosterAndPerkItem> items = new();
        items.AddRange(bronzeBoosters);
        items.AddRange(silverBoosters);
        items.AddRange(goldBoosters);
        items.AddRange(diamondBoosters);
        items.AddRange(perks);
        return items;
    }

    public BoosterAndPerkItem GetRandomItemFromGroup(ItemGroup group)
    {
        List<BoosterAndPerkItem> groupItems = GetBoostersAndPerksFromGroup(group).ToList();

        List<BoosterAndPerkItem> comingSoonItems = groupItems.Where(i => i.isComingSoon).ToList();
        foreach (var item in comingSoonItems)
            if (groupItems.Contains(item))
                groupItems.Remove(item);

        return groupItems[UnityEngine.Random.Range(0, groupItems.Count)];
    }

    public BoosterAndPerkItem GetRandomItemFromBoosters()
    {
        List<BoosterAndPerkItem> groupItems = GetBoostersAndPerksFromGroup(ItemGroup.BronzeBoosters).ToList();
        groupItems.AddRange(GetBoostersAndPerksFromGroup(ItemGroup.SilverBoosters).ToList());
        groupItems.AddRange(GetBoostersAndPerksFromGroup(ItemGroup.GoldBoosters).ToList());

        List<BoosterAndPerkItem> comingSoonItems = groupItems.Where(i => i.isComingSoon).ToList();
        foreach (var item in comingSoonItems)
            if(groupItems.Contains(item))
                groupItems.Remove(item);

        return groupItems[UnityEngine.Random.Range(0, groupItems.Count)];
    }

    public BoosterAndPerkItem GetBoosterOrPerkFromId(string id)
    {
        var allBoostersAndPerks = GetAllItems();

        foreach (var item in allBoostersAndPerks)
            if (item.itemId.Equals(id))
                return item;

        return null;
    }
}


[Serializable]
public enum ItemGroup
{
    BronzeBoosters,
    SilverBoosters,
    GoldBoosters,
    DiamondBoosters,
    Perks
}
