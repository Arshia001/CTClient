using LightMessage.Client.EndPoints;
using LightMessage.Common.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network
{
    [RequireComponent(typeof(ConnectionManager))]
    public class SystemEndPoint : MonoBehaviour, IEndPointHandler
    {
        EndPointProxy EndPoint;


        public void SetupHub(ConnectionManager ConnectionManager)
        {
            EndPoint = ConnectionManager.Client.CreateProxy("sys");
        }

        public async Task<bool> GetProfileInfo()
        {
            var Res = await EndPoint.SendInvocationForReply("prof", CancellationToken.None);
            TransientData.Instance.UserProfile = new UserProfile(Res);
            return Res[9].AsBoolean.Value;
        }

        /// <summary>
        ///  The results will be available from <see cref="TransientData"/>
        /// </summary>
        /// <returns>Whether the player is currently in a game and should attempt to rejoin immediately</returns>
        public async Task<bool> GetStartupInfo()
        {
            var Res = await EndPoint.SendInvocationForReply("start", CancellationToken.None);

            var T = TransientData.Instance;

            T.CutomizationItems = new Dictionary<int, CustomizationItemConfig>();
            var Array = Res[1].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new CustomizationItemConfig(Array[i].AsArray);
                T.CutomizationItems.Add(Item.ID, Item);
            }

            T.Games = new Dictionary<int, GameConfig>();
            Array = Res[2].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new GameConfig(Array[i].AsArray);
                T.Games.Add(Item.ID, Item);
            }

            T.PackCategories = new Dictionary<int, PackCategoryConfig>();
            Array = Res[3].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new PackCategoryConfig(Array[i].AsArray);
                T.PackCategories.Add(Item.ID, Item);
            }

            T.Packs = new Dictionary<int, PackConfig>();
            Array = Res[4].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new PackConfig(Array[i].AsArray, T.CutomizationItems, T.PackCategories);
                T.Packs.Add(Item.ID, Item);
            }

            T.SpinRewards = new Dictionary<int, SpinRewardConfig>();
            Array = Res[5].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new SpinRewardConfig(Array[i].AsArray);
                T.SpinRewards.Add(Item.ID, Item);
            }

            T.SpinMultipliers = new Dictionary<int, SpinMultiplierConfig>();
            Array = Res[6].AsArray;
            for (int i = 0; i < Array.Count; ++i)
            {
                var Item = new SpinMultiplierConfig(Array[i].AsArray);
                T.SpinMultipliers.Add(Item.ID, Item);
            }

            T.IsMultiplayerAllowed = Res[7].AsBoolean.Value;
            T.MaximumNameLength = (uint)Res[8].AsUInt.Value;
            T.VideoAdReward = (uint)Res[9].AsUInt.Value;
            T.NumCheckersToHitPerDayForReward = (uint)Res[10].AsUInt.Value;
            T.CheckerHitRewardPerDay = (uint)Res[11].AsUInt.Value;

            T.UserProfile = new UserProfile(Res[0].AsArray);

            return Res[0].AsArray[9].AsBoolean.Value;
        }

        public async Task<VersionInfo> GetClientVersion()
        {
            var Res = await EndPoint.SendInvocationForReply("ver", CancellationToken.None);

            return new VersionInfo { Latest = (uint)Res[0].AsUInt.Value, EarliestSupported = (uint)Res[1].AsUInt.Value };
        }

        public async Task<List<int>> SetActiveCustomizations(List<int> Items)
        {
            var Result = await EndPoint.SendInvocationForReply("cust", CancellationToken.None, Param.Array(Items.Select(i => Param.Int(i))));
            return Result[0].AsArray.Select(p => (int)p.AsInt.Value).ToList();
        }

        public async Task<PurchaseResult> PurchaseCustomizationItem(int ItemID)
        {
            var Res = await EndPoint.SendInvocationForReply("buyci", CancellationToken.None, Param.Int(ItemID));
            return new PurchaseResult(Res);
        }

        public async Task<PurchaseResult> PurchasePack(int PackID)
        {
            var Res = await EndPoint.SendInvocationForReply("buyp", CancellationToken.None, Param.Int(PackID));
            return new PurchaseResult(Res);
        }

        public async Task<PurchaseResult> PurchasePackWithIab(int PackID, string Token)
        {
            var Res = await EndPoint.SendInvocationForReply("buyiab", CancellationToken.None, Param.Int(PackID), Param.String(Token));
            return new PurchaseResult(Res);
        }

        public async Task<int> RollSpinner()
        {
            var Res = await EndPoint.SendInvocationForReply("spin", CancellationToken.None);

            var Profile = TransientData.Instance.UserProfile;
            Profile.Gold = Res[1].AsUInt.Value;
            Profile.Gems = Res[2].AsUInt.Value;
            Profile.NextSpinTime = DateTime.Now + Res[3].AsTimeSpan.Value;

            return (int)Res[0].AsInt.Value;
        }

        public async Task<int> RollMultiplierSpinner(string VideoAdID)
        {
            var Res = await EndPoint.SendInvocationForReply("spinm", CancellationToken.None, Param.String(VideoAdID));

            var Profile = TransientData.Instance.UserProfile;
            Profile.Gold = Res[1].AsUInt.Value;
            Profile.Gems = Res[2].AsUInt.Value;

            return (int)Res[0].AsInt.Value;
        }

        public async Task<LeaderBoardQueryResult> GetLeaderBoard()
        {
            var Res = await EndPoint.SendInvocationForReply("lb", CancellationToken.None);

            return new LeaderBoardQueryResult(Res);
        }

        public async Task TakeVideoAdReward(string VideoAdID)
        {
            var Res = await EndPoint.SendInvocationForReply("vid", CancellationToken.None, Param.String(VideoAdID));
            TransientData.Instance.UserProfile.Gold = Res[0].AsUInt.Value;
        }

        public async Task<bool> SetName(string Name)
        {
            var Res = (await EndPoint.SendInvocationForReply("name", CancellationToken.None, Param.String(Name)))[0].AsBoolean.Value;

            if (Res)
                TransientData.Instance.UserProfile.Name = Name;

            return Res;
        }

        public async Task<GetCheckerHitRewardResult> GetCheckerHitReward()
        {
            var res = new GetCheckerHitRewardResult(await EndPoint.SendInvocationForReply("hitreward", CancellationToken.None));

            TransientData.Instance.UserProfile.Gold = res.NewGold;

            return res;
        }
    }
}
