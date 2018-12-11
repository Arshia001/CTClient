using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerBar : MonoBehaviour
{
    static float FullHue = 105 / 359.0f;
    static float EmptyHue = 7 / 359.0f;
    static Color InactiveColor = new Color32(74, 92, 68, 255);
    static float GradientStart = 0.6f;
    static float GradientEnd = 0.2f;
    static float MaxSlice = 0.91f;
    static float MinSlice = 0.09f;
    static float LerpFactor = 4;
    static float BlinkStart = 0.3f;
    static float BlinkEnd = 0.15f;
    static float BlinkInterval = 0.4f;
    static float MinAmount = 0.03f;


    float FillAmount;
    bool IsActive;

    Image Image;


    void Start()
    {
        Image = GetComponent<Image>();
    }

    void Update()
    {
        var Amount = Mathf.Clamp01(FillAmount) * (1 - MinAmount) + MinAmount;
        Image.fillAmount = Mathf.Lerp(Image.fillAmount, Mathf.Lerp(MinSlice, MaxSlice, IsActive ? Amount : 1), Time.deltaTime * LerpFactor);
        var Col = Color.Lerp(Image.color, IsActive ? Color.HSVToRGB(Mathf.Lerp(FullHue, EmptyHue, Mathf.Clamp01(Mathf.InverseLerp(GradientStart, GradientEnd, Amount))), 1, 1) : InactiveColor, Time.deltaTime * LerpFactor);
        if (IsActive)
            Col.a = 1 - (Mathf.PingPong(Time.time, BlinkInterval) / BlinkInterval * Mathf.Clamp01(Mathf.InverseLerp(BlinkStart, BlinkEnd, Amount)));
        else
            Col.a = Mathf.Lerp(Col.a, 1, Time.deltaTime * LerpFactor);
        Image.color = Col;
    }

    public void Set(float FillAmount, bool IsActive)
    {
        this.FillAmount = FillAmount;
        this.IsActive = IsActive;
    }
}
