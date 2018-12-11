using Network;
using Network.Backgammon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;

public class MainMenu : MonoBehaviour
{
    GameObject HomeScreen, ProfileScreen, BackgammonGameSelectionScreen,
        LeaderBoardScreen, SpinnerScreen, ShopScreen, CustomizationScreen,
        DialogBox, MatchmakingScreen, EditProfileScreen, NameBoxScreen;

    Button SpinnerButton;
    TextMeshProUGUI SpinnerTimeText;


    void Start()
    {
        HomeScreen = transform.Find("Home").gameObject;
        ProfileScreen = transform.Find("Profile").gameObject;
        BackgammonGameSelectionScreen = transform.Find("BackgammonGameSelection").gameObject;
        LeaderBoardScreen = transform.Find("LeaderBoard").gameObject;
        SpinnerScreen = transform.Find("Spinner").gameObject;
        ShopScreen = transform.Find("Shop").gameObject;
        CustomizationScreen = transform.Find("Customization").gameObject;
        DialogBox = transform.Find("DialogBox").gameObject;
        MatchmakingScreen = transform.Find("Matchmaking").gameObject;
        EditProfileScreen = transform.Find("EditProfile").gameObject;
        NameBoxScreen = transform.Find("NameBox").gameObject;

        SpinnerButton = transform.Find("Home/SpinnerButton").GetComponent<Button>();
        SpinnerTimeText = transform.Find("Home/SpinnerButton/Text").GetComponent<TextMeshProUGUI>();


#if UNITY_EDITOR
        if (ConnectionManager.Instance == null)
        {
            SceneManager.LoadScene("Startup");
            return;
        }
#endif

        var Profile = TransientData.Instance.UserProfile;

        SwitchToHomeScreen();
        var SpinnerReady = DateTime.Now >= Profile.NextSpinTime;
        if (Profile.Statistics[UserStatistics.GamesPlayed.AsIndex()] > 0 && !Profile.IsNameSet)
            SwitchToNameBoxScreen();
        else if (SpinnerReady)
            SwitchToSpinnerScreen();

        SpinnerButton.interactable = SpinnerReady;

        UpdateCurrencyDisplay();
    }

    void Update()
    {
        var SpinTime = TransientData.Instance.UserProfile.NextSpinTime;
        SpinnerButton.interactable = DateTime.Now > SpinTime;
        SpinnerTimeText.text = PersianTextShaper.PersianTextShaper.ShapeText(DateTime.Now > SpinTime ? "آمادست!" : (SpinTime - DateTime.Now).ToString("h\\:mm\\:ss"));
    }

    public void UpdateCurrencyDisplay()
    {
        var Profile = TransientData.Instance.UserProfile;

        transform.Find("CurrencyDisplay/GoldButton/Text").GetComponent<TextMeshProUGUI>().text = Profile.Gold.ToString();
        transform.Find("CurrencyDisplay/GemButton/Text").GetComponent<TextMeshProUGUI>().text = Profile.Gems.ToString();
    }

    void HideAll()
    {
        HomeScreen.SetActive(false);
        BackgammonGameSelectionScreen.SetActive(false);
        SpinnerScreen.SetActive(false);
        LeaderBoardScreen.SetActive(false);
        ProfileScreen.SetActive(false);
        ShopScreen.SetActive(false);
        CustomizationScreen.SetActive(false);
        DialogBox.SetActive(false);
        MatchmakingScreen.SetActive(false);
        EditProfileScreen.SetActive(false);
        NameBoxScreen.SetActive(false);
    }

    public void SwitchToHomeScreen()
    {
        HideAll();
        HomeScreen.SetActive(true);
    }

    public void SwitchToBackgammonGameSelectionScreen()
    {
        HideAll();
        BackgammonGameSelectionScreen.SetActive(true);
    }

    public void SwitchToSpinnerScreen()
    {
        SpinnerScreen.SetActive(true);
    }

    public void SwitchToLeaderBoardScreen()
    {
        HideAll();
        LeaderBoardScreen.SetActive(true);
    }

    public void SwitchToProfileScreen()
    {
        HideAll();
        ProfileScreen.SetActive(true);
    }

    public void SwitchToShopScreen()
    {
        HideAll();
        ShopScreen.SetActive(true);
    }

    public void SwitchToShopScreenPurchaseSection(bool gold)
    {
        SwitchToShopScreen();
        ShopScreen.GetComponent<ShopMenu>().GoToPurchaseSection(gold);
    }

    public void SwitchToCustomizationScreen()
    {
        CustomizationScreen.SetActive(true);
    }

    public void ShowEditProfileScreen()
    {
        EditProfileScreen.SetActive(true);
    }

    public void ShowMatchmakingScreen()
    {
        MatchmakingScreen.SetActive(true);
    }

    public void HideMatchmakingScreen()
    {
        MatchmakingScreen.SetActive(false);
    }

    public void SwitchToNameBoxScreen()
    {
        NameBoxScreen.SetActive(true);
    }

    public async void EnterGame() //?? move logic to separate persistent gameobject and class (StateManager? SystemManager? something like that)
    {
        var ID = transform.GetComponentInChildren<GameSelection>().GetSelectedGameID();
        var EP = ConnectionManager.Instance.EndPoint<MatchmakingEndPoint>();
        EP.OnJoinGame += EP_OnJoinGame;
        EP.OnRemovedFromQueue += EP_OnRemovedFromQueue;
        try
        {
            ShowMatchmakingScreen();
            TransientData.Instance.CurrentGameID = ID;
            await EP.EnterQueue(ID);
        }
        catch (Exception Ex)
        {
            EP.OnJoinGame -= EP_OnJoinGame;
            EP.OnRemovedFromQueue -= EP_OnRemovedFromQueue;
            HideMatchmakingScreen();
            global::DialogBox.Instance.ShowOneButton("شرمنده", "ما نتونستیم برات دنبال حریف بگردیم.\nیه کم صبر کن بعد دوباره امتحان کن.", () => { });
            Debug.LogError($"Failed to initiate matchmaking due to {Ex}");
        }
    }

    private void EP_OnRemovedFromQueue()
    {
        HideMatchmakingScreen();
        global::DialogBox.Instance.ShowOneButton("حریف پیدا نشد", "ما نتونستیم برات حریف پیدا کنیم.\nیه کم صبر کن بعد دوباره امتحان کن.", () => { });
    }

    //public async void EnterGameWithVideoAd(string AdID)
    //{
    //    //??
    //}

    void EP_OnJoinGame(OpponentInfo OpponentInfo, ulong TotalGold)
    {
        Debug.Log($"Joining game with {OpponentInfo.Name}");

        ConnectionManager.Instance.EndPoint<MatchmakingEndPoint>().OnJoinGame -= EP_OnJoinGame;
        ConnectionManager.Instance.EndPoint<MatchmakingEndPoint>().OnRemovedFromQueue -= EP_OnRemovedFromQueue;
        TransientData.Instance.OpponentInfo = OpponentInfo;
        TransientData.Instance.UserProfile.Gold = TotalGold;

        SceneManager.LoadScene("Game"); //?? async, loading screen?
    }
}
