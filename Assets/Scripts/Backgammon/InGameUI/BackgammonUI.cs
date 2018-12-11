using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SuperBlur;
using TMPro;
using Random = UnityEngine.Random;

public class BackgammonUI : MonoBehaviour
{
    const float XPBarMaxWidth = 305;


    public static BackgammonUI Instance { get; private set; }


    TimerBar SelfTimer, EnemyTimer;
    EmoteBubble SelfBubble, EnemyBubble;

    CanvasGroup InGameUIGroup, EndGameUIGroup;
    RectTransform WinSign, LossSign, XPBar, RewardFrame, LevelUpFrame;
    TextMeshProUGUI CoinsText, LevelText, LevelUpText;
    SuperBlurBase Blur;

    public Texture[] DicePromptTextures;

    ParticleSystem WinEffect, LoseEffect, LevelUpEffect;
    Animator startTurnAnimator, dicePromptAnimator, dicePromptHandAnimator;
    Renderer dicePromptRenderer;
    UIElementDisplayController UndoButton, AcceptButton;

    public AudioClip WinSound, LoseSound, LevelUpSound;


    void Awake()
    {
        Instance = this;

        SelfTimer = transform.Find("InGame/ProfileSelf/TimerBar").GetComponent<TimerBar>();
        EnemyTimer = transform.Find("InGame/ProfileEnemy/TimerBar").GetComponent<TimerBar>();

        SelfBubble = transform.Find("InGame/EmoteBubbleSelf").GetComponent<EmoteBubble>();
        EnemyBubble = transform.Find("InGame/EmoteBubbleEnemy").GetComponent<EmoteBubble>();

        InGameUIGroup = transform.Find("InGame").GetComponent<CanvasGroup>();
        InGameUIGroup.alpha = 1;
        EndGameUIGroup = transform.Find("EndGame").GetComponent<CanvasGroup>();
        EndGameUIGroup.alpha = 0;

        WinSign = transform.Find("EndGame/WinSign") as RectTransform;
        LossSign = transform.Find("EndGame/LossSign") as RectTransform;
        XPBar = transform.Find("EndGame/Footer/XPBar") as RectTransform;
        RewardFrame = transform.Find("EndGame/Footer/RewardFrame") as RectTransform;
        LevelUpFrame = transform.Find("EndGame/LevelUp") as RectTransform;

        CoinsText = transform.Find("EndGame/Footer/RewardFrame/Text").GetComponent<TextMeshProUGUI>();
        LevelText = transform.Find("EndGame/Footer/LevelFrame/LevelText").GetComponent<TextMeshProUGUI>();
        LevelUpText = transform.Find("EndGame/LevelUp/Text").GetComponent<TextMeshProUGUI>();

        Blur = Camera.main.GetComponent<SuperBlurBase>();
        Blur.enabled = false;

        var _3dObjectsRoot = GetComponent<Canvas>().worldCamera.transform;
        WinEffect = _3dObjectsRoot.Find("Win").GetComponent<ParticleSystem>();
        LoseEffect = _3dObjectsRoot.Find("Lose").GetComponent<ParticleSystem>();
        LevelUpEffect = _3dObjectsRoot.Find("LevelUp").GetComponent<ParticleSystem>();
        startTurnAnimator = _3dObjectsRoot.Find("StartTurnAnimation").GetComponent<Animator>();
        dicePromptAnimator = _3dObjectsRoot.Find("DiceAnimation").GetComponent<Animator>();
        dicePromptRenderer = dicePromptAnimator.gameObject.GetComponentInChildren<Renderer>();
        dicePromptHandAnimator = _3dObjectsRoot.Find("HandAnimation").GetComponent<Animator>();

        // transform.Find("InGame/TurnStart").gameObject.SetActive(false);

        UndoButton = transform.Find("InGame/GameActions/Undo").GetComponent<UIElementDisplayController>();
        AcceptButton = transform.Find("InGame/GameActions/Accept").GetComponent<UIElementDisplayController>();

        if (!TransientData.Instance.IsMultiplayerAllowed)
        {
            transform.Find("InGame/EmoteSelf").gameObject.SetActive(false);
            transform.Find("InGame/ProfileEnemy").gameObject.SetActive(false);
            transform.Find("InGame/EmoteBubbleEnemy").gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetPlayerDetails(string OwnName, IEnumerable<int> OwnItems, string EnemyName, IEnumerable<int> EnemyItems)
    {
        transform.Find("InGame/ProfileSelf/Name").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(OwnName);
        transform.Find("InGame/ProfileEnemy/Name").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(EnemyName);

        transform.Find("InGame/ProfileSelf/Image").GetComponent<Image>().sprite = ProfileFrame.GetProfilePicture(OwnItems);
        transform.Find("InGame/ProfileEnemy/Image").GetComponent<Image>().sprite = ProfileFrame.GetProfilePicture(EnemyItems);
    }

    public void SetActionButtons(bool canUndo, bool canEndTurn)
    {
        UndoButton.Show = canUndo;
        AcceptButton.Show = canEndTurn;
    }

    public void SetTimers(bool ActiveSelf, bool ActiveEnemy, float Remaining)
    {
        SelfTimer.Set(Remaining, ActiveSelf);
        EnemyTimer.Set(Remaining, !ActiveSelf);
    }

    public void ShowEmote(int EmoteID, bool Self)
    {
        var E = Emote.Get(EmoteID);
        if (E == null)
            return;

        (Self ? SelfBubble : EnemyBubble).Enqueue(E);
    }

    public void SwitchToEndGameScreen(bool Win)
    {
        startTurnAnimator.gameObject.SetActive(false);
        dicePromptAnimator.gameObject.SetActive(false);
        dicePromptHandAnimator.gameObject.SetActive(false);

        StartCoroutine(SwitchToEndGameScreen_Impl(Win));
    }

    public void ShowOwnTurnStarted()
    {
        startTurnAnimator.SetTrigger("DoSlide");
    }

    bool showingDicePrompt = false;
    public void ShowDiceRollPrompt()
    {
        dicePromptAnimator.SetBool("Show", true);

        if (!showingDicePrompt && DicePromptTextures.Length > 0)
            dicePromptRenderer.material.mainTexture = DicePromptTextures[Random.Range(0, DicePromptTextures.Length)];

        showingDicePrompt = true;
        Invoke(nameof(ShowDicePromptHand), 3.0f);
    }

    void ShowDicePromptHand()
    {
        if (showingDicePrompt)
            dicePromptHandAnimator.SetBool("Show", true);
    }

    public void HideDiceRollPrompt()
    {
        showingDicePrompt = false;
        CancelInvoke(nameof(ShowDicePromptHand));

        dicePromptAnimator.SetBool("Show", false);
        dicePromptHandAnimator.SetBool("Show", false);
    }

    public void ShowWaitingForOpponent()
    {
        transform.Find("WaitingScreen").gameObject.SetActive(true);
        transform.Find("WaitingScreen/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText("در انتظار حریف...");
    }

    public void ShowConnectingToGame()
    {
        transform.Find("WaitingScreen").gameObject.SetActive(true);
        transform.Find("WaitingScreen/Text").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText("در حال اتصال به بازی...");
    }

    public void HideWaitingScreen()
    {
        transform.Find("WaitingScreen").gameObject.SetActive(false);
    }

    IEnumerator SwitchToEndGameScreen_Impl(bool Win)
    {
        var Profile = TransientData.Instance.UserProfile;

        var audio = GetComponent<AudioSource>();
        audio.clip = Win ? WinSound : LoseSound;
        audio.Play();

        WinSign.gameObject.SetActive(Win);
        RewardFrame.gameObject.SetActive(Win);
        LossSign.gameObject.SetActive(!Win);

        if (Win)
            WinEffect.Play(true);
        else
            LoseEffect.Play(true);

        CoinsText.text = PersianTextShaper.PersianTextShaper.ShapeText("0");
        LevelText.text = PersianTextShaper.PersianTextShaper.ShapeText((Profile.Level - Profile.LastDeltaLevel).ToString());

        float XPBarStartWidth, XPBarEndWidth = Profile.XP / (float)Profile.LevelXP;
        if (Profile.LastDeltaLevel == 0)
            XPBarStartWidth = Profile.LastXP / (float)Profile.LevelXP;
        else
            XPBarStartWidth = Profile.LastXP / (float)Profile.LastLevelXP;

        var Size = XPBar.sizeDelta;
        Size.x = XPBarStartWidth * XPBarMaxWidth;
        XPBar.sizeDelta = Size;

        Blur.enabled = true;
        Blur.Interpolation = 0;
        Blur.Saturation = 1;


        // Step 1: switch to end-game screen
        float LerpVal = 0;
        while (true)
        {
            LerpVal = Mathf.Lerp(LerpVal, 1, Time.deltaTime * 8);
            if (LerpVal > 0.99f)
                LerpVal = 1;

            InGameUIGroup.alpha = 1 - LerpVal;
            EndGameUIGroup.alpha = LerpVal;
            Blur.Interpolation = LerpVal;
            Blur.Saturation = 1 - LerpVal;

            if (LerpVal < 1)
                yield return null;
            else
                break;
        }

        yield return null;

        // Step 2: animate coins if player won
        LerpVal = 0;
        while (true)
        {
            LerpVal = Mathf.Lerp(LerpVal, 1, Time.deltaTime * 2.5f);
            if (LerpVal > 0.99f || Input.GetKeyDown(KeyCode.Mouse0))
                LerpVal = 1;

            CoinsText.text = PersianTextShaper.PersianTextShaper.ShapeText(((int)Mathf.Lerp(0, Profile.LastDeltaGold - Profile.LevelUpDeltaGold, LerpVal)).ToString());

            if (LerpVal < 1)
                yield return null;
            else
                break;
        }

        yield return new WaitForSeconds(0.5f);

        // Step 3: animate XP bar
        LerpVal = 0;
        while (true)
        {
            LerpVal = Mathf.Lerp(LerpVal, 1, Time.deltaTime * 2.5f);
            if (LerpVal > 0.99f || Input.GetKeyDown(KeyCode.Mouse0))
                LerpVal = 1;

            if (Profile.LastDeltaLevel > 0)
            {
                var Width = Mathf.Lerp(XPBarStartWidth, 1 + XPBarEndWidth, LerpVal);
                if (Width >= 1)
                    LevelText.text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Level.ToString());
                else
                    LevelText.text = PersianTextShaper.PersianTextShaper.ShapeText((Profile.Level - Profile.LastDeltaLevel).ToString());

                Size = XPBar.sizeDelta;
                Size.x = (Width % 1) * XPBarMaxWidth;
                XPBar.sizeDelta = Size;
            }
            else
            {
                Size = XPBar.sizeDelta;
                Size.x = Mathf.Lerp(XPBarStartWidth, XPBarEndWidth, LerpVal) * XPBarMaxWidth;
                XPBar.sizeDelta = Size;
            }

            if (LerpVal < 1)
                yield return null;
            else
                break;
        }

        yield return null;

        // Step 4: show level-up rewards if any
        if (Profile.LastDeltaLevel > 0)
        {
            LevelUpFrame.gameObject.SetActive(true);

            if (Profile.LevelUpDeltaGold > 0)
                LevelUpText.text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.LevelUpDeltaGold.ToString()) + " <sprite=1>";
            else
                LevelUpText.text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.LevelUpDeltaGems.ToString()) + " <sprite=0>";

            LerpVal = 0;
            while (true)
            {
                LerpVal = Mathf.Lerp(LerpVal, 1, Time.deltaTime * 4.0f);
                if (LerpVal > 0.99f || Input.GetKeyDown(KeyCode.Mouse0))
                    LerpVal = 1;

                var pos = LevelUpFrame.transform.localPosition;
                pos.y = Mathf.Lerp(-400, 60, LerpVal);
                LevelUpFrame.transform.localPosition = pos;

                if (LerpVal < 1)
                    yield return null;
                else
                    break;
            }

            LevelUpEffect.Play();

            yield return null;
        }

        // Step 5: Wait a few seconds and transition to menu
        var WaitTime = 0.0f;
        while (WaitTime < 5.0f)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
                break;

            WaitTime += Time.deltaTime;
            yield return null;
        }

        Profile.ResetDeltas();
        Backgammon.GameManager.Instance.EndGameScreenFinished();
    }
}
