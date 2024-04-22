using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/ShopItem")]
public class ShopItem : ScriptableObject
{
    [Header("Item Details")]
    public string itemName;
    public Sprite itemSprite;
    public int price;
    public bool isIAPItem;

    [Header("Payouts")]
    public RandomizedItem payout;
}

[System.Serializable]
public struct RandomizedItem
{
    public ItemGroup group;
    public int amount;

    [HideInInspector]
    internal string[] IDs
    {
        get
        {
            List<BoosterAndPerkItem> items = BoosterAndPerksData.data.GetBoostersAndPerksFromGroup(group).ToList();
            List<string> IDs = new();
            foreach (var item in items)
                IDs.Add(item.itemId);

            List<string> returnValue = new();

            if(group == ItemGroup.Perks)
            {
                IDs.Remove("prk_shuffle");
                IDs.Remove("prk_hammer");
            }

            for (int i = 0; i < amount; i++)
            {
                if (group != ItemGroup.Perks)
                {
                    returnValue.Add(IDs[Random.Range(0, IDs.Count)]);
                }
                else
                {
                    int random = Random.Range(0, IDs.Count);

                    // Add each perk 5 times
                    returnValue.Add(IDs[random]);
                    returnValue.Add(IDs[random]);
                    returnValue.Add(IDs[random]);
                    returnValue.Add(IDs[random]);
                    returnValue.Add(IDs[random]);

                    IDs.RemoveAt(random);
                }
            }

            return returnValue.ToArray();
        }

        private set { }
    }
}


