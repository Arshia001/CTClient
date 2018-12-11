using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileFrame : MonoBehaviour
{
    static HashSet<ProfileFrame> AllFrames = new HashSet<ProfileFrame>();

    public static void RefreshAll()
    {
        foreach (var PF in AllFrames)
            PF.Refresh();
    }

    public static Sprite GetProfilePicture(IEnumerable<int> Items)
    {
        var CustItems = TransientData.Instance.CutomizationItems;
        string ProfilePictureResourceID;
        var ProfilePictureID = Items.Where(i => CustItems[i].Category == CustomizationItemCategory.ProfileGender).Select(i => (int?)i).FirstOrDefault();
        if (ProfilePictureID == null)
            ProfilePictureResourceID = "profile_unknown";
        else
            ProfilePictureResourceID = CustItems[ProfilePictureID.Value].ResourceID;

        return Resources.Load<Sprite>($"Profile/{ProfilePictureResourceID}");
    }


    int? XPBarFullWidth;


    void Awake()
    {
        AllFrames.Add(this);
    }

    void OnDestroy()
    {
        AllFrames.Remove(this);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var Profile = TransientData.Instance.UserProfile;

        var XPBarTransform = transform.Find("XPBar").transform as RectTransform;
        var Size = XPBarTransform.sizeDelta;
        if (!XPBarFullWidth.HasValue)
            XPBarFullWidth = (int)Size.x;
        Size.x = Mathf.Lerp(0, XPBarFullWidth.Value, Profile.XP / (float)Profile.LevelXP);
        XPBarTransform.sizeDelta = Size;

        transform.Find("Name").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Name ?? "");
        transform.Find("Level").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(Profile.Level.ToString());

        transform.Find("ProfilePicture/Image").GetComponent<Image>().sprite = GetProfilePicture(Profile.ActiveItems);
    }
}
