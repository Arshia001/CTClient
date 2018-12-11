using Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EditProfileMenu : MonoBehaviour
{
    public async void MakeChoice(bool Female)
    {
        var Item = TransientData.Instance.CutomizationItems
            .Where(kv => kv.Value.Category == CustomizationItemCategory.ProfileGender && kv.Value.ResourceID.Contains(Female ? "_female" : "_male"))
            .FirstOrDefault();

        if (Item.Value != null)
            TransientData.Instance.UserProfile.ActiveItems = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetActiveCustomizations(new List<int> { Item.Value.ID });

        ProfileFrame.RefreshAll();

        gameObject.SetActive(false);
    }
}
