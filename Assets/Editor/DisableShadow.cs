using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DisableShadow : Editor
{
    //[MenuItem("Tools/Disable Shadow")]
    //public static void RemoveShadows()
    //{
    //    var Assets = AssetDatabase.FindAssets("t:gameobject", new[] { "Assets/Resources/Customization" })
    //        .Select(s => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(s), typeof(GameObject)));
    //    foreach (GameObject A in Assets)
    //        foreach (var R in A.GetComponentsInChildren<Renderer>())
    //        {
    //            R.receiveShadows = false;
    //            R.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    //        }
    //}
}
