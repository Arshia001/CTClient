using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchmakingScreen : MonoBehaviour
{
    void Start()
    {
        transform.Find("Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.IsMultiplayerAllowed ? "در حال پیدا کردن حریف..." : "در حال بارگذاری...");
    }
}
