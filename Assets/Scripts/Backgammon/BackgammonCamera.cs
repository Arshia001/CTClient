using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgammonCamera : MonoBehaviour
{
    Camera Camera;

    void Start()
    {
        Camera = GetComponent<Camera>();
    }

    void Update()
    {
        Camera.fieldOfView = Mathf.Lerp(38, 49, Mathf.Clamp01(Mathf.InverseLerp(1.777778f, 1.333333f, Camera.aspect)));
    }
}
