using BackgammonLogic;
using Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationMenu : MonoBehaviour
{
    public GameObject CheckerPreview;

    Dictionary<CustomizationItemCategory, CustomizationItemScroller> Scrollers = new Dictionary<CustomizationItemCategory, CustomizationItemScroller>();
    SystemEndPoint EndPoint;
    HashSet<int> PreviewItems = new HashSet<int>();
    TextMeshProUGUI AcceptButtonText;

    CheckerDeco CheckerDeco;

    BackgammonLogic.Color Color = BackgammonLogic.Color.White;


    void Awake()
    {
        CheckerDeco = CheckerPreview.GetComponentInChildren<CheckerDeco>();
        CheckerDeco.UpdateDeco(BackgammonLogic.Color.White, TransientData.Instance.UserProfile.ActiveItems);
    }

    void Start()
    {
        EndPoint = ConnectionManager.Instance.EndPoint<SystemEndPoint>();
        foreach (var S in GetComponentsInChildren<CustomizationItemScroller>())
            Scrollers.Add(S.Category, S);

        CheckerDeco.UpdateDeco(Color, PreviewItems);

        AcceptButtonText = transform.Find("AcceptButton").GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnDisable()
    {
        PreviewItems.Clear();
    }

    void Update()
    {
        var Items = new List<int>();

        foreach (var S in Scrollers)
            Items.Add(S.Value.SelectedID);

        if (Items.Except(PreviewItems).Any())
        {
            PreviewItems = new HashSet<int>(Items);
            CheckerDeco.UpdateDeco(Color, Items);
        }

        var Prof = TransientData.Instance.UserProfile;
        AcceptButtonText.text = PersianTextShaper.PersianTextShaper.ShapeText(Items.All(i => Prof.HasItem(i)) ? "تایید" : "خرید");
    }

    public void SwitchCheckerColor()
    {
        Color = Color.Flip();
        CheckerDeco.UpdateDeco(Color, PreviewItems);
    }

    public void PurchaseItem(ItemButton Button)
    {
        var ID = Button.ID;

        if (TransientData.Instance.UserProfile.OwnedItems.ContainsKey(ID))
            return;

        var Item = TransientData.Instance.CutomizationItems[ID];

        DialogBox.Instance.ShowTwoButton("خرید", $"می‌خوای اینو با {Item.Price} {(Item.PriceCurrency == CurrencyType.Gem ? "الماس" : "سکه")} بخری؟",
            async () =>
            {
                if (!CurrencyHelpers.HaveEnoughCurrency(Item.Price, Item.PriceCurrency))
                {
                    DialogBox.Instance.ShowTwoButton("پول نداری",
                        $"{CurrencyHelpers.GetLacking(Item.Price, Item.PriceCurrency)} {(Item.PriceCurrency == CurrencyType.Gem ? "الماس" : "سکه")} برای خرید کم داری.\nبریم فروشگاه بقیشو بگیریم؟",
                        () =>
                        {
                            Close();
                            transform.GetComponentInParent<MainMenu>().SwitchToShopScreenPurchaseSection(Item.PriceCurrency == CurrencyType.Gold);
                        }, () => { }, "بریم", "الان نه");
                    return;
                }

                var Result = await EndPoint.PurchaseCustomizationItem(ID);
                Debug.Log($"Item {ID} purchase result: {Result.Result}");

                switch (Result.Result)
                {
                    case EPurchaseResult.Success:
                        DialogBox.Instance.ShowOneButton("مبارکه", "خریدت انجام شد. مبارکت باشه!", () => { });
                        TransientData.Instance.UserProfile.ProcessPurchaseResult(Result);
                        GetComponentInParent<MainMenu>().UpdateCurrencyDisplay(); //??
                        Button.UpdateDisplay();
                        break;

                    case EPurchaseResult.AlreadyOwnsItem:
                        Button.UpdateDisplay();
                        break;

                    case EPurchaseResult.InsufficientFunds:
                    case EPurchaseResult.InvalidID:
                    default:
                        //?? Show dialogs
                        break;
                }
            }, () => { });
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CheckerDeco.UpdateDeco(BackgammonLogic.Color.White, TransientData.Instance.UserProfile.ActiveItems);
    }

    void PurchaseAll(List<int> Items)
    {
        ulong TotalGold = 0, TotalGems = 0;
        foreach (var ItemID in Items)
        {
            var Item = TransientData.Instance.CutomizationItems[ItemID];
            switch (Item.PriceCurrency)
            {
                case CurrencyType.Gold:
                    TotalGold += (ulong)Item.Price;
                    break;
                case CurrencyType.Gem:
                    TotalGems += (ulong)Item.Price;
                    break;
            }
        }

        string MessageText = "";

        if (TotalGold > 0)
            if (TotalGems > 0)
                MessageText = $"میخوای همه‌ی اینا رو با\n{TotalGems} الماس و {TotalGold} سکه بخری؟";
            else
                MessageText = $"میخوای همه‌ی اینا رو با\n{TotalGold} سکه بخری؟";
        else
            MessageText = $"میخوای همه‌ی اینا رو با\n{TotalGems} الماس بخری؟";

        DialogBox.Instance.ShowTwoButton("خرید", MessageText, async () =>
        {
            var Prof = TransientData.Instance.UserProfile;
            var NeededGold = (long)TotalGold - (long)Prof.Gold;
            var NeededGems = (long)TotalGems - (long)Prof.Gems;

            if (NeededGold > 0)
            {
                if (NeededGems > 0)
                    MessageText = $"{NeededGems} الماس و {NeededGold} سکه کم داری.\nبریم فروشگاه بقیشو بگیریم؟";
                else
                    MessageText = $"{NeededGold} سکه کم داری.\nبریم فروشگاه بقیشو بگیریم؟";
            }
            else
            {
                if (NeededGems > 0)
                    MessageText = $"{NeededGems} الماس کم داری.\nبریم فروشگاه بقیشو بگیریم؟";
                else
                {
                    foreach (var ID in Items)
                    {
                        //?? some sort of please wait... screen
                        var Result = await EndPoint.PurchaseCustomizationItem(ID);
                        Debug.Log($"Item {ID} purchase result: {Result.Result}");

                        switch (Result.Result)
                        {
                            case EPurchaseResult.Success:
                                TransientData.Instance.UserProfile.ProcessPurchaseResult(Result);
                                break;

                            case EPurchaseResult.AlreadyOwnsItem:
                            case EPurchaseResult.InsufficientFunds:
                            case EPurchaseResult.InvalidID:
                            default:
                                //?? Show dialogs
                                break;
                        }
                    }

                    foreach (var button in GetComponentsInChildren<ItemButton>())
                        button.UpdateDisplay();

                    DialogBox.Instance.ShowOneButton("مبارکه", "خریدت انجام شد. مبارکت باشه!", () => { });
                    GetComponentInParent<MainMenu>().UpdateCurrencyDisplay(); //??
                    AcceptChanges();

                    return;
                }
            }

            DialogBox.Instance.ShowTwoButton("پول نداری", MessageText, () =>
            {
                Close();
                transform.GetComponentInParent<MainMenu>().SwitchToShopScreenPurchaseSection(NeededGems == 0);
            }, () => { }, "بریم", "الان نه");
        }, () => { });
    }

    public async void AcceptChanges()
    {
        var ItemsToActivate = new List<int>();

        foreach (var S in Scrollers)
            ItemsToActivate.Add(S.Value.SelectedID);

        var Profile = TransientData.Instance.UserProfile;
        var AllUnownedItems = new List<int>();
        foreach (var I in ItemsToActivate)
            if (!Profile.HasItem(I))
                AllUnownedItems.Add(I);
        if (AllUnownedItems.Any())
        {
            PurchaseAll(AllUnownedItems);
            return;
        }

        var Result = await EndPoint.SetActiveCustomizations(ItemsToActivate);

        Profile.SetActiveItems(Result);
        Close();
    }
}
