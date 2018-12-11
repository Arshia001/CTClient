using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimSoundPlayer : MonoBehaviour
{
    public bool Mute { get; set; }

    AudioSource Audio;


    void Start()
    {
        Audio = GetComponent<AudioSource>();
    }

    public void PlaySound(AnimationEvent Param)
    {
        if (Mute)
            return;
        var Sound = Param.objectReferenceParameter as AudioClip;
        Audio.clip = Sound;
        Audio.Play();
    }
}
