using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitCounter : MonoBehaviour
{
    public AudioClip TakeRewardSound;


    void Start()
    {
        var tr = TransientData.Instance;
        var bar = transform.Find("Bar");
        var template = transform.Find("Bar/FillerTemplate").gameObject;

        for (int i = 0; i < Mathf.Min(tr.UserProfile.Statistics[UserStatistics.CheckersHitToday.AsIndex()], tr.NumCheckersToHitPerDayForReward); ++i)
        {
            var go = Instantiate(template);
            go.SetActive(true);
            go.transform.SetParent(bar, false);
            var size = (go.transform as RectTransform).sizeDelta;
            size.x = size.x / tr.NumCheckersToHitPerDayForReward;
            (go.transform as RectTransform).sizeDelta = size;
        }

        transform.Find("Button").gameObject.SetActive(tr.UserProfile.Statistics[UserStatistics.CheckersHitToday.AsIndex()] >= tr.NumCheckersToHitPerDayForReward
            && tr.UserProfile.Statistics[UserStatistics.CheckersHitRewardCollectedForToday.AsIndex()] == 0);
    }

    public async void GetRewards()
    {
        var result = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().GetCheckerHitReward();

        if (result.RewardWasGiven)
        {
            TransientData.Instance.UserProfile.Statistics[UserStatistics.CheckersHitRewardCollectedForToday.AsIndex()] = 1;
            transform.Find("Button").gameObject.SetActive(false);

            AudioSource.PlayClipAtPoint(TakeRewardSound, transform.root.position);
            GetComponentInParent<MainMenu>().UpdateCurrencyDisplay(); //??
        }
    }
}
