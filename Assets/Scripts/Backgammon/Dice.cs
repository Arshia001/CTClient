using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = BackgammonLogic.Color;

public class Dice : MonoBehaviour
{
    const float PerRollWaitTime = 3.0f;
    const float AnimDoneOffset = 1.0f;


    public static Dice Instance { get; private set; }


    Die OwnDie1, OwnDie2;
    Die OpponentDie1, OpponentDie2;
    Animator OwnAnimator, OpponentAnimator;
    AnimSoundPlayer OpponentSoundPlayer;

    public Color OwnColor { get; set; }

    Queue<Tuple<Color, int, Color, int>> AnimQueue = new Queue<Tuple<Color, int, Color, int>>();
    float CurrentRollEndTime = float.MinValue;


    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        var Own = transform.Find("Self");
        OwnDie1 = Own.GetComponentsInChildren<Die>()[0];
        OwnDie2 = Own.GetComponentsInChildren<Die>()[1];
        OwnAnimator = Own.GetComponent<Animator>();

        var Opponent = transform.Find("Opponent");
        OpponentDie1 = Opponent.GetComponentsInChildren<Die>()[0];
        OpponentDie2 = Opponent.GetComponentsInChildren<Die>()[1];
        OpponentAnimator = Opponent.GetComponent<Animator>();
        OpponentSoundPlayer = Opponent.GetComponent<AnimSoundPlayer>();
    }

    void Update()
    {
        if (Time.time >= CurrentRollEndTime && AnimQueue.Count > 0)
        {
            var R = AnimQueue.Dequeue();
            RollTo_Impl(R.Item1, R.Item2, R.Item3, R.Item4);
        }
    }

    public void RollTo(Color Color1, int Number1, Color Color2, int Number2) //?? add notification for used dice, not playable, etc.
    {
        if (Time.time >= CurrentRollEndTime)
            RollTo_Impl(Color1, Number1, Color2, Number2);
        else
            AnimQueue.Enqueue(new Tuple<Color, int, Color, int>(Color1, Number1, Color2, Number2));
    }

    void RollTo_Impl(Color Color1, int Number1, Color Color2, int Number2)
    {
        if (Color1 == Color2)
        {
            OpponentSoundPlayer.Mute = false;
            if (Color1 == OwnColor)
            {
                OpponentAnimator.SetTrigger("Clear");
                OwnDie1.SetDice(Color1, Number1);
                OwnDie2.SetVisible(true);
                OwnDie2.SetDice(Color1, Number2);
                OwnAnimator.SetTrigger("Roll");
            }
            else
            {
                OwnAnimator.SetTrigger("Clear");
                OpponentDie1.SetDice(Color1, Number1);
                OpponentDie2.SetVisible(true);
                OpponentDie2.SetDice(Color1, Number2);
                OpponentAnimator.SetTrigger("Roll");
            }
        }
        else
        {
            OpponentSoundPlayer.Mute = true;
            if (Color1 == OwnColor)
            {
                OwnDie1.SetDice(Color1, Number1);
                OpponentDie1.SetDice(Color2, Number2);
            }
            else
            {
                OwnDie1.SetDice(Color2, Number2);
                OpponentDie1.SetDice(Color1, Number1);
            }

            OwnDie2.SetVisible(false);
            OpponentDie2.SetVisible(false);

            OwnAnimator.SetTrigger("Roll");
            OpponentAnimator.SetTrigger("Roll");
        }

        CurrentRollEndTime = Time.time + PerRollWaitTime;
    }

    public bool IsAnimDone()
    {
        return AnimQueue.Count == 0 && Time.time > CurrentRollEndTime - AnimDoneOffset;
    }
}
