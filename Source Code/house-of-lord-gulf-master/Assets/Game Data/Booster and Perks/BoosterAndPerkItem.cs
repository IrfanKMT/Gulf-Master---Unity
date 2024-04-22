using UnityEngine;

[CreateAssetMenu(fileName = "Booster", menuName = "Game/Booster")]
public class BoosterAndPerkItem : ScriptableObject
{
    public Sprite itemImage;
    public string itemName;
    public string itemId;
    public int candiesToActivate = 5;
    public GameObject itemPrefab;
    public ItemGroup itemGroup;
    public bool isPerk = false;
    public bool isPerkCancellable = false; // Can a user click on perk button to not use the perk
    public bool isComingSoon = false;
}