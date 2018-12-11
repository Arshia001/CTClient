using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class ExtendedInputModule : StandaloneInputModule
{
    static ExtendedInputModule Instance { get; set; }

    public static PointerEventData GetPointerEventData()
    {
        if (Instance.m_PointerData.ContainsKey(-1))
            return Instance.m_PointerData[-1];
        if (Instance.m_PointerData.ContainsKey(0))
            return Instance.m_PointerData[0];
        else
            return null;
    }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;
    }
}
