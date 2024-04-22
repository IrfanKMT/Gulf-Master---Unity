using System;
using System.Collections.Generic;
using UnityEngine;
using FLOBUK.ReceiptValidator;
using UnityEngine.Purchasing;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine.UI;
using SimpleJSON;
using PlayFab;
using PlayFab.ClientModels;
using Newtonsoft.Json;
using Firebase.Database;
using Firebase.Extensions;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager manager;
    [Tooltip("Make sure the list is in increasing order of coins and the gameObject also has Image attached")]
    [SerializeField] Button[] blueCoins;
    [Tooltip("Make sure the list is in increasing order of coins and the gameObject also has Image attached")]
    [SerializeField] Button[] pinkCoins;

    [SerializeField] Button buySubscriptionBtn;
    [SerializeField] Button buySpecialOfferBtn;

    [SerializeField] GameObject successfulPurchaseVFX;

    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;
    private IAppleExtensions m_AppleExtensions;
    private IGooglePlayStoreExtensions m_GoogleExtensions;
    private ConfigurationBuilder m_builder;
    public static bool storeInitialized = false;
    public DatabaseReference DBref;


    [Space]
    public GameObject loadingPanel;

    private Dictionary<string, Button> buttonWithIDs = new();

    public bool IsSubscriptionActive
    {
        get
        {
            return CheckSubscriptionStatus("com.unity3d.subscription.new");
        }
    }
    int purchaseId = -1;

    #region Unity Functions

    void Awake()
    {
        manager = this;
        DBref = FirebaseDatabase.DefaultInstance.RootReference;
    }

    async void Start()
    {
        try
        {
            var options = new InitializationOptions().SetEnvironmentName("production");
            await UnityServices.InitializeAsync(options);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
        }

        if (m_StoreController == null)
            InitializePurchasing();
    }

    #endregion

    #region Initializing IAP

    public void InitializePurchasing()
    {
        if (IsInitialized())
        {
            Debug.Log("Already Initialized");
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct("com.unity3d.subscription.new", ProductType.Subscription, new IDs
            {
                { "com.unity3d.subscription.new", AppleAppStore.Name },
                { "com.unity3d.subscription.new", GooglePlay.Name},
            });

        builder.AddProduct("special_offer", ProductType.Consumable, new IDs
            {
                { "special_offer", AppleAppStore.Name },
                { "special_offer", GooglePlay.Name},
            });

        foreach (ProductIDs item in Enum.GetValues(typeof(ProductIDs)))
        {
            string id = item.ToString();
            builder.AddProduct(id, ProductType.Consumable, new IDs
            {
                { id, AppleAppStore.Name },
                { id, GooglePlay.Name},
            });
        }

        m_builder = builder;
#pragma warning disable CS0618 // Type or member is obsolete
        UnityPurchasing.Initialize(this, builder);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;


#if UNITY_IOS
        m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
        Dictionary<string, string> dict = m_AppleExtensions.GetIntroductoryPriceDictionary();
#endif

        ReceiptValidator.Instance.Initialize(m_StoreController, m_builder);
        ReceiptValidator.PurchaseCallback += OnPurchaseResult;

        int blueCoinIndex = 0;
        int pinkCoinIndex = 0;

        foreach(var product in controller.products.all)
        {
            if (product.definition.id.StartsWith("bc"))
            {
                blueCoins[blueCoinIndex].onClick.AddListener(()=>PurchaseProduct(product.definition.id));
                buttonWithIDs.Add(product.definition.id, blueCoins[blueCoinIndex]);
                blueCoinIndex++;
            }
          /*  else if (product.definition.id.StartsWith("pc"))
            {
                pinkCoins[pinkCoinIndex].onClick.AddListener(() => PurchaseProduct(product.definition.id));
                buttonWithIDs.Add(product.definition.id, pinkCoins[pinkCoinIndex]);
                pinkCoinIndex++;
            }*/
            else if (product.definition.id.Contains("subscription"))
            {
                buySubscriptionBtn.onClick.AddListener(() => PurchaseProduct(product.definition.id));
            }
            else if (product.definition.id.Contains("special_offer"))
            {
                buySpecialOfferBtn.onClick.AddListener(() => PurchaseProduct(product.definition.id));
            }
        }
        storeInitialized = true;

        Debug.Log("IAP Initialized: Subscription is " + (IsSubscriptionActive ? "active" : "inactive"));
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("IAP Initialization Failed.\nError : " + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError("IAP Initialization Failed.\nError : " + message + "\nReason :" + error);
    }

    #endregion

    #region Initialize Purchase

    public void PurchaseProduct(string productId)
    {
        Debug.Log(productId);

        loadingPanel.SetActive(true);

        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                //if (product.definition.id.StartsWith("bc") || product.definition.id.StartsWith("pc"))
                if (product.definition.id.StartsWith("bc"))
                {
                    purchaseId = -1;
                    OnPurchaseInitiated_InitializeDatabase(id =>
                    {
                        Debug.Log("AddPurchasedItemsInfoToFirebaseDatabase");
                        purchaseId = id;
                        Debug.Log(id);
                        AddPurchasedItemsInfoToFirebaseDatabase(productId, product.metadata.localizedPrice);
                        m_StoreController.InitiatePurchase(product);
                    });
                }
                else
                {

                    m_StoreController.InitiatePurchase(product);
                }
            }
            else
            {
                loadingPanel.SetActive(false);
                Debug.LogError("Purchase Product Error : \nNot purchasing product, either is not found or is not available for purchase.");
            }
        }
        else
        {
            loadingPanel.SetActive(false);
            Debug.LogError("Purchase Product Error : \nBuyProductID FAIL. Not initialized.");
        }
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log("ProcessPurchase");
        Debug.Log(args.purchasedProduct);

        PurchaseState state = ReceiptValidator.Instance.RequestPurchase(args.purchasedProduct);

        Debug.Log(state);

        if (state == PurchaseState.Pending)
        {
            return PurchaseProcessingResult.Pending;
        }
        else if (state == PurchaseState.Purchased)
        {
            OnPurchaseResult(true, args.purchasedProduct.definition.id);
            Debug.Log("Purchase Validated From Unity IAP");
        }

        return PurchaseProcessingResult.Complete;
    }

    private void OnPurchaseResult(bool success, string data)
    {
        loadingPanel.SetActive(false);
        Debug.Log("OnPurchaseResult");
        Debug.Log(success);
        Debug.Log(data);
        //string id = data["data"]["productId"];
        string id = data;
        if (success)
        {
            Debug.Log("Purchase Validated From Receipt Validator");

            if (id.Contains("subscription"))
            {
                ShopUIManager.manager.OnPurchaseSuccessful("You have successfully purchased the subscription, Enjoy the new features!");
            }
            else if (id.Contains("special_offer"))
            {
                ShopManager.SaveSpecialOfferRecievedData();
            }
            // else if (id.StartsWith("bc") || id.StartsWith("pc"))
            else if (id.StartsWith("bc"))
            {
                UpdatePurchasedItemsStatusToFirebaseDatabase(2);

                string coinCode = id.Substring(0, 2).ToUpper();
                if (int.TryParse(id.Remove(0, 3), out int amount))
                {
                    ShopManager.OnCoinPurchaseSuccessful(coinCode, amount);
                    Destroy(Instantiate(successfulPurchaseVFX, buttonWithIDs[id].transform), 5);
                }
                else
                    Debug.LogError($"Coin Purchase Error : \nUnable to parse coin amount from {id}");
            }
            else
            {
                ShopUIManager.manager.OnPurchaseSuccessful($"You have purchased an unkown item, Please contact the development team!");
            }
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        loadingPanel.SetActive(false);
        Debug.LogError($"Item [{product.definition.id}] Purchase Failed.\nReason : " + failureReason);
        ShopUIManager.manager.OnPurchaseFailed($"Item {product.definition.id} can not be purchased.\nReason : {failureReason}\nPlease try again later.");

        if(product.definition.id.StartsWith("bc") || product.definition.id.StartsWith("pc"))
            UpdatePurchasedItemsStatusToFirebaseDatabase(1);
    }

    #endregion

    #region Purchase ID

    /// <summary>
    /// Returns the ID of the purchase and increase the ID in the playfab server
    /// </summary>
    private void OnPurchaseInitiated_InitializeDatabase(Action<int> onSuccess)
    {
        var getPurchaseIDData = new GetUserDataRequest {Keys = new() { PlayfabDataKeys.PlayerPurchaseIDData }};

        PlayFabClientAPI.GetUserData(getPurchaseIDData, (res) =>
        {
            if (res.Data.ContainsKey(PlayfabDataKeys.PlayerPurchaseIDData))
            {
                if (int.TryParse(res.Data[PlayfabDataKeys.PlayerPurchaseIDData].Value, out int id))
                {
                    onSuccess(id);

                    //Increase The ID and then return
                    var setPurchaseIDDataReq = new UpdateUserDataRequest { Data = new() { { PlayfabDataKeys.PlayerPurchaseIDData, (id+1).ToString() } } };
                    PlayFabClientAPI.UpdateUserData(setPurchaseIDDataReq,
                        res => Debug.Log("Playfab Purchase ID Data Succesfully Set to " + (id+1).ToString()),
                        err => Debug.LogError("Error Setting Purchase ID Data In Playfab: Error Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));

                    return;
                }
            }

            var setPurchaseIDDataReq1 = new UpdateUserDataRequest { Data = new() { { PlayfabDataKeys.PlayerPurchaseIDData, "1" } } };
            PlayFabClientAPI.UpdateUserData(setPurchaseIDDataReq1,
                res =>
                {
                    Debug.Log("Playfab Purchase ID Data Succesfully Reset!");
                    onSuccess(0);
                },
                err => Debug.LogError("Error Resetting Purchase ID Data In Playfab: Error Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));
        },
        err => Debug.LogError("Error Getting Purchase ID Data from Playfab: Error Message : " + err.ErrorMessage + "\nReport : " + err.GenerateErrorReport()));
    }

    private void AddPurchasedItemsInfoToFirebaseDatabase(string itemID, decimal price)
    {
        int amount = 1;

        //if (itemID.StartsWith("bc") || itemID.StartsWith("pc"))
            if (itemID.StartsWith("bc"))
                int.TryParse(itemID.Remove(0, 3), out amount);

        ShopItemPurchaseInformation info = new()
        {
            itemID = itemID,
            quantity = amount,
            price = price,
            dateTime = DateTime.UtcNow.ToString(),
            status = 0
        };
        UpdatePaymentDataFirebaseDatabase(info, purchaseId);
    }

    private void UpdatePaymentDataFirebaseDatabase(ShopItemPurchaseInformation info, int purchaseId)
    {
        string jsonData = JsonConvert.SerializeObject(info, Formatting.Indented);

        var DBTask = DBref.Child("PaymentInfo").Child(PlayerData.PlayfabID).Child(purchaseId.ToString()).SetRawJsonValueAsync(jsonData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
                Debug.LogError("Error while adding payment information in Firebase Database: \nError : " + task.Exception + "\nError Message : " + task.Exception.Message + "\n\nStack Trace : " + task.Exception.StackTrace);
            else if (task.IsCompleted)
                Debug.Log("Payment Information In Firebase Database Added!\nData : " + jsonData);
        });
    }

    internal void UpdatePurchasedItemsStatusToFirebaseDatabase(int status)
    {
        FirebaseDatabase dbInstance = FirebaseDatabase.DefaultInstance;
        dbInstance.GetReference("PaymentInfo").Child(PlayerData.PlayfabID).Child(purchaseId.ToString()).GetValueAsync().ContinueWithOnMainThread(DBTask =>
        {
            if (DBTask.IsFaulted)
            {
                Debug.LogError("Error while retrieving payment information from Firebase Database: \nError : " + DBTask.Exception + "\nError Message : " + DBTask.Exception.Message + "\n\nStack Trace : " + DBTask.Exception.StackTrace);
            }

            else if (DBTask.IsCompleted)
            {
                string jsonData = DBTask.Result.GetRawJsonValue();
                Debug.Log(jsonData);
                ShopItemPurchaseInformation info = JsonConvert.DeserializeObject<ShopItemPurchaseInformation>(jsonData);
                info.status = status;
                UpdatePaymentDataFirebaseDatabase(info, purchaseId);
            }
        });
    }

    #endregion

    #region Subscription

    public bool CheckSubscriptionStatus(string productId)
    {

        if (m_StoreController == null || m_StoreController.products == null || m_StoreController.products.all == null)
        {
            Debug.Log("m_StoreController not initialized!");
        }

#if UNITY_IOS
        Dictionary<string, string> dict = m_AppleExtensions.GetIntroductoryPriceDictionary();
#endif
        foreach (Product item in m_StoreController.products.all)
        {
            //Debug.Log(item.receipt);

            if (item.receipt != null)
            {
                Debug.Log("Yes gone in");

                string intro_json = "";
#if UNITY_IOS
                intro_json = (dict == null || !dict.ContainsKey(item.definition.storeSpecificId)) ? null : dict[item.definition.storeSpecificId];
#endif
                if (item.definition.type == ProductType.Subscription)
                {
                    SubscriptionManager p = new SubscriptionManager(item, intro_json);
                    SubscriptionInfo info = p.getSubscriptionInfo();

                    Debug.Log(info.getProductId());
                    Debug.Log(info.getPurchaseDate());
                    Debug.Log(info.getExpireDate());
                    Debug.Log(info.isSubscribed());
                    Debug.Log(info.isExpired());
                    Debug.Log(info.isCancelled());
                    Debug.Log(info.isFreeTrial());
                    Debug.Log(info.isAutoRenewing());
                    Debug.Log(info.getRemainingTime());
                    Debug.Log(info.isIntroductoryPricePeriod());
                    Debug.Log(info.getIntroductoryPrice());
                    Debug.Log(info.getIntroductoryPricePeriod());
                    Debug.Log(info.getIntroductoryPricePeriodCycles());

                    Debug.Log("checked for sub :" + info.getProductId().ToString());
                    if (info.getProductId().ToString() == productId)
                    {
                        return true;
                    }
                }
            }
            else
            {
                //Debug.Log("Recipt Null");
            }
        }

        return false; ;
    }

    #endregion
}

enum ProductIDs
{
    bc_1000,
    bc_5000,
    bc_11000,
    bc_24000,
    bc_50000,
    bc_100000,
  /*  pc_1000,
    pc_5000,
    pc_11000,
    pc_24000,
    pc_50000,
    pc_100000*/
}

[Serializable]
class ShopItemPurchaseInformation
{
    public string itemID;
    public int quantity;
    public decimal price;
    public string dateTime;

    // 0 = initiated, 1 = Failed, 2 = success, 3 = success but item sent failed, 4 = success and item sent
    public int status;
}
