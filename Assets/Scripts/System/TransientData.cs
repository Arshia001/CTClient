using LightMessage.Common.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UserProfile
{
    ulong _Gold;
    public ulong Gold { get { return _Gold; } set { LastDeltaGold = value - _Gold; _Gold = value; } }
    ulong _Gems;
    public ulong Gems { get { return _Gems; } set { LastDeltaGems = value - _Gems; _Gems = value; } }
    uint _XP;
    public uint XP { get { return _XP; } set { LastXP = _XP; _XP = value; } }
    uint _Level;
    public uint Level { get { return _Level; } set { LastDeltaLevel = value - _Level; _Level = value; } }
    uint _LevelXP;
    public uint LevelXP { get { return _LevelXP; } set { LastLevelXP = _LevelXP; _LevelXP = value; } }
    public DateTime NextSpinTime { get; set; }
    public bool HasRatedOurApp { get; set; }
    public string Name { get; set; }
    public bool IsNameSet { get; set; }
    public bool IsTutorialFinished { get; set; }
    public List<int> ActiveItems { get; set; }
    public Dictionary<int, int> OwnedItems { get; set; }
    public uint[] Statistics { get; set; }

    // These values will be used for UI animation, and may be set back to zero by the UI code when they're used
    public ulong LastDeltaGold { get; set; }
    public ulong LastDeltaGems { get; set; }
    public ulong LastXP { get; set; }
    public ulong LastDeltaLevel { get; set; }
    public ulong LastLevelXP { get; set; }

    public ulong LevelUpDeltaGold { get; set; }
    public ulong LevelUpDeltaGems { get; set; }


    public UserProfile(IReadOnlyList<Param> Params)
    {
        _Gems = Params[0].AsUInt.Value;
        _Gold = Params[1].AsUInt.Value;
        _Level = (uint)Params[2].AsUInt.Value;
        _XP = (uint)Params[3].AsUInt.Value;
        LevelXP = (uint)Params[4].AsUInt.Value;
        Name = Params[5].AsString;
        NextSpinTime = DateTime.Now + Params[6].AsTimeSpan.Value;
        IsTutorialFinished = Params[7].AsBoolean.Value;
        HasRatedOurApp = Params[8].AsBoolean.Value;

        var AllItems = TransientData.Instance.CutomizationItems;
        var CustArray = Params[10].AsArray;
        ActiveItems = new List<int>();
        for (int Idx = 0; Idx < CustArray.Count; ++Idx)
            ActiveItems.Add((int)CustArray[Idx].AsInt.Value);

        var OwnedArray = Params[11].AsArray;
        OwnedItems = new Dictionary<int, int>();
        for (int Idx = 0; Idx < OwnedArray.Count; ++Idx)
            OwnedItems.Add((int)OwnedArray[Idx].AsInt.Value, 1);

        Statistics = Params[12].AsArray.Select(p => (uint)p.AsUInt.Value).ToArray();

        IsNameSet = Params[13].AsBoolean.Value;
    }

    public void ResetDeltas()
    {
        LastDeltaGems = 0;
        LastDeltaGold = 0;
        LastDeltaLevel = 0;
        LastXP = 0;
        LastLevelXP = 0;
        LevelUpDeltaGems = 0;
        LevelUpDeltaGold = 0;
    }

    public void ProcessPurchaseResult(PurchaseResult Purchase)
    {
        if (Purchase.Result == EPurchaseResult.Success)
        {
            Gold = Purchase.TotalGold;
            Gems = Purchase.TotalGems;
            if (Purchase.OwnedItems != null)
                OwnedItems = Purchase.OwnedItems;
        }
    }

    public void SetActiveItems(IEnumerable<int> ItemIDs)
    {
        ActiveItems.Clear();
        foreach (var ID in ItemIDs)
            ActiveItems.Add(ID);
    }

    public bool HasItem(int ID)
    {
        CustomizationItemConfig Item;
        return (OwnedItems?.ContainsKey(ID) ?? false) || (TransientData.Instance.CutomizationItems.TryGetValue(ID, out Item) && Item.Price == 0);
    }
}

public enum CurrencyType
{
    Gold,
    Gem,
    IRR
}

public enum CustomizationItemCategory
{
    CheckerFrame,
    CheckerImage,
    CheckerGem,
    ProfileGender
    //?? other entries here
}

public enum EPurchaseResult
{
    Success,
    InsufficientFunds,
    InvalidID,
    AlreadyOwnsItem,
    IabTokenIsInvalid,
    CannotVerifyIab
}

public class VersionInfo
{
    public uint Latest;
    public uint EarliestSupported;
}

public class PurchaseResult
{
    public EPurchaseResult Result { get; set; }
    public ulong TotalGold { get; set; }
    public ulong TotalGems { get; set; }
    public Dictionary<int, int> OwnedItems { get; set; }

    public PurchaseResult(IReadOnlyList<Param> Params)
    {
        Result = (EPurchaseResult)Params[0].AsUInt.Value;
        TotalGold = Params[1].AsUInt ?? 0;
        TotalGems = Params[2].AsUInt ?? 0;

        var Arr = Params[3].AsArray;
        if (Arr != null)
        {
            OwnedItems = new Dictionary<int, int>();
            foreach (var P in Arr)
                OwnedItems.Add((int)P.AsInt.Value, 1);
        }
    }
}

public enum UserStatistics
{
    CheckersHit = 0,
    Gammons = 1,
    GamesPlayed = 2,
    GamesWon = 3,
    DoubleSixes = 4,
    WinStreak = 5,
    MaxWinStreak = 6,
    FirstPlace = 7,
    SecondPlace = 8,
    ThirdPlace = 9,
    CheckersHitToday = 10,
    CheckersHitRewardCollectedForToday = 11,
    MAX = 12
}

public static class UserStatisticsExtensions
{
    public static int AsIndex(this UserStatistics Stat) => (int)Stat;
}

public class SpinResults
{
    public int RewardID;
    public ulong TotalGold;
    public ulong TotalGems;
    public TimeSpan TimeUntilNextSpin;
}

public class CustomizationItemConfig
{
    public int ID { get; }
    public float Sort { get; }
    public string ResourceID { get; }
    public CustomizationItemCategory Category { get; }
    public CurrencyType PriceCurrency { get; }
    public int Price { get; }
    public bool IsPurchasable { get; }

    public CustomizationItemConfig(IReadOnlyList<Param> Params)
    {
        ID = (int)Params[0].AsInt.Value;
        Category = (CustomizationItemCategory)Enum.Parse(typeof(CustomizationItemCategory), Params[1].AsString, true);
        IsPurchasable = Params[2].AsBoolean.Value;
        Price = (int)Params[3].AsUInt.Value;
        PriceCurrency = (CurrencyType)Enum.Parse(typeof(CurrencyType), Params[4].AsString, true);
        ResourceID = Params[5].AsString;
        Sort = Params[6].AsFloat.Value;
    }
}

public class GameConfig
{
    public int ID { get; }
    public float Sort { get; }
    public string Name { get; }
    public string BoardID { get; }
    public ulong EntranceFee { get; }
    public ulong Reward { get; }
    // public bool CanWatchVideoAd { get; }

    public GameConfig(IReadOnlyList<Param> Params)
    {
        ID = (int)Params[0].AsInt.Value;
        BoardID = Params[1].AsString;
        EntranceFee = Params[2].AsUInt.Value;
        Name = Params[3].AsString;
        Reward = Params[4].AsUInt.Value;
        Sort = Params[5].AsFloat.Value;
        // CanWatchVideoAd = Params[6].AsBoolean.Value;
    }
}

public class PackCategoryConfig
{
    public int ID { get; }
    public float Sort { get; }
    public string Name { get; }
    public DateTime? PurchaseDeadline { get; }
    public bool IsSpecial { get; }
    public bool IsAvailableInShop { get; }

    public List<PackConfig> Packs { get; }

    public PackCategoryConfig(IReadOnlyList<Param> Params)
    {
        ID = (int)Params[0].AsInt.Value;
        IsSpecial = Params[1].AsBoolean.Value;
        Name = Params[2].AsString;
        PurchaseDeadline = Params[3].AsDateTime;
        IsAvailableInShop = Params[4].AsBoolean.Value;
        Sort = Params[5].AsFloat.Value;

        Packs = new List<PackConfig>();
    }
}

public class PackConfig
{
    public int ID { get; }
    public float Sort { get; }
    public string Name { get; }
    public PackCategoryConfig Category { get; }
    public string Tag { get; }
    public string ImageID { get; }
    public CurrencyType PriceCurrency { get; }
    public int Price { get; private set; }
    public string IabSku { get; }
    public Dictionary<CurrencyType, int> Contents_Currency { get; }
    public HashSet<CustomizationItemConfig> Contents_CustomizationItem { get; }
    public string ValueSpecifier { get; }
    public int? PurchaseLimit { get; }

    public PackConfig(IReadOnlyList<Param> Params, IDictionary<int, CustomizationItemConfig> Items, IDictionary<int, PackCategoryConfig> PackCategories)
    {
        ID = (int)Params[0].AsInt.Value;
        Category = PackCategories[(int)Params[1].AsInt.Value];

        var CurrencyContents = Params[2].AsArray;
        Contents_Currency = new Dictionary<CurrencyType, int>();
        for (int i = 0; i * 2 < CurrencyContents.Count; ++i)
            Contents_Currency.Add((CurrencyType)Enum.Parse(typeof(CurrencyType), CurrencyContents[i * 2].AsString, true), (int)CurrencyContents[i * 2 + 1].AsUInt.Value);

        var ItemContents = Params[3].AsArray;
        Contents_CustomizationItem = new HashSet<CustomizationItemConfig>();
        for (int i = 0; i < ItemContents.Count; ++i)
            Contents_CustomizationItem.Add(Items[(int)ItemContents[i].AsInt.Value]);

        ImageID = Params[4].AsString;
        Name = Params[5].AsString;
        IabSku = Params[7].AsString;
        PriceCurrency = (CurrencyType)Enum.Parse(typeof(CurrencyType), Params[8].AsString, true);
        Price = PriceCurrency == CurrencyType.IRR ? -1 : (int)Params[6].AsUInt.Value;
        PurchaseLimit = (int?)Params[8].AsUInt;
        Tag = Params[10].AsString;
        ValueSpecifier = Params[11].AsString;
        Sort = Params[12].AsFloat.Value;

        Category.Packs.Add(this);
    }

    public void SetPrice(int Price)
    {
        if (PriceCurrency == CurrencyType.IRR)
            this.Price = Price;
    }
}

public class SpinMultiplierConfig
{
    public int ID { get; }
    public int Multiplier { get; }
    public float Chance { get; }

    public SpinMultiplierConfig(IReadOnlyList<Param> Params)
    {
        ID = (int)Params[0].AsInt.Value;
        Multiplier = (int)Params[1].AsUInt.Value;
        Chance = Params[2].AsFloat.Value;
    }

    public override string ToString()
    {
        return $"X{Multiplier}";
    }
}

public class SpinRewardConfig
{
    public int ID { get; }
    public CurrencyType RewardType { get; }
    public int Count { get; }
    public float Chance { get; }

    public SpinRewardConfig(IReadOnlyList<Param> Params)
    {
        ID = (int)Params[0].AsInt.Value;
        RewardType = (CurrencyType)Params[1].AsUInt.Value;
        Count = (int)Params[2].AsUInt.Value;
        Chance = Params[3].AsFloat.Value;
    }

    public override string ToString()
    {
        return $"{Count}{RewardType}";
    }
}

public class LeaderBoardQueryResult
{
    public ulong LifetimeRank { get; }
    public List<LeaderBoardEntry> LifetimeEntries { get; }
    public ulong ThisMonthRank { get; }
    public List<LeaderBoardEntry> ThisMonthEntries { get; }
    public List<LeaderBoardEntry> LastMonthEntries { get; }

    public LeaderBoardQueryResult(IReadOnlyList<Param> Params)
    {
        var Lifetime = Params[0].AsArray;
        LifetimeRank = Lifetime[0].AsUInt.Value;
        LifetimeEntries = new List<LeaderBoardEntry>();
        foreach (var I in Lifetime[1].AsArray)
            LifetimeEntries.Add(new LeaderBoardEntry(I.AsArray));

        var ThisMonth = Params[1].AsArray;
        ThisMonthRank = ThisMonth[0].AsUInt.Value;
        ThisMonthEntries = new List<LeaderBoardEntry>();
        foreach (var I in ThisMonth[1].AsArray)
            ThisMonthEntries.Add(new LeaderBoardEntry(I.AsArray));

        var LastMonth = Params[2].AsArray;
        LastMonthEntries = new List<LeaderBoardEntry>();
        foreach (var I in LastMonth)
            LastMonthEntries.Add(new LeaderBoardEntry(I.AsArray));
    }
}

public class LeaderBoardEntry
{
    public ulong Rank { get; }
    public ulong Score { get; }
    public string Name { get; }
    public IReadOnlyList<int> ActiveCustomizations { get; }

    public LeaderBoardEntry(IReadOnlyList<Param> Params)
    {
        Rank = Params[0].AsUInt.Value;
        Score = Params[1].AsUInt.Value;
        Name = Params[2].AsString;
        ActiveCustomizations = Params[3].AsArray?.Select(p => (int)p.AsInt.Value).ToList();
    }
}

public class GetCheckerHitRewardResult
{
    public ulong NewGold { get; }
    public bool RewardWasGiven { get; }

    public GetCheckerHitRewardResult(IReadOnlyList<Param> @params)
    {
        NewGold = @params[0].AsUInt.Value;
        RewardWasGiven = @params[1].AsBoolean.Value;
    }
}


public class TransientData
{
    static TransientData _Instance;
    public static TransientData Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new TransientData();

            return _Instance;
        }
    }


    public UserProfile UserProfile { get; set; }

    public Dictionary<int, CustomizationItemConfig> CutomizationItems { get; set; }
    public Dictionary<int, GameConfig> Games { get; set; }
    public Dictionary<int, PackCategoryConfig> PackCategories { get; set; }
    public Dictionary<int, PackConfig> Packs { get; set; }
    public Dictionary<int, SpinRewardConfig> SpinRewards { get; set; }
    public Dictionary<int, SpinMultiplierConfig> SpinMultipliers { get; set; }
    public bool IsMultiplayerAllowed { get; set; }
    public uint MaximumNameLength { get; set; }
    public uint VideoAdReward { get; set; }
    public uint NumCheckersToHitPerDayForReward { get; set; }
    public uint CheckerHitRewardPerDay { get; set; }

    public uint ClientLatestVersion { get; set; }
    public uint ClientEarliestSupportedVersion { get; set; }

    public OpponentInfo OpponentInfo { get; set; }
    public int CurrentGameID { get; set; }
}
