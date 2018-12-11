using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmoteBubble : MonoBehaviour
{
    const float Duration = 2.0f;


    Transform CenterTransform;
    Queue<Emote> Queue = new Queue<Emote>();
    float? EndTime;


    void Awake()
    {
        gameObject.SetActive(false);
        CenterTransform = transform.Find("Center");
    }

    void Update()
    {
        if (EndTime != null && Time.time >= EndTime)
        {
            if (Queue.Count > 0)
                Show(Queue.Dequeue());
            else
            {
                gameObject.SetActive(false);
                EndTime = null;
            }
        }
    }

    public void Enqueue(Emote Emote)
    {
        if (EndTime == null)
            Show(Emote);
        else
            Queue.Enqueue(Emote);
    }

    void Show(Emote Emote)
    {
        for (int i = 0; i < CenterTransform.childCount; ++i)
            Destroy(CenterTransform.GetChild(i).gameObject);

        gameObject.SetActive(true);

        var GO = Instantiate(Emote.gameObject);
        GO.GetComponent<Emote>().InitializeFor(true);
        var Tr = GO.transform;
        Tr.SetParent(CenterTransform, false);
        Tr.localPosition = Vector3.zero;

        EndTime = Time.time + Duration;
    }
}
