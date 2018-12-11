using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenEdgeAligner : MonoBehaviour
{
    public float aspect1, x1, aspect2, x2;


    void Start()
    {
        var aspect = Camera.main.aspect;

        var p = transform.localPosition;
        p.x = Mathf.Lerp(x1, x2, Mathf.InverseLerp(aspect1, aspect2, aspect));
        transform.localPosition = p;
    }
}
