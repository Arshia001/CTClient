using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = BackgammonLogic.Color;

public class CheckerDeco : MonoBehaviour
{
    static UnityEngine.Color ImageColorWhite = new Color32(54, 31, 11, 255), ImageColorBlack = new Color32(198, 132, 57, 255);

    public Material WhiteMaterial, BlackMaterial;
    Renderer Renderer;


    public void UpdateDeco(Color Color, IEnumerable<int> ActiveItems)
    {
        if (Renderer == null)
            Renderer = GetComponent<Renderer>();

        Renderer.material = Color == Color.Black ? BlackMaterial : WhiteMaterial;

        for (int Idx = transform.childCount - 1; Idx >= 0; --Idx)
            Destroy(transform.GetChild(Idx).gameObject);

        var AllItems = TransientData.Instance.CutomizationItems;
        foreach (var I in ActiveItems)
        {
            try
            {
                var Item = AllItems[I];
                switch (Item.Category)
                {
                    case CustomizationItemCategory.CheckerFrame:
                    case CustomizationItemCategory.CheckerGem:
                    case CustomizationItemCategory.CheckerImage:
                        break;
                    default:
                        continue;
                }

                var GO = Instantiate(Resources.Load<GameObject>("Customization/" + Item.ResourceID));
                GO.transform.parent = transform;
                GO.transform.localPosition = Vector3.zero;
                GO.transform.localRotation = Quaternion.identity;
                SetLayer(GO, gameObject.layer);

                if (Item.Category == CustomizationItemCategory.CheckerImage)
                {
                    var Renderer = GO.GetComponentInChildren<SpriteRenderer>();
                    if (Renderer != null)
                        Renderer.color = Color == Color.White ? ImageColorWhite : ImageColorBlack;
                }
            }
            catch (System.Exception Ex)
            {
                Debug.LogError("Failed to create deco " + I.ToString() + " due to " + Ex.ToString());
            }
        }
    }

    void SetLayer(GameObject GO, int Layer)
    {
        GO.layer = Layer;
        foreach (Transform Tr in GO.transform)
            SetLayer(Tr.gameObject, Layer);
    }
}
