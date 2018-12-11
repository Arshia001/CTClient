using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class VideoAdForRewardButton : MonoBehaviour, IPointerClickHandler
{
    CanvasGroup Group;
    bool displayedAd, waitingToDisplayAd;

    void Start()
    {
        Group = GetComponent<CanvasGroup>();

        GetComponentInChildren<TextMeshProUGUI>().text = $"+{PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.VideoAdReward.ToString())}";
    }

    void Update()
    {
        if (waitingToDisplayAd)
        {
            Group.interactable = false;
            Group.alpha = 1;
        }
        else if (TapsellVideoAdManager.Instance.IsAdAvailable(TapsellVideoAdManager.EZones.AdForCoinReward) && !displayedAd)
        {
            Group.interactable = true;
            Group.alpha = 1;
        }
        else
        {
            Group.interactable = false;
            Group.alpha = 0;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        displayedAd = true;
        waitingToDisplayAd = true;
        TapsellVideoAdManager.Instance.OnAdFinishedEvent += OnAdFinished;
        TapsellVideoAdManager.Instance.StartAd(TapsellVideoAdManager.EZones.AdForCoinReward);
    }

    private async void OnAdFinished(TapsellVideoAdManager.EZones Zone, bool ShouldRewardPlayer, string AdID)
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
}
