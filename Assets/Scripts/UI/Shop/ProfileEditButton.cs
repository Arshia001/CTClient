using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfileEditButton : MonoBehaviour
{
    Image Image;

    void Start()
    {
        Image = transform.Find("ProfileImage").GetComponent<Image>();
    }

    void Update()
    {
        Image.sprite = ProfileFrame.GetProfilePicture(TransientData.Instance.UserProfile.ActiveItems);
    }
}
