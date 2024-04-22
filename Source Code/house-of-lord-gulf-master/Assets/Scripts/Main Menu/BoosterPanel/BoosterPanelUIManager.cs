using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class BoosterPanelUIManager : MonoBehaviour
{
    public static BoosterPanelUIManager manager;

    [Header("Booster Holders")]
    [SerializeField] Transform bronzeBoosterHolder;
    [SerializeField] Transform silverBoosterHolder;
    [SerializeField] Transform goldBoosterHolder;
    [SerializeField] Transform gold2BoosterHolder;
    [SerializeField] Transform diamondBoosterHolder;

    [Header("Perks Holder")]
    [SerializeField] Transform perksHolder1;
    [SerializeField] Transform perksHolder2;
    [SerializeField] Transform perksHolder3;

    [Header("Perk Images")]
    [SerializeField] Image perk1Image;
    [SerializeField] Image perk2Image;

    [Header("Prefabs")]
    [SerializeField] GameObject bronzeBoosterPrefab;
    [SerializeField] GameObject silverBoosterPrefab;
    [SerializeField] GameObject goldBoosterPrefab;
    [SerializeField] GameObject diamondBoosterPrefab;
    [SerializeField] GameObject perkPrefab;

    [Header("Panels")]
    [SerializeField] GameObject perkPanel;
    List<BoosterPanelItem> spawnedItems = new();
    Dictionary<string, Button> perkButtons = new(); // Key = Perk ID, Value = Button Component // Used for disable and enabling the interactable property on btns

    // PERK SELECTION
    internal bool isFirstPerkScreenOpen = true;
    internal string perk1ID = "prk_hammer";
    internal string perk2ID = "prk_shuffle";

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        InventoryManager.manager.OnInventoryUpdated += (res)=> InitializeBoosterPanel();
    }

    private void InitializeBoosterPanel()
    {
        perkButtons.Clear();
        foreach (var item in spawnedItems)
            Destroy(item.gameObject);

        spawnedItems.Clear();

        var inventory = InventoryManager.manager.userInventory;
        var itemCatalog = BoosterAndPerksData.data.GetAllItems();

        int goldBoosterSpawned = 0;
        int perkSpawned = 0;
        foreach (var item in itemCatalog)
        {
            Transform holder = item.itemGroup == ItemGroup.BronzeBoosters ? bronzeBoosterHolder : item.itemGroup == ItemGroup.SilverBoosters ? silverBoosterHolder : item.itemGroup == ItemGroup.GoldBoosters ? goldBoosterSpawned < 3 ? goldBoosterHolder : gold2BoosterHolder : item.itemGroup == ItemGroup.DiamondBoosters ? diamondBoosterHolder : perkSpawned < 2 ? perksHolder1 : perkSpawned < 6 ? perksHolder2 : perksHolder3; ;
            GameObject prefab = item.itemGroup == ItemGroup.BronzeBoosters ? bronzeBoosterPrefab : item.itemGroup == ItemGroup.SilverBoosters ? silverBoosterPrefab : item.itemGroup == ItemGroup.GoldBoosters ? goldBoosterPrefab : item.itemGroup == ItemGroup.DiamondBoosters ? diamondBoosterPrefab : perkPrefab;
            int count = 0;

            foreach (var inventoryItem in inventory)
                if (item.itemId.Equals(inventoryItem.Key))
                    count = (int)inventoryItem.Value.RemainingUses;

            BoosterPanelItem spawneditem = Instantiate(prefab, holder).GetComponent<BoosterPanelItem>();
            spawnedItems.Add(spawneditem);

            if (item.isPerk)
                if (!perkButtons.ContainsKey(item.itemId))
                    perkButtons.Add(item.itemId, spawneditem.GetComponent<Button>());

            spawneditem.SetupItem(item, count);
            if (item.itemGroup == ItemGroup.GoldBoosters) goldBoosterSpawned++;
            if (item.isPerk) perkSpawned++;
        }

        perk1Image.sprite = BoosterAndPerksData.data.GetBoosterOrPerkFromId(perk1ID).itemImage;
        perk2Image.sprite = BoosterAndPerksData.data.GetBoosterOrPerkFromId(perk2ID).itemImage;

        foreach (var item in perkButtons)
        {
            if (item.Key == perk1ID || item.Key == perk2ID)
                item.Value.interactable = false;
            else
                item.Value.interactable = true;
        }
    }

    #region Set Perks

    public void SetPerkData(string perkID)
    {
        if (isFirstPerkScreenOpen)
        {
            perk1ID = perkID;
            perk1Image.sprite = BoosterAndPerksData.data.GetBoosterOrPerkFromId(perkID).itemImage;
        }
        else
        {
            perk2ID = perkID;
            perk2Image.sprite = BoosterAndPerksData.data.GetBoosterOrPerkFromId(perkID).itemImage;
        }

        foreach (var item in perkButtons)
        {
            if (item.Key == perk1ID || item.Key == perk2ID)
                item.Value.interactable = false;
            else
                item.Value.interactable = true;
        }
    }

    #endregion

    #region Button Clicks

    public void OnClick_CloseButton()
    {
        UIManager.manager.ClosePanel(UIManager.manager.boosterPanel);
    }

    public void OnClick_ShopButton()
    {
        UIManager.manager.ClosePanel(UIManager.manager.boosterPanel);
        LobbyUIManager.manager.OnClick_Shop();
    }

    public void OnClick_PerkButton(bool isFirstPerk)
    {
        isFirstPerkScreenOpen = isFirstPerk;
        UIAnimationManager.manager.PopUpPanel(perkPanel);
    }

    public void OnClick_PerkCloseButton()
    {
        UIAnimationManager.manager.PopDownPanel(perkPanel);
    }

    #endregion

    #region Booster Panel Item Button Click Handler

    public void OnBoosterPanelItemClicked(BoosterPanelItem boosterPanelItem)
    {
        if (!boosterPanelItem.availableInInventory) return;

        foreach (var item in spawnedItems)
        {
            if (item.isPerk) continue;

            item.gameObject.transform.DOScale(new Vector3(1, 1, 1), 0.25f);
            item.selectButton.gameObject.SetActive(false);
        }

        boosterPanelItem.selectButton.gameObject.SetActive(true);
        boosterPanelItem.gameObject.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.25f);
    }

    #endregion
}
