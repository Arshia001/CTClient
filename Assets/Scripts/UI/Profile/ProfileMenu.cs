using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileMenu : MonoBehaviour
{
    void OnEnable()
    {
        var Profile = TransientData.Instance.UserProfile;

        transform.Find("NumFirst").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.FirstPlace.AsIndex()].ToString());
        transform.Find("NumSecond").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.SecondPlace.AsIndex()].ToString());
        transform.Find("NumThird").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.ThirdPlace.AsIndex()].ToString());
        transform.Find("NumHit/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.CheckersHit.AsIndex()].ToString());
        transform.Find("NumGammons/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.Gammons.AsIndex()].ToString());
        transform.Find("NumDoubleSixes/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.DoubleSixes.AsIndex()].ToString());
        transform.Find("WinStreak/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Statistics[UserStatistics.MaxWinStreak.AsIndex()].ToString());

        if (Profile.Statistics[UserStatistics.GamesPlayed.AsIndex()] == 0)
            transform.Find("WinRate/Text").GetComponent<TextMeshProUGUI>().text = "--";
        else
            transform.Find("WinRate/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(
                (Mathf.RoundToInt((float)Profile.Statistics[UserStatistics.GamesWon.AsIndex()] /
                Profile.Statistics[UserStatistics.GamesPlayed.AsIndex()] * 100)).ToString() + "%");
    }
}
