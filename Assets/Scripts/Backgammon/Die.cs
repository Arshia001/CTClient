using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = BackgammonLogic.Color;

public class Die : MonoBehaviour
{
    public Vector3[] Rotations =
    {
        new Vector3(float.NaN, 0, 0),
        new Vector3(float.NaN, 0, 90),
        new Vector3(0, -90, float.NaN),
        new Vector3(-180, -90, float.NaN),
        new Vector3(float.NaN, -180, 90),
        new Vector3(float.NaN, -180, 0),
    };

    public Texture2D White, Black; //??

    public void SetDice(Color Color, int Number)
    {
        var R = GetComponentInChildren<Renderer>(); //??
        R.material.mainTexture = Color == Color.White ? White : Black;

        var RotVector = Rotations[Number - 1];
        if (float.IsNaN(RotVector.x))
            RotVector.x = Random.Range(0, 4) * 90;
        if (float.IsNaN(RotVector.y))
            RotVector.y = Random.Range(0, 4) * 90;
        if (float.IsNaN(RotVector.z))
            RotVector.z = Random.Range(0, 4) * 90;
        transform.localRotation = Quaternion.Euler(RotVector.x, RotVector.y, RotVector.z);
    }

    public void SetVisible(bool Visible)
    {
        gameObject.SetActive(Visible);
    }
}
