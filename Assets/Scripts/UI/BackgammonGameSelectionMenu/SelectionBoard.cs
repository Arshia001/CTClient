using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionBoard : MonoBehaviour
{
    RenderTexture RenderTexture;
    Animator Animator;
    Camera Camera;


    public Texture Initialize()
    {
        Animator = GetComponent<Animator>();
        Camera = transform.GetComponentInChildren<Camera>();

        RenderTexture = new RenderTexture(512, 512, 16);
        Camera.targetTexture = RenderTexture;
        return RenderTexture;
    }

    public void Open()
    {
        Animator.SetTrigger("Open");
    }

    void OnDestroy()
    {
        Camera.enabled = false;
        Camera.targetTexture = RenderTexture;
        RenderTexture.DiscardContents();
        RenderTexture = null;
    }
}
