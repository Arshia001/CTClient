using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using BackgammonLogic;
using LightMessage.Client.EndPoints;
using LightMessage.Common.Messages;
using System.Threading;
using Color = BackgammonLogic.Color;
using System.Linq;
using LightMessage.Common.Util;

namespace Network.Backgammon
{
    public enum GameOverReason
    {
        GameEndedNormally = 1,
        Inactivity = 2,
        Concede = 3,
        FailedToJoinInTime = 4
    }

    [RequireComponent(typeof(ConnectionManager))]
    public class BackgammonGameEndPoint : MonoBehaviour, IEndPointHandler
    {
        public class MatchStateContainer
        {
            public class GameStateContainer
            {
                public Color Turn;
                public GameState State;
                public sbyte[] Board;
                public byte[] Bar;
                public byte[] BorneOff;
                public byte[] RolledDice;
                public byte[] RemainingDice;
                public byte Stamp;
                public IEnumerable<Move> CurrentTurnMoves;
            }

            public GameStateContainer GameState;
            public Color MyColor;
            public TimeSpan RemainingTime;
            public OpponentInfo OpponentInfo;
            public int GameID;
        }

        public delegate void OnStartGameDelegate(byte MyColor, OpponentInfo OpponentInfo, int GameID);
        public event OnStartGameDelegate OnStartGame;

        public delegate void OnInitDiceRolledDelegate(byte My, byte Their, byte Stamp);
        public event OnInitDiceRolledDelegate OnInitDiceRolled;

        public delegate void OnStartTurnDelegate(bool My, byte Stamp, TimeSpan TurnTime);
        public event OnStartTurnDelegate OnStartTurn;

        public delegate void OnDiceRolledDelegate(bool My, byte Roll1, byte Roll2, byte Stamp);
        public event OnDiceRolledDelegate OnDiceRolled;

        public delegate void OnOpponentUndoDelegate(byte stamp);
        public event OnOpponentUndoDelegate OnOpponentUndo;

        public delegate void OnOpponentMovedDelegate(sbyte From, sbyte To, byte Stamp);
        public event OnOpponentMovedDelegate OnOpponentMoved;

        public delegate void OnForcedOwnMoveDelegate(sbyte From, sbyte To, byte Stamp);
        public event OnForcedOwnMoveDelegate OnForcedOwnMove;

        public delegate void OnMatchResultDelegate(bool MyWin, GameOverReason reason, ulong TotalGold, ulong TotalGems, uint TotalXP, uint Level, uint LevelUpDeltaGold, uint LevelUpDeltaGems, uint LevelXP, uint[] UpdatedStatistics);
        public event OnMatchResultDelegate OnMatchResult;

        public delegate void OnEmoteDelegate(int EmoteID);
        public event OnEmoteDelegate OnEmote;


        EndPointProxy EndPoint;


        void IEndPointHandler.SetupHub(ConnectionManager ConnectionManager)
        {
            EndPoint = ConnectionManager.Client.CreateProxy("bkgm");
            EndPoint.On("start", OnStartGameImpl);
            EndPoint.On("initdice", OnInitDiceRolledImpl);
            EndPoint.On("turn", OnStartTurnImpl);
            EndPoint.On("dice", OnDiceRolledImpl);
            EndPoint.On("undo", OnOpponentUndoImpl);
            EndPoint.On("opmove", OnOpponentMovedImpl);
            EndPoint.On("forceme", OnForcedOwnMoveImpl);
            EndPoint.On("result", OnMatchResultImpl);
            EndPoint.On("emote", OnEmoteImpl);
        }

        void OnStartGameImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnStartGame?.Invoke((byte)Params[0].AsUInt, new OpponentInfo(Params[1].AsArray), (int)Params[2].AsInt.Value));
        }

        void OnInitDiceRolledImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnInitDiceRolled?.Invoke((byte)Params[0].AsUInt, (byte)Params[1].AsUInt, (byte)Params[2].AsUInt));
        }

        void OnStartTurnImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnStartTurn?.Invoke(Params[0].AsBoolean.Value, (byte)Params[1].AsUInt, Params[2].AsTimeSpan.Value));
        }

        void OnDiceRolledImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnDiceRolled?.Invoke(Params[0].AsBoolean.Value, (byte)Params[1].AsUInt, (byte)Params[2].AsUInt, (byte)Params[3].AsUInt));
        }

        void OnOpponentUndoImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnOpponentUndo?.Invoke((byte)Params[0].AsUInt));
        }

        void OnOpponentMovedImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnOpponentMoved?.Invoke((sbyte)Params[0].AsInt, (sbyte)Params[1].AsInt, (byte)Params[2].AsUInt));
        }

        void OnForcedOwnMoveImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnForcedOwnMove?.Invoke((sbyte)Params[0].AsInt, (sbyte)Params[1].AsInt, (byte)Params[2].AsUInt));
        }

        void OnMatchResultImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnMatchResult?.Invoke(
                Params[0].AsBoolean.Value,
                (GameOverReason)Params[1].AsUInt.Value,
                Params[2].AsUInt.Value,
                Params[3].AsUInt.Value,
                (uint)Params[4].AsUInt.Value,
                (uint)Params[5].AsUInt.Value,
                (uint)Params[6].AsUInt.Value,
                (uint)Params[7].AsUInt.Value,
                (uint)Params[8].AsUInt.Value,
                Params[9].AsArray.Select(p => (uint)p.AsUInt.Value).ToArray()
                ));
        }

        void OnEmoteImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnEmote?.Invoke((int)Params[0].AsInt.Value));
        }

        public Task<bool> RollDice(byte Stamp)
        {
            return EndPoint.SendInvocationForReply("dice", CancellationToken.None, Param.UInt(Stamp))
                .ConvertResult(Params => Params[0].AsBoolean.Value);
        }

        public Task<bool> EndTurn(byte Stamp)
        {
            return EndPoint.SendInvocationForReply("turn", CancellationToken.None, Param.UInt(Stamp))
                .ConvertResult(Params => Params[0].AsBoolean.Value);
        }

        public Task<bool> UndoLastMove(byte Stamp)
        {
            return EndPoint.SendInvocationForReply("undo", CancellationToken.None, Param.UInt(Stamp))
                .ConvertResult(Params => Params[0].AsBoolean.Value);
        }

        public Task<byte> MakeMove(IEnumerable<Tuple<sbyte, sbyte>> Moves, byte Stamp)
        {
            return EndPoint.SendInvocationForReply("move", CancellationToken.None,
                Param.Array(Moves.Select(t => Param.Array(Param.Int(t.Item1), Param.Int(t.Item2)))),
                Param.UInt(Stamp)
                ).ConvertResult(Params => (byte)Params[0].AsUInt.Value);
        }

        public Task<MatchStateContainer> GetGameState()
        {
            return EndPoint.SendInvocationForReply("state", CancellationToken.None).ConvertResult(Params =>
                Params.Count == 0 ? default(MatchStateContainer) : new MatchStateContainer
                {
                    GameState = Params[0].IsNull ? null : new MatchStateContainer.GameStateContainer
                    {
                        Turn = Params[0].AsArray[0].AsUInt == Color.White.AsIndex() ? Color.White : Color.Black,
                        State = (GameState)Params[0].AsArray[1].AsUInt.Value,
                        Board = Params[0].AsArray[2].AsArray.Select(s => (sbyte)s.AsInt.Value).ToArray(),
                        Bar = Params[0].AsArray[3].AsArray.Select(s => (byte)s.AsUInt.Value).ToArray(),
                        BorneOff = Params[0].AsArray[4].AsArray.Select(s => (byte)s.AsUInt.Value).ToArray(),
                        RolledDice = Params[0].AsArray[5].AsArray.Select(s => (byte)s.AsUInt.Value).ToArray(),
                        RemainingDice = Params[0].AsArray[6].AsArray.Select(s => (byte)s.AsUInt.Value).ToArray(),
                        Stamp = (byte)Params[0].AsArray[7].AsUInt.Value,
                        CurrentTurnMoves = Params[0].AsArray[8].AsArray.Select(a => new Move((sbyte)a.AsArray[0].AsInt.Value, (sbyte)a.AsArray[1].AsInt.Value, a.AsArray[2].AsBoolean.Value, (byte)a.AsArray[3].AsUInt.Value))
                    },
                    MyColor = Params[1].AsUInt == Color.White.AsIndex() ? Color.White : Color.Black,
                    RemainingTime = Params[2].AsTimeSpan.Value,
                    OpponentInfo = new OpponentInfo(Params[3].AsArray),
                    GameID = (int)Params[4].AsInt.Value
                });
        }

        public Task<OpponentInfo> GetOpponentInfo()
        {
            return EndPoint.SendInvocationForReply("opp", CancellationToken.None).ConvertResult(Params =>
                Params.Count == 0 ? default(OpponentInfo) : new OpponentInfo(Params));
        }

        public void Emote(int EmoteID)
        {
            EndPoint.SendInvocationForReply("emote", CancellationToken.None, Param.Int(EmoteID)).DontCare();
        }

        internal Task Concede()
        {
            return EndPoint.SendInvocation("concede", CancellationToken.None);
        }
    }
}
