using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PackButton : MonoBehaviour, IPointerClickHandler
{
    public int PackID;


    public void OnPointerClick(PointerEventData eventData)
    {
        GetComponentInParent<ShopMenu>().MakePurchase(PackID);
    }
}
