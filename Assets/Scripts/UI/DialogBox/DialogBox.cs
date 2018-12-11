using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogBox : MonoBehaviour
{
    public static DialogBox Instance { get; private set; }


    GameObject SingleButton, TwinButtons;
    TextMeshProUGUI Title, Text, OKButton, YesButton, NoButton;

    Action OnOK, OnYes, OnNo;


    void Awake()
    {
        Instance = this;

        SingleButton = transform.Find("SingleButton").gameObject;
        TwinButtons = transform.Find("TwinButtons").gameObject;
        Title = transform.Find("Title").GetComponent<TextMeshProUGUI>();
        Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        OKButton = transform.Find("SingleButton/Text").GetComponent<TextMeshProUGUI>();
        YesButton = transform.Find("TwinButtons/YesButton/Text").GetComponent<TextMeshProUGUI>();
        NoButton = transform.Find("TwinButtons/NoButton/Text").GetComponent<TextMeshProUGUI>();

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowOneButton(string TitleText, string BodyText, Action OnOKClicked, string OKButtonText = "باشه")
    {
        Title.text = PersianTextShaper.PersianTextShaper.ShapeText(TitleText);
        Text.text = PersianTextShaper.PersianTextShaper.ShapeText(BodyText);
        OKButton.text = PersianTextShaper.PersianTextShaper.ShapeText(OKButtonText);
        OnOK = OnOKClicked;

        gameObject.SetActive(true);
        SingleButton.SetActive(true);
        TwinButtons.SetActive(false);
    }

    public void ShowTwoButton(string TitleText, string BodyText, Action OnYesClicked, Action OnNoClicked, string YesButtonText = "آره", string NoButtonText = "نه")
    {
        //?? MASSIVE HACK, need support for tags in persian text shaper or better replacement logic here
        Title.text = PersianTextShaper.PersianTextShaper.ShapeText(TitleText).Replace("<sprite=۰>", "<sprite=0>").Replace("<sprite=۱>", "<sprite=1>");
        Text.text = PersianTextShaper.PersianTextShaper.ShapeText(BodyText).Replace("<sprite=۰>", "<sprite=0>").Replace("<sprite=۱>", "<sprite=1>");
        YesButton.text = PersianTextShaper.PersianTextShaper.ShapeText(YesButtonText);
        NoButton.text = PersianTextShaper.PersianTextShaper.ShapeText(NoButtonText);
        OnYes = OnYesClicked;
        OnNo = OnNoClicked;

        gameObject.SetActive(true);
        SingleButton.SetActive(false);
        TwinButtons.SetActive(true);
    }

    public void OnOKClicked()
    {
        gameObject.SetActive(false);
        OnOK?.Invoke();
    }

    public void OnYesClicked()
    {
        gameObject.SetActive(false);
        OnYes?.Invoke();
    }

    public void OnNoClicked()
    {
        gameObject.SetActive(false);
        OnNo?.Invoke();
    }
}
