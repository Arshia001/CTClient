using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TapsellSDK;
using UnityEngine;

public class TapsellVideoAdManager : MonoBehaviour
{
    public string TapsellSecret = "apohomlcsrhgpdktibeecoarsakabgbppcnckbgmemkfmsaceiafjfntfkhthblrrlirfh";
    public string SpinnerMultiplierZoneID = "5a71b3d0c21bf000016aa95d";
    // public string EntryFeeZoneID = "5a71b41bdc93ee0001eabd12";
    public string AdForCoinRewardZoneID = "5a71b41bdc93ee0001eabd12";


    public enum EZones
    {
        Unknown = -1,
        SpinnerMultiplier,
        // EntryFee,
        AdForCoinReward
    }

    class ZoneData
    {
        public readonly bool CacheAds;
        public TapsellAd Ad;
        public int NoAdRetryCount;


        public ZoneData(bool CacheAds)
        {
            this.CacheAds = CacheAds;
        }
    }


    public static TapsellVideoAdManager Instance { get; private set; }


    EZones ConvertZone(string ZoneID)
    {
        if (ZoneID == SpinnerMultiplierZoneID)
            return EZones.SpinnerMultiplier;
        //else if (ZoneID == EntryFeeZoneID)
        //    return EZones.EntryFee;
        else if (ZoneID == AdForCoinRewardZoneID)
            return EZones.AdForCoinReward;

        return EZones.Unknown;
    }

    string ConvertZone(EZones ZoneID)
    {
        switch (ZoneID)
        {
            //case EZones.EntryFee:
            //    return EntryFeeZoneID;
            case EZones.SpinnerMultiplier:
                return SpinnerMultiplierZoneID;
            case EZones.AdForCoinReward:
                return AdForCoinRewardZoneID;
            default:
                return null;
        }
    }


    public delegate void OnAdFinishedDelegate(EZones Zone, bool ShouldRewardPlayer, string AdID);
    public event OnAdFinishedDelegate OnAdFinishedEvent;

    Dictionary<string, ZoneData> Zones = new Dictionary<string, ZoneData>();


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Zones.Add(SpinnerMultiplierZoneID, new ZoneData(true));
        // Zones.Add(EntryFeeZoneID, new ZoneData(false));
        Zones.Add(AdForCoinRewardZoneID, new ZoneData(true));

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        Tapsell.initialize(TapsellSecret);
        Tapsell.setRewardListener(OnFinished);

        RequestAd(SpinnerMultiplierZoneID);
        // RequestAd(EntryFeeZoneID);
        RequestAd(AdForCoinRewardZoneID);
#endif
    }

    public bool IsAdAvailable(EZones Zone)
    {
        return Zones[ConvertZone(Zone)].Ad != null;
    }

    public bool StartAd(EZones Zone)
    {
        ConnectionManager.Instance.DelayKeepAlive(TimeSpan.FromMinutes(2));

        var Ad = Zones[ConvertZone(Zone)].Ad;

        if (Ad == null)
            return false;

        var ShowOptions = new TapsellShowOptions();
        ShowOptions.backDisabled = true;
        ShowOptions.immersiveMode = true;
        ShowOptions.rotationMode = TapsellShowOptions.ROTATION_LOCKED_LANDSCAPE;
        ShowOptions.showDialog = true;

        Tapsell.showAd(Ad, ShowOptions);

        Zones[ConvertZone(Zone)].Ad = null;

        return true;
    }

    void RequestAd(string ZoneID)
    {
        Tapsell.requestAd(ZoneID, Zones[ZoneID].CacheAds, OnAvailable, OnNoAdAvailable, OnError, OnNoNetwork, OnExpiring, OnOpened, OnClosed);
    }

    void OnAvailable(TapsellAd Ad)
    {
        Debug.Log("Ad available - " + Ad.adId + " - " + Ad.zoneId);
        Zones[Ad.zoneId].Ad = Ad;
    }

    async void OnNoAdAvailable(string ZoneID)
    {
        Debug.LogWarning("No ad available - " + ZoneID);

        var Zone = Zones[ZoneID];
        Zone.Ad = null;
        ++Zone.NoAdRetryCount;
        await Task.Delay(TimeSpan.FromMinutes(Zone.NoAdRetryCount));
        RequestAd(ZoneID);
    }

    async void OnError(TapsellError Error)
    {
        Debug.LogError(Error.error);

        await Task.Delay(1000);
        RequestAd(Error.zoneId);
    }

    async void OnNoNetwork(string ZoneID)
    {
        Debug.LogError("No network for getting ad");

        await Task.Delay(1000);
        RequestAd(ZoneID);
    }

    void OnExpiring(TapsellAd Ad)
    {
        Debug.Log("Ad expiring - " + Ad.adId + " - " + Ad.zoneId);

        Zones[Ad.zoneId].Ad = null;
        RequestAd(Ad.zoneId);
    }

    void OnOpened(TapsellAd Ad)
    {
        Debug.Log("Ad opened - " + Ad.adId);
    }

    void OnClosed(TapsellAd Ad)
    {
        Debug.Log("Ad closed - " + Ad.adId);
    }

    void OnFinished(TapsellAdFinishedResult Result)
    {
        Debug.Log("Ad finished - " + Result.adId + " - " + Result.zoneId + " - rewarded: " + Result.rewarded + " - completed: " + Result.completed);

        RequestAd(Result.zoneId);

        MainThreadDispatcher.Instance.Enqueue(() => OnAdFinishedEvent?.Invoke(ConvertZone(Result.zoneId), Result.rewarded && Result.completed, Result.adId));
    }
}
