using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomScrollRect : ScrollRect
{
    public bool IsBeingDragged { get; private set; }


    public override void OnBeginDrag(PointerEventData eventData)
    {
        IsBeingDragged = true;
        base.OnBeginDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        IsBeingDragged = false;
        base.OnEndDrag(eventData);
    }
}
