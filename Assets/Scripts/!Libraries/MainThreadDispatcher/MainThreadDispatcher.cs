using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

// Based VERY loosely on https://github.com/PimDeWitte/UnityMainThreadDispatcher/
public class MainThreadDispatcher : MonoBehaviour
{
    interface IDispatcher
    {
        void ExecuteAll(MainThreadDispatcher Owner);
    }

    class ActionDispatcher : IDispatcher
    {
        static ActionDispatcher _Instance;
        public static ActionDispatcher Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ActionDispatcher();
                return _Instance;
            }
        }

        public static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

        void IDispatcher.ExecuteAll(MainThreadDispatcher Owner)
        {
            while (ExecutionQueue.Count > 0)
                try
                {
                    ExecutionQueue.Dequeue().Invoke();
                }
                catch (Exception Ex)
                {
                    Debug.LogException(Ex);
                }

            ExecutionQueue.Clear();
        }
    }

    class CoroutineDispatcher : IDispatcher
    {
        static CoroutineDispatcher _Instance;
        public static CoroutineDispatcher Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new CoroutineDispatcher();
                return _Instance;
            }
        }

        public static readonly Queue<IEnumerator> ExecutionQueue = new Queue<IEnumerator>();

        void IDispatcher.ExecuteAll(MainThreadDispatcher Owner)
        {
            while (ExecutionQueue.Count > 0)
                try
                {
                    Owner.StartCoroutine(ExecutionQueue.Dequeue());
                }
                catch (Exception Ex)
                {
                    Debug.LogException(Ex);
                }

            ExecutionQueue.Clear();
        }
    }

    class FuncDispatcher<T> : IDispatcher
    {
        static FuncDispatcher<T> _Instance;
        public static FuncDispatcher<T> Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new FuncDispatcher<T>();
                return _Instance;
            }
        }

        public class ExecItem
        {
            public TaskCompletionSource<T> TCS;
            public Func<T> Func;
        }
        public static readonly Queue<ExecItem> ExecutionQueue = new Queue<ExecItem>();

        void IDispatcher.ExecuteAll(MainThreadDispatcher Owner)
        {
            while (ExecutionQueue.Count > 0)
            {
                var F = ExecutionQueue.Dequeue();
                try
                {
                    F.TCS.SetResult(F.Func.Invoke());
                }
                catch (Exception Ex)
                {
                    F.TCS.SetException(Ex);
                }
            }

            ExecutionQueue.Clear();
        }
    }


    // This can NOT automaticaly create the instance because it may be
    // called from a background thread (which is the entire point of
    // this class, btw) and fail to create the instance.
    public static MainThreadDispatcher Instance { get; private set; }

    static readonly HashSet<IDispatcher> Dispatchers = new HashSet<IDispatcher>();


    static bool IsMainThread()
    {
        return !System.Threading.Thread.CurrentThread.IsBackground;
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void Update()
    {
        lock (Dispatchers)
        {
            foreach (var H in Dispatchers)
                H.ExecuteAll(this);

            Dispatchers.Clear();
        }
    }

    public void Enqueue(Action Action)
    {
        if (IsMainThread())
            Action();
        else
            lock (Dispatchers)
            {
                ActionDispatcher.ExecutionQueue.Enqueue(Action);
                Dispatchers.Add(ActionDispatcher.Instance);
            }
    }

    public void Enqueue(IEnumerator Coroutine)
    {
        if (IsMainThread())
            StartCoroutine(Coroutine);
        else
            lock (Dispatchers)
            {
                CoroutineDispatcher.ExecutionQueue.Enqueue(Coroutine);
                Dispatchers.Add(CoroutineDispatcher.Instance);
            }
    }

    public Task<T> Enqueue<T>(Func<T> Action)
    {
        if (IsMainThread())
            return Task.FromResult(Action());
        else
            lock (Dispatchers)
            {
                var TCS = new TaskCompletionSource<T>();
                FuncDispatcher<T>.ExecutionQueue.Enqueue(new FuncDispatcher<T>.ExecItem { Func = Action, TCS = TCS });
                Dispatchers.Add(FuncDispatcher<T>.Instance);
                return TCS.Task;
            }
    }
}