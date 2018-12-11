using LightMessage.Common.Util;
using Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour
{
    int? IabPackID = null;
    System.Threading.Tasks.TaskCompletionSource<bool> ConsumeTCS;
    bool displayedAd, waitingToDisplayAd;

    Button VideoAdButton;

    void Awake()
    {
        var IabManager = BazaarIabManager.Instance;
        if (IabManager.GetInventory())
            IabManager.OnGetInventoryResultEvent += IabManager_OnGetInventoryResult;
        else
            Debug.LogError("Failed to get inventory details (is IAB initialized?)");


        var CoinsContainer = transform.Find("Scroll View/Viewport/Content/CoinsContainer");
        var CoinTemplate = CoinsContainer.Find("Template").gameObject;

        VideoAdButton = CoinsContainer.Find("VideoAd").GetComponent<Button>();
        VideoAdButton.gameObject.SetActive(TapsellVideoAdManager.Instance.IsAdAvailable(TapsellVideoAdManager.EZones.AdForCoinReward));
        VideoAdButton.transform.Find("Count").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.VideoAdReward.ToString());

        foreach (var Pack in TransientData.Instance.Packs.Where(p => p.Value.Category.ID == 0).OrderBy(p => p.Value.Sort).Select(p => p.Value))
        {
            var GO = Instantiate(CoinTemplate);
            GO.SetActive(true);

            var Tr = GO.transform;
            Tr.Find("Price").GetComponent<TextMeshProUGUI>().text = StylizePrice(Pack);
            Tr.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Shop/{Pack.ImageID}");
            Tr.Find("Count").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Pack.Contents_Currency.First().Value.ToString());
            Tr.GetComponent<PackButton>().PackID = Pack.ID;
            Tr.SetParent(CoinsContainer, false);
        }

        var GemsContainer = transform.Find("Scroll View/Viewport/Content/GemsContainer");
        var GemTemplate = GemsContainer.Find("Template").gameObject;

        foreach (var Pack in TransientData.Instance.Packs.Where(p => p.Value.Category.ID == 1).OrderBy(p => p.Value.Sort).Select(p => p.Value))
        {
            var GO = Instantiate(GemTemplate);
            GO.SetActive(true);

            var Tr = GO.transform;
            Tr.Find("Price").GetComponent<TextMeshProUGUI>().text = StylizePrice(Pack);
            Tr.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Shop/{Pack.ImageID}");
            Tr.Find("Count").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Pack.Contents_Currency.First().Value.ToString());
            if (!string.IsNullOrEmpty(Pack.Tag))
                Tr.Find("Tag/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Pack.Tag);
            else
                Tr.Find("Tag").gameObject.SetActive(false);
            Tr.GetComponent<PackButton>().PackID = Pack.ID;
            Tr.SetParent(GemsContainer, false);
        }
    }

    void Update()
    {
        if (waitingToDisplayAd)
        {
            VideoAdButton.interactable = false;
            VideoAdButton.gameObject.SetActive(true);
        }
        else if (TapsellVideoAdManager.Instance.IsAdAvailable(TapsellVideoAdManager.EZones.AdForCoinReward) && !displayedAd)
        {
            VideoAdButton.interactable = true;
            VideoAdButton.gameObject.SetActive(true);
        }
        else
        {
            VideoAdButton.interactable = false;
            VideoAdButton.gameObject.SetActive(false);
        }
    }

    public void StartVideoAd()
    {
        displayedAd = true;
        waitingToDisplayAd = true;
        TapsellVideoAdManager.Instance.OnAdFinishedEvent += OnAdFinished;
        TapsellVideoAdManager.Instance.StartAd(TapsellVideoAdManager.EZones.AdForCoinReward);
    }

    async void OnAdFinished(TapsellVideoAdManager.EZones Zone, bool ShouldRewardPlayer, string AdID)
    {
        TapsellVideoAdManager.Instance.OnAdFinishedEvent -= OnAdFinished;
        waitingToDisplayAd = false;
        if (ShouldRewardPlayer)
        {
            await ConnectionManager.Instance.EndPoint<SystemEndPoint>().TakeVideoAdReward(AdID);
            GetComponentInParent<MainMenu>().UpdateCurrencyDisplay();
            DialogBox.Instance.ShowOneButton("مبارکه", $"{TransientData.Instance.VideoAdReward} سکه جایزه گرفتی!", () => { });
        }
    }

    public void GoToPurchaseSection(bool gold)
    {
        transform.Find("Scroll View").GetComponent<ScrollRect>().verticalNormalizedPosition = gold ? 0 : 0.5f;
    }

    async void IabManager_OnGetInventoryResult(ResultCode Code, Dictionary<string, Purchase> OwnedItems)
    {
        if (OwnedItems == null)
            Debug.LogError("Failed to query inventory");
        else
            foreach (var KV in OwnedItems)
            {
                Debug.Log("Have item " + KV.Value.Sku);

                var Pack = TransientData.Instance.Packs.Where(p => p.Value.IabSku == KV.Value.Sku).FirstOrDefault().Value;
                if (Pack == null)
                {
                    Debug.LogWarning("Unknown SKU " + KV.Value.Sku);
                    continue;
                }

                await ProcessIabPuchase(Pack.ID, KV.Value.PurchaseToken, KV.Value.Sku);
            }

        var IabPacks = TransientData.Instance.Packs.Where(p => p.Value.PriceCurrency == CurrencyType.IRR);

        if (IabPacks.First().Value.Price == -1)
        {
            Debug.Log("Getting SKU details");
            var IabManager = BazaarIabManager.Instance;
            IabManager.OnSkuDetailsResultEvent += IabManager_OnSkuDetailsResult;
            IabManager.GetSkuDetails(IabPacks.Select(p => p.Value.IabSku));
        }
    }

    void OnDestroy()
    {
        BazaarIabManager.Instance.OnSkuDetailsResultEvent -= IabManager_OnSkuDetailsResult;
    }

    void IabManager_OnSkuDetailsResult(ResultCode Code, Dictionary<string, SkuDetails> Details)
    {
        if (Code == ResultCode.Success)
        {
            foreach (var KV in Details)
                TransientData.Instance.Packs.Where(p => p.Value.PriceCurrency == CurrencyType.IRR && p.Value.IabSku == KV.Key).FirstOrDefault().Value?.SetPrice(KV.Value.Price);

            var GemsContainer = transform.Find("Scroll View/Viewport/Content/GemsContainer");
            for (int Idx = 0; Idx < GemsContainer.childCount; ++Idx)
            {
                var Tr = GemsContainer.GetChild(Idx);
                Tr.Find("Price").GetComponent<TextMeshProUGUI>().text = StylizePrice(TransientData.Instance.Packs[Tr.GetComponent<PackButton>().PackID]);
            }
        }
        else
        {
            Debug.LogError("Failed to get pack prices from Bazaar, result code " + Code.ToString());
        }
    }

    string StylizePrice(PackConfig Pack)
    {
        return StylizePrice(Pack.Price, Pack.PriceCurrency);
    }

    string StylizePrice(int Price, CurrencyType Currency)
    {
        switch (Currency)
        {
            case CurrencyType.Gem:
                return PersianTextShaper.PersianTextShaper.ShapeText(Price.ToString()) + "<sprite=0>";
            case CurrencyType.Gold:
                return PersianTextShaper.PersianTextShaper.ShapeText(Price.ToString()) + "<sprite=1>";
            case CurrencyType.IRR:
                if (Price == -1)
                    return $"<size=60%>{PersianTextShaper.PersianTextShaper.ShapeText("واستا...")}</size>";
                var PriceText = PersianTextShaper.PersianTextShaper.ShapeText((Price / 10).ToString(), false, true); // Convert IRR to Toman
                if (PriceText.Length > 3)
                    return $"<size=60%>{PersianTextShaper.PersianTextShaper.ShapeText("تومن")}</size>{PriceText.Substring(0, PriceText.Length - 3)}<size=50%>,{PriceText.Substring(PriceText.Length - 3)}</size>";
                else
                    return $"<size=60%>{PersianTextShaper.PersianTextShaper.ShapeText("تومن")}</size>{PriceText}";
            default:
                return "";
        }
    }

    public void MakePurchase(int PackID)
    {
        PackConfig Pack;
        if (!TransientData.Instance.Packs.TryGetValue(PackID, out Pack))
        {
            Debug.LogError("Unknown pack ID " + PackID.ToString());
            return;
        }

        Debug.Log("Purchasing " + PackID);

        if (Pack.PriceCurrency == CurrencyType.IRR)
        {
            PurchaseWithIab(Pack);
            return;
        }

        //?? support for multiple pack contents
        DialogBox.Instance.ShowTwoButton("خرید بسته", $"مطمئنی می‌خوای با {StylizePrice(Pack)}،\n{StylizePrice(Pack.Contents_Currency.First().Value, Pack.Contents_Currency.First().Key)} بخری؟", async () =>
        {
            if (!CurrencyHelpers.HaveEnoughCurrency(Pack.Price, Pack.PriceCurrency))
            {
                Debug.LogWarning("Insufficient funds");
                DialogBox.Instance.ShowOneButton("پول نداری", "برای خریدن این پول نداری،\nباید پولتو بیشتر کنی!", () => GoToPurchaseSection(false));
                return;
            }

            var Result = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().PurchasePack(PackID);
            Debug.Log("Response from server: " + Result.Result.ToString());
            switch (Result.Result)
            {
                case EPurchaseResult.Success:
                    //?? show anim here
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        DialogBox.Instance.ShowOneButton("مبارکه", "خریدت انجام شد. مبارکت باشه!", () => { });
                        TransientData.Instance.UserProfile.ProcessPurchaseResult(Result);
                    });
                    GetComponentInParent<MainMenu>().UpdateCurrencyDisplay(); //??
                    break;

                case EPurchaseResult.InsufficientFunds:
                case EPurchaseResult.AlreadyOwnsItem:
                case EPurchaseResult.InvalidID:
                default:
                    //?? show all kinds of dialogs here
                    break;
            }
        }, null);
    }

    public void PurchaseWithIab(PackConfig Pack)
    {
        var IabManager = BazaarIabManager.Instance;
        if (IabManager.Purchase(Pack.IabSku))
        {
            ConnectionManager.Instance.DelayKeepAlive(System.TimeSpan.FromMinutes(5));
            IabManager.OnPurchaseResultEvent += IabManager_OnPurchaseResult;
            IabPackID = Pack.ID;
        }
        else
            Debug.LogError("Failed to purchase " + Pack.IabSku);
    }

    async void IabManager_OnPurchaseResult(ResultCode Code, Purchase Purchase)
    {
        BazaarIabManager.Instance.OnPurchaseResultEvent -= IabManager_OnPurchaseResult;

        if (Code != ResultCode.Success)
        {
            Debug.LogError("Failed to purchase, result code is " + Code.ToString());
            return;
        }

        Debug.Log($"Purchase successful, verifying with server {Purchase.Sku} {Purchase.PurchaseToken}");

        await ProcessIabPuchase(IabPackID.Value, Purchase.PurchaseToken, Purchase.Sku);

        IabPackID = null;
    }

    async System.Threading.Tasks.Task ProcessIabPuchase(int PackID, string Token, string Sku)
    {
        var PurchaseResult = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().PurchasePackWithIab(PackID, Token);
        Debug.Log("IAB Purchase result: " + PurchaseResult.Result);

        switch (PurchaseResult.Result)
        {
            case EPurchaseResult.Success:
                //?? show dialog/anim here - doesn't necessarily happen in shop menu
                //?? handle case of purchase getting handled outside shop menu (e.g. at app startup)
                TransientData.Instance.UserProfile.ProcessPurchaseResult(PurchaseResult);
                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    DialogBox.Instance.ShowOneButton("مبارکه", "خریدت انجام شد. مبارکت باشه!", () => { });
                    GetComponentInParent<MainMenu>().UpdateCurrencyDisplay();
                });
                BazaarIabManager.Instance.OnConsumeResultEvent += IabManager_OnConsumeResultEvent;
                if (BazaarIabManager.Instance.Consume(Sku))
                {
                    // Need to wait for consume to complete, otherwise we'll get simultaneous operations on the IAB manager
                    ConsumeTCS = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    await ConsumeTCS.Task; // We don't care about the consume operation result, even if it fails the server will have a record of the completed transaction
                }
                break;

            case EPurchaseResult.AlreadyOwnsItem:
            case EPurchaseResult.InsufficientFunds:
            case EPurchaseResult.InvalidID:
            case EPurchaseResult.IabTokenIsInvalid:
            case EPurchaseResult.CannotVerifyIab:
            default:
                //?? show all kinds of dialogs here
                break;
        }

        Debug.Log("IAB Purchase processed");
    }

    private void IabManager_OnConsumeResultEvent(ResultCode Code, Purchase Purchase)
    {
        ConsumeTCS.SetResult(Code == ResultCode.Success);
    }
}
