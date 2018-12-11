using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;

public class LogVisualizer : MonoBehaviour
{
    class LogEntry
    {
        public string LogString;
        public string StackTrace;
        public LogType Type;
        public System.DateTime Time;

        public string GetString(bool bShowStackTrace)
        {
            string Result;
            Result = "<color=" + LogVisualizer.GetColor(Type) + ">" + Type.ToString() + ": " + LogString;
            if (bShowStackTrace)
                Result += "\n" + StackTrace + "---------------------------------------------";
            Result += "</color>\n";
            return Result;
        }

        public string GetFileEntry(bool forceIncludeStackTrace)
        {
            string Result;
            Result = "(" +
#if UNITY_WSA
                Time.ToString()
#else
                Time.ToLongTimeString()
#endif
                + ") " + Type.ToString() + ": " + LogString + "\r\n";
            if (Debug.isDebugBuild || forceIncludeStackTrace)
                Result += StackTrace + "---------------------------------------------" + "\r\n";
            return Result;
        }
    }

    static LogVisualizer Instance;

    static string GetColor(LogType Type)
    {
        switch (Type)
        {
            case LogType.Assert:
                return "gray";
            case LogType.Error:
                return "red";
            case LogType.Exception:
                return "magenta";
            case LogType.Log:
                return "lime";
            case LogType.Warning:
                return "yellow";
            default:
                return "white";
        }
    }


    LinkedList<LogEntry> LogList = new LinkedList<LogEntry>();

    public int[] ActivationSequence;
    public int[] ToggleStackTraceSequence;
    public bool bDisableLogInReleaseBuilds = true;
    public bool StartWithLogEnabledInDebugBuilds = true;
    public bool EnableStackTraceInReleaseBuild = true;

    LinkedList<int> TouchCountList = new LinkedList<int>();
    bool bShowLog, bShowStackTrace, bDontEverShowLog;
    string LogPath;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Debug.isDebugBuild)
            bShowLog = StartWithLogEnabledInDebugBuilds;
        else
        {
            if (bDisableLogInReleaseBuilds)
                bDontEverShowLog = true;
        }

        LogPath = Path.Combine(Application.temporaryCachePath, DateTime.Now.ToString("yyyy-MM-dd_hh-mm") + ".txt");

        Application.logMessageReceived += Application_logMessageReceived;
        //Application.logMessageReceivedThreaded += Application_logMessageReceived;

        gameObject.AddComponent<TouchCountDetector>().TouchCountDetectedEvent += TouchCountDetected;

        Debug.Log("Log visualizer active.");
    }

    void Application_logMessageReceived(string Log, string Trace, LogType Type)
    {
        lock (LogList)
        {
            LogList.AddLast(new LogEntry() { LogString = Log, StackTrace = Trace, Type = Type, Time = System.DateTime.Now });
            while (LogList.Count > 80)
                LogList.RemoveFirst();

            if (Application.platform != RuntimePlatform.IPhonePlayer)
                File.AppendAllText(LogPath, LogList.Last.Value.GetFileEntry(EnableStackTraceInReleaseBuild));
        }
    }

    void TouchCountDetected(int Count)
    {
        TouchCountList.AddLast(Count);
        while (TouchCountList.Count > 10)
            TouchCountList.RemoveFirst();

        if (CountListMatchesSequence(TouchCountList, ActivationSequence))
            bShowLog = !bShowLog;
        if (CountListMatchesSequence(TouchCountList, ToggleStackTraceSequence))
            bShowStackTrace = !bShowStackTrace;
    }

    bool CountListMatchesSequence(LinkedList<int> List, int[] Sequence)
    {
        if (List.Count >= Sequence.Length)
        {
            int Idx = Sequence.Length - 1;
            var Node = List.Last;
            while (Idx >= 0)
            {
                if (Sequence[Idx] != Node.Value)
                    break;

                if (Idx == 0) //If all matched
                    return true;

                --Idx;
                Node = Node.Previous;
            }
        }

        return false;
    }

    void OnGUI()
    {
        if (bShowLog && !bDontEverShowLog)
        {
            GUILayout.BeginVertical();
            foreach (var LogEntry in LogList.Reverse())
                GUILayout.Label(LogEntry.GetString(bShowStackTrace));
            GUILayout.EndVertical();
        }
    }
}
