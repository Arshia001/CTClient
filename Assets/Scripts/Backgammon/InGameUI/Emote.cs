using Backgammon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Emote : MonoBehaviour, IPointerClickHandler
{
    static Dictionary<int, Emote> AllEmotes;


    public static IReadOnlyDictionary<int, Emote> All
    {
        get
        {
            if (AllEmotes == null)
                LoadAll();

            return AllEmotes;
        }
    }


    public static Emote Get(int ID)
    {
        if (AllEmotes == null)
            LoadAll();

        Emote Result;
        if (AllEmotes.TryGetValue(ID, out Result))
            return Result;

        return null;
    }

    static void LoadAll()
    {
        var AllGameObjects = Resources.LoadAll<GameObject>("Emote");
        AllEmotes = new Dictionary<int, Emote>();

        int ID;
        Emote Emote;
        foreach (var GO in AllGameObjects)
            if (int.TryParse(GO.name, out ID) && (Emote = GO.GetComponent<Emote>()) != null)
                AllEmotes.Add(ID, Emote);
    }


    public bool IsSmiley;
    bool IsClickable;

    public int ID { get; set; }


    public void InitializeFor(bool Bubble)
    {
        if (IsSmiley)
        {
            if (Bubble)
                (transform as RectTransform).sizeDelta = new Vector2(48, 48);
            else
                (transform as RectTransform).sizeDelta = new Vector2(35, 35);
        }

        IsClickable = !Bubble;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsClickable)
        {
            GetComponentInParent<EmoteDropdown>().SetOpen(false);
            BackgammonUI.Instance.ShowEmote(ID, true);
            GameManager.Instance.SendEmote(ID);
        }
    }
}
