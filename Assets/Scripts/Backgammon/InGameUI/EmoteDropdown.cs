using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmoteDropdown : MonoBehaviour
{
    RectTransform ContainerTransform;
    CanvasGroup CanvasGroup;

    bool IsOpen;


    void Start()
    {
        ContainerTransform = transform.Find("Group/Mask/Container") as RectTransform;
        CanvasGroup = transform.Find("Group").GetComponent<CanvasGroup>();

        FillEmotes();
    }

    void FillEmotes()
    {
        var Smilies = new SortedDictionary<int, Emote>();
        var Texts = new SortedDictionary<int, Emote>();

        foreach (var KV in Emote.All)
            if (KV.Value.IsSmiley)
                Smilies.Add(KV.Key, KV.Value);
            else
                Texts.Add(KV.Key, KV.Value);

        var Row = default(GameObject);
        var RowTemplate = ContainerTransform.Find("SmileyRowTemplate").gameObject;
        foreach (var KV in Smilies)
        {
            var GO = Instantiate(KV.Value.gameObject);
            var Emote = GO.GetComponent<Emote>();
            Emote.ID = KV.Key;
            Emote.InitializeFor(false);
            if (Row == null)
            {
                Row = Instantiate(RowTemplate);
                Row.SetActive(true);
                Row.transform.SetParent(ContainerTransform, false);
                GO.transform.SetParent(Row.transform, false);
            }
            else
            {
                GO.transform.SetParent(Row.transform, false);
                Row = null;
            }
        }

        foreach (var KV in Texts)
        {
            var GO = Instantiate(KV.Value.gameObject);
            var Emote = GO.GetComponent<Emote>();
            Emote.ID = KV.Key;
            Emote.InitializeFor(false);
            GO.transform.SetParent(ContainerTransform, false);
        }
    }

    void Update()
    {
        var Pos = ContainerTransform.localPosition;
        Pos.y = Mathf.Lerp(Pos.y, IsOpen ? 22 : 222, Mathf.Clamp01(Time.deltaTime * 10));
        ContainerTransform.localPosition = Pos;
    }

    public void Flip()
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool Open)
    {
        IsOpen = Open;
        CanvasGroup.interactable = IsOpen;
        CanvasGroup.blocksRaycasts = IsOpen;
    }
}
