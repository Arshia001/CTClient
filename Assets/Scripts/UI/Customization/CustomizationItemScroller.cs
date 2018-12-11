using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class CustomizationItemScroller : MonoBehaviour, IBeginDragHandler
{
    public CustomizationItemCategory Category;

    public int SelectedID => _Items[SelectedIndex].ID;
    public int SelectedIndex { get; private set; }
    public int LastSelectedIndex { get; private set; }

    ScrollRect ScrollRect;
    List<ItemButton> _Items = new List<ItemButton>();
    bool FirstFrame;


    void Start()
    {
        ScrollRect = GetComponent<ScrollRect>();

        var Container = transform.Find("Viewport/Content");
        var Template = transform.Find("Viewport/Content/Template").gameObject;

        var Prof = TransientData.Instance.UserProfile;
        foreach (var Item in TransientData.Instance.CutomizationItems.Select(kv => kv.Value).Where(i => i.Category == Category && i.IsPurchasable).OrderBy(i => i.Sort))
        {
            var GO = Instantiate(Template);
            GO.transform.SetParent(Container, false);
            GO.SetActive(true);
            var ItemButton = GO.GetComponent<ItemButton>();
            ItemButton.Initialize(Item.ID, _Items.Count);
            if (Prof.ActiveItems.Contains(Item.ID))
                SelectedIndex = _Items.Count;
            _Items.Add(ItemButton);
        }
    }

    void OnEnable()
    {
        FirstFrame = true;
    }

    void Update()
    {
        if (FirstFrame)
            ScrollRect.horizontalNormalizedPosition = SelectedIndex / (float)(_Items.Count - 1);
        else
            ScrollRect.horizontalNormalizedPosition = Mathf.Lerp(ScrollRect.horizontalNormalizedPosition, SelectedIndex / (float)(_Items.Count - 1), Time.deltaTime * 7);

        FirstFrame = false;
    }

    public void MoveSelection(int Count)
    {
        SetSelection(SelectedIndex + Count);
    }

    public void SetSelection(int Index)
    {
        LastSelectedIndex = SelectedIndex;
        SelectedIndex = Mathf.Clamp(Index, 0, _Items.Count - 1);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((eventData.position - eventData.pressPosition).x < 0)
            MoveSelection(1);
        else
            MoveSelection(-1);
    }
}
