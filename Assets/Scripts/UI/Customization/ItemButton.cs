using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class ItemButton : MonoBehaviour, IPointerClickHandler
{
    const float MinScale = 0.8f;
    const float MaxScale = 1.0f;


    public int ID { get; private set; }

    RectTransform ViewportTransform;
    RectTransform ScalerTransform;
    CustomizationItemScroller Scroller;
    CustomizationMenu Menu;
    int Index;


    void Start()
    {
        ViewportTransform = transform.parent.parent as RectTransform;
        ScalerTransform = transform.GetChild(0) as RectTransform;
        Scroller = GetComponentInParent<CustomizationItemScroller>();
        Menu = GetComponentInParent<CustomizationMenu>();
    }

    public void Initialize(int ID, int Index)
    {
        this.ID = ID;
        this.Index = Index;
        UpdateDisplay();
    }

    internal void UpdateDisplay()
    {
        var Item = TransientData.Instance.CutomizationItems[ID];
        gameObject.name = Item.ResourceID;
        var HasItem = TransientData.Instance.UserProfile.HasItem(Item.ID);
        transform.Find("Frame/Text").GetComponent<TextMeshProUGUI>().text = HasItem ? PersianTextShaper.PersianTextShaper.ShapeText("قابل انتخاب") : GetItemPriceText(Item);
        (transform.Find("Frame/Image") ?? transform.Find("Frame/Mask/Image")).GetComponent<Image>().sprite = Resources.Load<Sprite>("Customization/Thumbnail/" + Item.ResourceID);
        transform.Find("Frame/LockImage").gameObject.SetActive(!HasItem);
    }

    string GetItemPriceText(CustomizationItemConfig Item)
    {
        return $"{PersianTextShaper.PersianTextShaper.ShapeText(Item.Price.ToString())}<size=120%><sprite={(Item.PriceCurrency == CurrencyType.Gold ? 1 : 0)}></size>";
    }

    void Update()
    {
        if (Scroller.SelectedIndex == Index || Scroller.LastSelectedIndex == Index)
        {
            var OffsetRatio = 0.5f - Mathf.Abs((ScalerTransform.position.x - ViewportTransform.position.x) / ViewportTransform.rect.width);
            ScalerTransform.localScale = Vector3.one * Mathf.SmoothStep(MinScale, MaxScale, Mathf.Clamp01((OffsetRatio - 0.35f) * 7));
        }
        else
        {
            ScalerTransform.localScale = Vector3.one * MinScale;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Scroller.SelectedIndex == Index)
            Menu.PurchaseItem(this);
        else
            Scroller.SetSelection(Index);
    }
}
