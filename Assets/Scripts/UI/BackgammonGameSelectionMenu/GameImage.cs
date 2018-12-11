using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class GameImage : MonoBehaviour, IPointerClickHandler
{
    const float MinScale = 0.5f;
    const float MaxScale = 1.0f;


    public int GameID { get; set; }

    RectTransform ViewportTransform;
    RectTransform ScalerTransform;


    void Start()
    {
        ViewportTransform = transform.parent.parent as RectTransform;
        ScalerTransform = transform.GetChild(0) as RectTransform;
    }

    void Update()
    {
        var OffsetRatio = 0.5f - Mathf.Abs(ViewportTransform.worldToLocalMatrix.MultiplyPoint(transform.position).x / ViewportTransform.rect.width);
        ScalerTransform.localScale = Vector3.one * Mathf.SmoothStep(MinScale, MaxScale, Mathf.Clamp01((OffsetRatio - 0.25f) / 0.2f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GetComponentInParent<GameSelection>().SetSelectedID(GameID);
    }
}
