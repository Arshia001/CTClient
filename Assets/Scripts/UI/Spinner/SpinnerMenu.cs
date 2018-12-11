using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerMenu : MonoBehaviour
{
    enum EState
    {
        WaitingToStart,
        OuterSpinning,
        OuterDone,
        InnerSpinning,
        Done
    }


    public AudioClip TakeRewardSound;

    EState State;
    Spinner Outer, Inner;
    Transform SpinnerContainer, MultiplyArrow;
    Button MultiplyButton;
    CanvasGroup AfterSpin;
    SpinnerLights Lights;
    TextMeshProUGUI RewardText;

    SpinRewardConfig OuterSpinResult;
    int? InnerSpinResult;


    void Awake()
    {
        SpinnerContainer = transform.Find("SpinnerContainer");
        Outer = transform.Find("SpinnerContainer/Outer").GetComponent<Spinner>();
        Inner = transform.Find("SpinnerContainer/Inner").GetComponent<Spinner>();
        AfterSpin = transform.Find("AfterSpin").GetComponent<CanvasGroup>();
        Lights = transform.Find("SpinnerContainer/Frame").GetComponent<SpinnerLights>();
        MultiplyArrow = transform.Find("AfterSpin/Arrow");
        MultiplyButton = transform.Find("AfterSpin/MultiplyButton").GetComponent<Button>();
        RewardText = transform.Find("AfterSpin/RewardText").GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (DateTime.Now < TransientData.Instance.UserProfile.NextSpinTime)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            State = EState.WaitingToStart;
        }

        TapsellVideoAdManager.Instance.OnAdFinishedEvent += OnTapsellAdFinished;

        var Pos = SpinnerContainer.transform.localPosition;
        Pos.x = 0;
        SpinnerContainer.transform.localPosition = Pos;

        AfterSpin.alpha = 0;

        OuterSpinResult = null;
        InnerSpinResult = null;
    }

    void OnDisable()
    {
        TapsellVideoAdManager.Instance.OnAdFinishedEvent -= OnTapsellAdFinished;
        Inner.OnFinishedEvent -= InnerSpinDone;
        Outer.OnFinishedEvent -= OuterSpinDone;
    }

    async void OnTapsellAdFinished(TapsellVideoAdManager.EZones Zone, bool ShouldRewardPlayer, string AdID)
    {
        if (Zone == TapsellVideoAdManager.EZones.SpinnerMultiplier && ShouldRewardPlayer)
        {
            try
            {
                State = EState.InnerSpinning;
                Inner.StartSpinning();
                Inner.OnFinishedEvent += InnerSpinDone;
                var SpinRes = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().RollMultiplierSpinner(AdID);
                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log($"Spin result: ID {SpinRes}, Contents {TransientData.Instance.SpinMultipliers[SpinRes].ToString()}");
                    Inner.SpinTo(SpinRes);
                    InnerSpinResult = SpinRes;
                    GetComponentInParent<MainMenu>().UpdateCurrencyDisplay();
                    Debug.Log(SpinRes);
                });
            }
            catch (Exception Ex)
            {
                Debug.LogError("Failed to roll multiplier spinner due to exception: " + Ex.ToString());
            }
        }
    }

    void InnerSpinDone()
    {
        State = EState.Done;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        var FirstSpinDone = !(State == EState.WaitingToStart || State == EState.OuterSpinning);

        var Pos = SpinnerContainer.transform.localPosition;
        Pos.x = Mathf.Lerp(Pos.x, FirstSpinDone ? -146 : 0, Time.deltaTime * 5);
        SpinnerContainer.transform.localPosition = Pos;

        AfterSpin.alpha = Mathf.Lerp(AfterSpin.alpha, FirstSpinDone ? 1 : 0, Time.deltaTime * 5);
        AfterSpin.blocksRaycasts = AfterSpin.interactable = AfterSpin.alpha > 0.5f;

        Lights.IsSpinnerRotating = State == EState.InnerSpinning || State == EState.OuterSpinning;

        MultiplyButton.interactable = TapsellVideoAdManager.Instance.IsAdAvailable(TapsellVideoAdManager.EZones.SpinnerMultiplier) && State == EState.OuterDone;
        MultiplyArrow.gameObject.SetActive(MultiplyButton.interactable);

        if (OuterSpinResult != null)
        {
            RewardText.gameObject.SetActive(true);
            var Count = OuterSpinResult.Count;
            if (InnerSpinResult.HasValue && State == EState.Done)
                Count *= TransientData.Instance.SpinMultipliers[InnerSpinResult.Value].Multiplier;
            RewardText.text = StylizePrice(Count, OuterSpinResult.RewardType);
        }
        else
            RewardText.gameObject.SetActive(false);
    }

    string StylizePrice(int Price, CurrencyType Currency)
    {
        switch (Currency)
        {
            case CurrencyType.Gem:
                return Price.ToString() + "<sprite=0>";
            case CurrencyType.Gold:
                return Price.ToString() + "<sprite=1>";
            default:
                return "";
        }
    }

    public void Close()
    {
        if (State != EState.InnerSpinning && State != EState.OuterSpinning)
        {
            if (State == EState.OuterDone || State == EState.Done)
                AudioSource.PlayClipAtPoint(TakeRewardSound, transform.root.position);

            gameObject.SetActive(false);
            //?? animate currency display
        }
    }

    public async void SpinOuter()
    {
        if (State != EState.WaitingToStart)
            return;

        Outer.StartSpinning();
        Outer.OnFinishedEvent += OuterSpinDone;
        State = EState.OuterSpinning;
        var SpinRes = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().RollSpinner();
        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            var Contents = TransientData.Instance.SpinRewards[SpinRes];
            Debug.Log($"Spin result: ID {SpinRes}, Contents {Contents.ToString()}");
            Outer.SpinTo(SpinRes);
            OuterSpinResult = Contents;
            GetComponentInParent<MainMenu>().UpdateCurrencyDisplay();
            Debug.Log(SpinRes);
        });
    }

    void OuterSpinDone()
    {
        if (State == EState.OuterSpinning)
            State = EState.OuterDone;
    }

    public void SpinInner()
    {
        var TapsellManager = TapsellVideoAdManager.Instance;
        if (State != EState.OuterDone || !TapsellManager.IsAdAvailable(TapsellVideoAdManager.EZones.SpinnerMultiplier))
            return;

        TapsellManager.StartAd(TapsellVideoAdManager.EZones.SpinnerMultiplier);
    }
}
