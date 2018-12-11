using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppVersion : MonoBehaviour
{
    public static AppVersion Instance { get; private set; }


    public int Version;


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
}
