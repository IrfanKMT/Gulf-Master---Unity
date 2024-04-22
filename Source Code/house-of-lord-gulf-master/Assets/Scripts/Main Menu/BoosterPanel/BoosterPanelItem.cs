using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using Photon.Pun;

public class BoosterPanelItem : MonoBehaviour
{
    [SerializeField] TMP_Text itemName;
    [SerializeField] Image itemImage;
    [SerializeField] TMP_Text itemCount;
    public Button selectButton;

    string boosterID = "";
    string perkID = "";
    internal bool isPerk = false;
    internal bool availableInInventory = false;

    public void SetupItem(BoosterAndPerkItem data, int count)
    {
        if (itemName != null)
            itemName.text = data.itemName;

        isPerk = data.isPerk;
        itemImage.sprite = data.itemImage;
        itemCount.text = data.isComingSoon ? "Coming Soon!" : InventoryManager.manager.IsItemInInventory(data.itemId) ? count.ToString() : "";

        //Uncomment this
        if (!InventoryManager.manager.IsItemInInventory(data.itemId))
        {
            selectButton.interactable = false;

            if (!data.isPerk)
                selectButton.gameObject.SetActive(false);

            return;
        }

        if (data.isComingSoon)
        {
            selectButton.interactable = false;

            if (!data.isPerk)
                selectButton.gameObject.SetActive(false);

            return;
        }

        availableInInventory = true;

        if (!data.isPerk)
        {
            boosterID = data.itemId;
            selectButton.onClick.AddListener(SelectButton_OnClick_SelectBooster);
        }
        else
        {
            perkID = data.itemId;
            selectButton.onClick.AddListener(SelectButton_OnClick_SelectPerk);
        }

        if(isPerk)
            selectButton.interactable = true;
        else
            selectButton.interactable = false;
    }

    private void Update()
    {
        if (!isPerk && availableInInventory)
            selectButton.interactable = true;
    }

    public void Booster_OnClick_ShowSelectButton()
    {
        SoundManager.manager.Play_ButtonClickSound();
        BoosterPanelUIManager.manager.OnBoosterPanelItemClicked(this);
    }

    private void SelectButton_OnClick_SelectBooster()
    {
        if (string.IsNullOrEmpty(boosterID)) return;
        if (string.IsNullOrEmpty(BoosterPanelUIManager.manager.perk1ID)) return;
        if (string.IsNullOrEmpty(BoosterPanelUIManager.manager.perk2ID)) return;

        SoundManager.manager.Play_ButtonClickSound();
        PlayerData.BoosterID = boosterID;
        PlayerData.Perk1ID = BoosterPanelUIManager.manager.perk1ID;
        PlayerData.Perk2ID = BoosterPanelUIManager.manager.perk2ID;

        if (GameNetworkManager.manager.isLocal)
        {
            GameNetworkManager.manager.StartClient();
        }
        else
        {
            MatchMakingManager.manager.StartMatchMaking();
        }
        UIManager.manager.OpenPanel(UIManager.manager.matchmakingPanel, UIManager.manager.boosterPanel);
    }

    private void SelectButton_OnClick_SelectPerk()
    {
        if (string.IsNullOrEmpty(perkID)) return;

        SoundManager.manager.Play_ButtonClickSound();
        BoosterPanelUIManager.manager.SetPerkData(perkID);
        BoosterPanelUIManager.manager.OnClick_PerkCloseButton();
    }
}
