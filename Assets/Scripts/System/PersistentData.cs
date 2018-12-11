using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentData
{
    static PersistentData _Instance;
    public static PersistentData Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new PersistentData();
            return _Instance;
        }
    }


    Guid? _ClientId;
    public Guid? ClientId
    {
        get { return _ClientId; }
        set
        {
            _ClientId = value;
            PlayerPrefs.SetString(nameof(ClientId), value?.ToString() ?? "");
            PlayerPrefs.Save();
        }
    }


    PersistentData()
    {
        LoadAll();
    }

    void LoadAll()
    {
        Guid TempGuid;
        _ClientId = Guid.TryParse(PlayerPrefs.GetString(nameof(ClientId), ""), out TempGuid) ? TempGuid : default(Guid?);
    }
}
