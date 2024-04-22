using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager manager;
    [SerializeField] ShopManager shopManager;

    [Header("Animations")]
    [SerializeField] Ease slideOutEase;

    [Header("Catalogs")]
    public GameObject chestAndPerkCatalog;
    public GameObject coinsCatalog; //Used to set in animation manager

    [Header("Purchase Result")]
    [SerializeField] TMP_Text purchaseResultTitleTxt;
    [SerializeField] TMP_Text purchaseResultTxt;
    [SerializeField] GameObject purchaseResultWindow;

    [Header("Buy Item Window")]
    [SerializeField] GameObject buyItemWindow;
    [SerializeField] TMP_Text buyItemTitleTxt;
    [SerializeField] Image buyItemImage;
    [SerializeField] TMP_Text buyItemPriceTxt;
    [SerializeField] Button buyItem_buyBtn_blueCoin;
    [SerializeField] Button buyItem_buyBtn_pinkCoin;

    [Header("Special Offer Window")]
    public GameObject specialOfferWindow;

    [Header("Chests")]
    [SerializeField] Button bronzeChestBtn;
    [SerializeField] Button silverChestBtn;
    [SerializeField] Button goldChestBtn;
    [SerializeField] Button perkChestBtn;

    [Header("VFX")]
    [SerializeField] GameObject boosterChestVFX;

    Dictionary<ShopItem, Button> buttonWithShopItems = new();
    bool chestCatalogOpened = false;
    bool canSwitch = true;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

#if !UNITY_SERVER
    private async void Start()
    {
        bool isSpecialOfferRecieved = await ShopManager.IsSpecialOfferRecieved();

        if (!isSpecialOfferRecieved)
            UIAnimationManager.manager.PopUpPanel(specialOfferWindow);

        buttonWithShopItems.Add(shopManager.bronzeShopItem, bronzeChestBtn);
        buttonWithShopItems.Add(shopManager.silverShopItem, silverChestBtn);
        buttonWithShopItems.Add(shopManager.goldShopItem, goldChestBtn);
        buttonWithShopItems.Add(shopManager.perksShopItem, perkChestBtn);
    }

#endif

    #endregion

    #region OnClick Tab Switching

    public void OnClick_Booster()
    {
        if (!chestCatalogOpened && canSwitch)
        {
            canSwitch = false;
            UIManager.manager.ClosePanel(coinsCatalog, SlideDirection.Right, slideOutEase);
            UIManager.manager.OpenPanel(chestAndPerkCatalog, coinsCatalog, SlideDirection.Right, () => canSwitch = true);
            chestCatalogOpened = true;
        }
    }

    public void OnClick_Coins()
    {
        if (chestCatalogOpened && canSwitch)
        {
            canSwitch = false;
            chestCatalogOpened = false;
            UIManager.manager.OpenPanel(coinsCatalog, chestAndPerkCatalog, SlideDirection.Left, () => canSwitch = true);
            UIManager.manager.ClosePanel(chestAndPerkCatalog, SlideDirection.Left, slideOutEase);
        }
    }


    #endregion

    #region Shop OnClick Items

    public void OnClick_BuyItem_BronzeChest()
    {
        ShopItem item = shopManager.bronzeShopItem;
        SetupBuyItemWindow(item);
    }

    public void OnClick_BuyItem_SilverChest()
    {
        ShopItem item = shopManager.silverShopItem;
        SetupBuyItemWindow(item);
    }

    public void OnClick_BuyItem_GoldChest()
    {
        ShopItem item = shopManager.goldShopItem;
        SetupBuyItemWindow(item);
    }

    public void OnClick_BuyItem_DiamondChest()
    {
        ShopItem item = shopManager.diamondShopItem;
        SetupBuyItemWindow(item);
    }

    public void OnClick_BuyItem_PerksChest()
    {
        ShopItem item = shopManager.perksShopItem;
        SetupBuyItemWindow(item);
    }

    #endregion

    #region Buy Item Window

    private void SetupBuyItemWindow(ShopItem item)
    {
        buyItem_buyBtn_blueCoin.onClick.RemoveAllListeners();
//        buyItem_buyBtn_pinkCoin.onClick.RemoveAllListeners();
        buyItem_buyBtn_blueCoin.interactable = true;
      //  buyItem_buyBtn_pinkCoin.interactable = true;

        buyItemTitleTxt.text = item.itemName;
        buyItemImage.sprite = item.itemSprite;
        buyItemPriceTxt.text = item.price.ToString();
        buyItem_buyBtn_blueCoin.onClick.AddListener(() =>
        {
            buyItem_buyBtn_blueCoin.interactable = false;
            shopManager.PurchaseInGameItem(item, true);
        });
       /* buyItem_buyBtn_pinkCoin.onClick.AddListener(() =>
        {
            buyItem_buyBtn_pinkCoin.interactable = false;
            shopManager.PurchaseInGameItem(item, false);
        });*/
        UIAnimationManager.manager.PopUpPanel(buyItemWindow);
    }

    public void SetupBuyItemWindow(string itemName, Sprite itemSprite, int itemPrice, System.Action onBlueCoinBuyButtonClick, System.Action onPinkCoinBuyButtonClick)
    {
        buyItem_buyBtn_blueCoin.onClick.RemoveAllListeners();
       // buyItem_buyBtn_pinkCoin.onClick.RemoveAllListeners();
        buyItem_buyBtn_blueCoin.interactable = true;
      //  buyItem_buyBtn_pinkCoin.interactable = true;

        buyItemTitleTxt.text = itemName;
        buyItemImage.sprite = itemSprite;
        buyItemPriceTxt.text = itemPrice.ToString();
        buyItem_buyBtn_blueCoin.onClick.AddListener(() =>
        {
            buyItem_buyBtn_blueCoin.interactable = false;
            onBlueCoinBuyButtonClick();
        });
       /* buyItem_buyBtn_pinkCoin.onClick.AddListener(() =>
        {
            buyItem_buyBtn_pinkCoin.interactable = false;
            onPinkCoinBuyButtonClick();
        });*/
        UIAnimationManager.manager.PopUpPanel(buyItemWindow);
    }


    public void OnClick_Close_BuyItemWindow()
    {
        UIAnimationManager.manager.PopDownPanel(buyItemWindow);
    }

    #endregion

    #region Purchase Result Window

    public void OnPurchaseSuccessful(ShopItem item, string text)
    {
        UIAnimationManager.manager.PopDownPanel(buyItemWindow);

        if (buttonWithShopItems.ContainsKey(item))
        {
            Destroy(Instantiate(boosterChestVFX, buttonWithShopItems[item].transform), 5);
        }

        purchaseResultTitleTxt.text = "Purchase Successful";
        purchaseResultTxt.text = text;
        //UIAnimationManager.manager.PopUpPanel(purchaseResultWindow);
    }

    public void OnPurchaseSuccessful(string text)
    {
        purchaseResultTitleTxt.text = "Purchase Successful";
        purchaseResultTxt.text = text;
        UIAnimationManager.manager.PopUpPanel(purchaseResultWindow);
    }

    public void OnPurchaseFailed(string text)
    {
        purchaseResultTitleTxt.text = "Purchase Failed";
        purchaseResultTxt.text = text;
        UIAnimationManager.manager.PopUpPanel(purchaseResultWindow);
    }

    public void PurchaseWindow_OnClick_Close()
    {
        UIAnimationManager.manager.PopDownPanel(purchaseResultWindow);
    }

    #endregion

    #region Special Offer

    public void OnClick_Close_SpecialOffer()
    {
        UIAnimationManager.manager.PopDownPanel(specialOfferWindow);
    }

    #endregion
}
