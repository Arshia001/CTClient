using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class NameBox : MonoBehaviour
{
    TMP_InputField Text;
    string Name;

    void Start()
    {
        Text = GetComponentInChildren<TMP_InputField>();
        Text.characterLimit = (int)TransientData.Instance.MaximumNameLength;

        Text.onSelect.AddListener(new UnityAction<string>(s =>
        {
            Text.textComponent.color = Color.clear;
            Text.text = "";
        }));
        Text.onSubmit.AddListener(new UnityAction<string>(s =>
        {
            Name = Text.text;
            Text.text = PersianTextShaper.PersianTextShaper.ShapeText(Text.text);
            Text.textComponent.color = Color.black;
        }));
    }

    public async void Accept()
    {
        if (string.IsNullOrEmpty(Name))
        {
            DialogBox.Instance.ShowOneButton("خطا", "اسمتو ننوشتی که!", () => { });
            return;
        }

        if (!await ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetName(Name))
        {
            DialogBox.Instance.ShowOneButton("خطا", "یکی این اسمو انتخاب کرده،\nیه اسم دیگه انتخاب کن.", () => { });
            return;
        }

        TransientData.Instance.UserProfile.IsNameSet = true;
        ProfileFrame.RefreshAll();
        gameObject.SetActive(false);
    }
}
