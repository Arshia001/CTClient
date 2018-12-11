using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossiblePlayMarker : MonoBehaviour
{
    public Color NormalMoveColor, TakeMoveColor;

    Material Mat;
    bool Visible;
    float FullAlpha;
    Quaternion BaseRotation;


    void Start()
    {
        Mat = GetComponent<Renderer>().material;
        FullAlpha = 0;
        BaseRotation = transform.rotation;
    }

    void Update()
    {
        var Color = Mat.color;
        var A = Mathf.MoveTowards(Color.a, Visible ? FullAlpha : 0, Time.deltaTime * 7);
        if (Color.a != A)
        {
            Color.a = A;
            Mat.color = Color;
        }
    }

    public void ShowAt(Tuple<Vector3, Quaternion> Transform, bool IsTakeMove)
    {
        transform.position = Transform.Item1;
        transform.rotation = Transform.Item2 * BaseRotation;
        transform.localScale = Vector3.one * (IsTakeMove ? 1.1f : 1.0f);
        Visible = true;

        var C = IsTakeMove ? TakeMoveColor : NormalMoveColor;
        C.a = 0;
        Mat.color = C;
        FullAlpha = IsTakeMove ? TakeMoveColor.a : NormalMoveColor.a;
    }

    public void Hide()
    {
        Visible = false;
    }
}
