using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameSelection : MonoBehaviour
{
    List<GameImage> Games = new List<GameImage>();
    int SelectedIndex;

    CustomScrollRect ScrollRect;

    TKSwipeRecognizer SwipeRecognizer;


    void Start()
    {
        //?? move all this to GameSelectionMenu
        foreach (var GI in transform.GetComponentsInChildren<GameImage>())
            Games.Add(GI);

        ScrollRect = GetComponent<CustomScrollRect>();
    }

    void OnEnable()
    {
        if (SwipeRecognizer == null)
        {
            SwipeRecognizer = new TKSwipeRecognizer(TKSwipeDirection.Horizontal);
            SwipeRecognizer.gestureRecognizedEvent += SwipeRecognizer_Recognized;
        }
        TouchKit.addGestureRecognizer(SwipeRecognizer);
    }

    private void OnDisable()
    {
        TouchKit.removeGestureRecognizer(SwipeRecognizer);
    }

    void SwipeRecognizer_Recognized(TKSwipeRecognizer obj)
    {
        SetSelectedIndex(SelectedIndex + (obj.completedSwipeDirection == TKSwipeDirection.Left ? 1 : -1));
    }

    public void SetSelectedID(int ID)
    {
        for (int Idx = 0; Idx < Games.Count; ++Idx)
            if (Games[Idx].GameID == ID)
                SetSelectedIndex(Idx);
    }

    public void SetSelectedIndex(int Index)
    {
        SelectedIndex = Mathf.Clamp(Index, 0, Games.Count - 1);
    }

    void Update()
    {
        ScrollRect.horizontalNormalizedPosition = Mathf.Lerp(ScrollRect.horizontalNormalizedPosition, SelectedIndex / (float)(Games.Count - 1), Time.deltaTime * 5);
    }

    public int GetSelectedGameID()
    {
        return Games[SelectedIndex].GameID;
    }
}
