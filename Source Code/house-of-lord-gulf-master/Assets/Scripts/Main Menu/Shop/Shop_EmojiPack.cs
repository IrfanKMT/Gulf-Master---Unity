using PlayFab;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class Shop_EmojiPack : MonoBehaviour
{
    public static Shop_EmojiPack manager;

    [SerializeField] Button emojiBuyButton;
    [SerializeField] int emojiPackPrice;
    [SerializeField] string emojiPackName;
    [SerializeField] Sprite emojiPackSprite;

    internal bool isEmojiPackBought = false;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        AuthenticationManager.manager.OnPlayerLoggedIn += OnPlayerLoggedIn_CheckEmojiBoughtData;
    }

    private void OnPlayerLoggedIn_CheckEmojiBoughtData()
    {
        var getEmojiBoughtDataRequest = new GetUserDataRequest {Keys = new() { PlayfabDataKeys.PlayerEmojiPackBoughtData }};
        PlayFabClientAPI.GetUserData(getEmojiBoughtDataRequest, res =>
        {
            if (res.Data.ContainsKey(PlayfabDataKeys.PlayerEmojiPackBoughtData))
            {
                emojiBuyButton.interactable = false;
                isEmojiPackBought = true;
            }
        }, err => Debug.LogError("Error in Getting Emoji Bought data in Playfab. \nError Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));
    }

    public void OnClick_BuyEmojiPack()
    {
        ShopUIManager.manager.SetupBuyItemWindow(emojiPackName, emojiPackSprite, emojiPackPrice, () =>BuyEmojiPack("BC"), ()=>BuyEmojiPack("PC"));
    }

    private void BuyEmojiPack(string currencyCode)
    {
        bool canSpend = CurrencyManager.manager.CanSpend(emojiPackPrice, currencyCode);

        if (canSpend)
        {
            CurrencyManager.manager.SubtractUserVirtualCurency(currencyCode, emojiPackPrice,
            () =>
            {
                ShopUIManager.manager.OnPurchaseSuccessful($"You have successfully purchased Emoji Pack for {emojiPackPrice} {currencyCode}.");
                emojiBuyButton.interactable = false;
                isEmojiPackBought = true;
                var emojiPackDataSetRequest = new UpdateUserDataRequest{Data = new() { { PlayfabDataKeys.PlayerEmojiPackBoughtData, true.ToString() } }};
                PlayFabClientAPI.UpdateUserData(emojiPackDataSetRequest, (res) => Debug.Log("Emoji Pack Bought Playfab Data Updated"), err => Debug.LogError("Error in setting bought emoji pack data in Playfab. \nError Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));

            },(error) => ShopUIManager.manager.OnPurchaseFailed($"Failed to deduct {emojiPackPrice} coins.\nError : " + error));
        }
        else
        {
            ShopUIManager.manager.OnPurchaseFailed($"Failed to deduct {emojiPackPrice} coins for Emoji Pack.\nError : Not Enough Funds Available");
        }
    }

}
