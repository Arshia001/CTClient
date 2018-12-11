using BackgammonLogic;
using LightMessage.Client.EndPoints;
using LightMessage.Common.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Network.Backgammon
{
    public enum ReadyResponse
    {
        OK = 1,
        AlreadyInProgress = 2,
        NotInGame = 3
    }

    [RequireComponent(typeof(ConnectionManager))]
    public class MatchmakingEndPoint : MonoBehaviour, IEndPointHandler
    {
        public delegate void OnJoinGameDelegate(OpponentInfo OpponentInfo, ulong TotalGold);
        public event OnJoinGameDelegate OnJoinGame;

        public delegate void OnRemovedFromQueueDelegate();
        public event OnRemovedFromQueueDelegate OnRemovedFromQueue;


        EndPointProxy EndPoint;


        void IEndPointHandler.SetupHub(ConnectionManager ConnectionManager)
        {
            EndPoint = ConnectionManager.Client.CreateProxy("mmk");
            EndPoint.On("join", OnJoinGameImpl);
            EndPoint.On("kick", OnRemovedFromQueueImpl);
        }

        void OnJoinGameImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnJoinGame?.Invoke(new OpponentInfo(Params), Params[3].AsUInt.Value));
        }

        void OnRemovedFromQueueImpl(IReadOnlyList<Param> Params)
        {
            MainThreadDispatcher.Instance.Enqueue(() => OnRemovedFromQueue?.Invoke());
        }

        public Task EnterQueue(int GameID)
        {
            return EndPoint.SendInvocationForReply("queue", CancellationToken.None, Param.Int(GameID));
        }

        public Task LeaveQueue()
        {
            return EndPoint.SendInvocation("leave", CancellationToken.None);
        }

        public async Task<ReadyResponse> ClientReady()
        {
            var Res = await EndPoint.SendInvocationForReply("ready", CancellationToken.None);
            return (ReadyResponse)Res[0].AsUInt.Value;
        }
    }
}
