using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardMenu : MonoBehaviour
{
    public Color OddRow, EvenRow, SelfRow;

    bool IsShowingGlobal, IsLoading;

    LeaderBoardQueryResult LeaderBoardInfo;


    void Start()
    {
        SwitchToLeaderBoard(true);
    }

    async void LoadLeaderBoard()
    {
        if (IsLoading)
            return;

        IsLoading = true;

        LeaderBoardInfo = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().GetLeaderBoard();

        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            DisplayLeaderBoard(IsShowingGlobal);

            var entry = LeaderBoardInfo.LastMonthEntries.Count >= 1 ? LeaderBoardInfo.LastMonthEntries[0] : null;
            if (entry != null)
                transform.Find("LastMonth1").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(entry.Name ?? TransientData.Instance.UserProfile.Name ?? "");

            entry = LeaderBoardInfo.LastMonthEntries.Count >= 2 ? LeaderBoardInfo.LastMonthEntries[1] : null;
            if (entry != null)
                transform.Find("LastMonth2").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(entry.Name ?? TransientData.Instance.UserProfile.Name ?? "");

            entry = LeaderBoardInfo.LastMonthEntries.Count >= 3 ? LeaderBoardInfo.LastMonthEntries[2] : null;
            if (entry != null)
                transform.Find("LastMonth3").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(entry.Name ?? TransientData.Instance.UserProfile.Name ?? "");
        });

        IsLoading = false;
    }

    public void SwitchToLeaderBoard(bool LoadGlobal)
    {
        if (IsShowingGlobal != LoadGlobal)
        {
            IsShowingGlobal = LoadGlobal;

            if (LeaderBoardInfo == null)
            {
                DisplayLoading();
                LoadLeaderBoard();
            }
            else
                DisplayLeaderBoard(LoadGlobal);
        }
    }

    void ClearAll(Transform Transform)
    {
        for (int i = 0; i < Transform.childCount; ++i)
        {
            var GO = Transform.GetChild(i).gameObject;
            if (GO.activeSelf)
                Destroy(GO);
        }
    }

    GameObject AddChild(Transform Parent, GameObject Template)
    {
        var GO = Instantiate(Template);
        GO.SetActive(true);
        GO.transform.SetParent(Parent, false);
        return GO;
    }

    void DisplayLeaderBoard(bool ShowGlobal) //?? empty list
    {
        var List = ShowGlobal ? LeaderBoardInfo.LifetimeEntries : LeaderBoardInfo.ThisMonthEntries;
        var OwnRank = ShowGlobal ? LeaderBoardInfo.LifetimeRank : LeaderBoardInfo.ThisMonthRank;

        var Parent = transform.Find("Table/Viewport/Content");
        var RowTemplate = Parent.Find("Template").gameObject;
        var DiscontinuityTemplate = Parent.Find("DiscontinuityTemplate").gameObject;

        ClearAll(Parent);

        ulong LastRank = 0;
        bool Odd = true;
        var Prof = TransientData.Instance.UserProfile;
        foreach (var E in List)
        {
            if (LastRank != 0 && E.Rank - LastRank > 1)
                AddChild(Parent, DiscontinuityTemplate);

            LastRank = E.Rank;

            var GO = AddChild(Parent, RowTemplate);
            if (E.Rank == OwnRank)
                GO.GetComponent<Image>().color = SelfRow;
            else
                GO.GetComponent<Image>().color = Odd ? OddRow : EvenRow;

            Odd = !Odd;

            var Tr = GO.transform;
            Tr.Find("Name").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(E.Name ?? Prof.Name ?? "");
            Tr.Find("Rank").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(E.Rank.ToString());
            Tr.Find("Score").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(E.Score.ToString());
            //?? active customizations (with null check for player's own row)
        }
    }

    void DisplayLoading()
    {
        var Parent = transform.Find("Table/Viewport/Content");
        ClearAll(Parent);

        AddChild(Parent, Parent.Find("LoadingTemplate").gameObject);
    }
}
