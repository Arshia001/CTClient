using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerLights : MonoBehaviour
{
    public bool IsSpinnerRotating;

    Image[] Lights;


    void Start()
    {
        Lights = new Image[transform.childCount];

        for (int Idx = 0; Idx < Lights.Length; ++Idx)
            Lights[Idx] = transform.Find($"{Idx}").GetComponent<Image>();
    }

    void Update()
    {
        if (IsSpinnerRotating)
        {
            for (int Idx = 0; Idx < Lights.Length; ++Idx)
            {
                Lights[Idx].color = Color.Lerp(Lights[Idx].color, new Color(1, 1, 1, 1 - ((Time.time * 12 + 100 - Idx) % 12) / 12.0f), Time.deltaTime * 10f);
            }
        }
        else
        {
            for (int Idx = 0; Idx < Lights.Length; ++Idx)
            {
                Lights[Idx].color = Color.Lerp(Lights[Idx].color, new Color(1, 1, 1, Mathf.FloorToInt(Time.time % 3) == Idx % 3 ? 1 : 0), Time.deltaTime * 10f);
            }
        }
    }
}
