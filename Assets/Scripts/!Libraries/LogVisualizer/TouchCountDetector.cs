using UnityEngine;
using System.Collections;

/// <summary>
/// This class detects multiple touches and reports the count.
/// It's useful for detecting sequences of many touches, for
/// example: 4-3-4 touches in a sequence to open a log window.
/// </summary>
public class TouchCountDetector : MonoBehaviour
{
    public delegate void TouchCountDetected(int Count);
    public event TouchCountDetected TouchCountDetectedEvent;

    int LastFrameTouchCount;
    bool bTouchCountIncreasing;

    void Update()
    {
        int TouchCount = Input.touchCount;

#if UNITY_EDITOR
        if (TouchCount == 0)
        {
            if (Input.GetKey(KeyCode.Mouse0))
                TouchCount += 1;
            if (Input.GetKey(KeyCode.Mouse1))
                TouchCount += 1;
            if (Input.GetKey(KeyCode.Mouse2))
                TouchCount += 1;
            if (Input.GetKey(KeyCode.Mouse3))
                TouchCount += 1;
            if (Input.GetKey(KeyCode.Mouse4))
                TouchCount += 1;
            if (Input.GetKey(KeyCode.Mouse5))
                TouchCount += 1;
        }
#endif

        if (TouchCount > LastFrameTouchCount)
        {
            bTouchCountIncreasing = true;
        }
        else if (TouchCount < LastFrameTouchCount && bTouchCountIncreasing)
        {
            ReportCount(LastFrameTouchCount);
            bTouchCountIncreasing = false;
        }

        LastFrameTouchCount = TouchCount;
    }

    void ReportCount(int Count)
    {
        if (TouchCountDetectedEvent != null)
            TouchCountDetectedEvent.Invoke(Count);
    }
}
