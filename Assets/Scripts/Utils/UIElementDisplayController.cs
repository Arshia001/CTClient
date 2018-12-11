using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIElementDisplayController : MonoBehaviour
{
    public bool Show { get; set; }


    public float ShowLerpSpeed = 12;

    CanvasGroup group;


    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        group.alpha = Mathf.Lerp(group.alpha, Show ? 1 : 0, Time.deltaTime * ShowLerpSpeed);
        group.interactable = group.blocksRaycasts = group.alpha > 0.7f;
    }
}
