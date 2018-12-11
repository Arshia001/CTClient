using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollbarSizeFixer : MonoBehaviour
{
    public float Size;

    void Awake()
    {
        GetComponent<Scrollbar>().size = Size;
    }

    void Update()
    {
        GetComponent<Scrollbar>().size = Size;
    }
}
