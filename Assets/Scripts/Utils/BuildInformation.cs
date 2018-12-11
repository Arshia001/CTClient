using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildInformation : MonoBehaviour
{
    public static BuildInformation Instance { get; private set; }

    public bool IsNotForReleaseBuild;


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnGUI()
    {
        if (IsNotForReleaseBuild)
        {
            GUI.color = Color.magenta;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * 3);
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "ALPHA BUILD - FOR TESTING ONLY. NOT FOR RELEASE.");
        }
    }
}
