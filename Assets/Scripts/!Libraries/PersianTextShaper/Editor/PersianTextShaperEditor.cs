using UnityEditor;
using UnityEngine;

public class PersianTextShaperHelperWindow : EditorWindow
{
    string RawText;
    string ShapedText;

    [MenuItem("Window/Persian Text Shaper")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PersianTextShaperHelperWindow));
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(RawText))
        {
            ShapedText = "";
        }
        else
        {
            bool Rtl = true;
            foreach (var ch in RawText)
            {
                switch (PersianTextShaper.PersianTextShaper.GetCharType(ch))
                {
                    case PersianTextShaper.PersianTextShaper.CharType.LTR:
                        Rtl = false;
                        break;
                    case PersianTextShaper.PersianTextShaper.CharType.RTL:
                        break;
                    default:
                        continue;
                }
                break;
            }
            ShapedText = PersianTextShaper.PersianTextShaper.ShapeText(RawText, Rtl);
        }

        GUILayout.Label("Input (Not Fixed)", EditorStyles.boldLabel);
        RawText = EditorGUILayout.TextArea(RawText);

        GUILayout.Label("Output (Fixed)", EditorStyles.boldLabel);
        ShapedText = EditorGUILayout.TextArea(ShapedText);

        if (GUILayout.Button("Copy"))
        {
            var T = new TextEditor();
            T.text = ShapedText;
            T.SelectAll();
            T.Copy();
        }
    }
}