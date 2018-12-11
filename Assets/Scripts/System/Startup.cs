using LightMessage.Common.Connection;
using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Startup : MonoBehaviour
{
    async void Start()
    {
        transform.Find("DialogBox").gameObject.SetActive(false);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        var Text = GetComponentInChildren<TextMeshProUGUI>();
        var CM = ConnectionManager.Instance;

        int NumRetriesAfterConnectingToServer = 0;
        while (true)
        {
            if (NumRetriesAfterConnectingToServer >= 3)
            {
                GetComponentInChildren<DialogBox>().ShowOneButton("شرمنده", "ما نتونستیم به سرور بازی وصل شیم.\nممکنه با آپدیت کردن بازی مشکل حل بشه.\nبریم؟", () =>
                {
                    Application.OpenURL("https://cafebazaar.ir/app/com.sperlous.ctclient/?l=fa");
                    Application.Quit();
                }, "بریم!");
                return;
            }

            try
            {
                MainThreadDispatcher.Instance.Enqueue(() => { Text.text = PersianTextShaper.PersianTextShaper.ShapeText("در حال اتصال به سرور..."); });

                await CM.Connect();

                MainThreadDispatcher.Instance.Enqueue(() => { Text.text = PersianTextShaper.PersianTextShaper.ShapeText("در حال دریافت اطلاعات نسخه..."); });

                ++NumRetriesAfterConnectingToServer;

                var Ver = await CM.EndPoint<SystemEndPoint>().GetClientVersion();
                var MyVer = AppVersion.Instance.Version;
                TaskCompletionSource<bool> VersionCheckTCS = null;
                if (MyVer < Ver.EarliestSupported)
                {
                    transform.Find("DialogBox").GetComponent<DialogBox>().ShowOneButton("به‌روزرسانی", "باید بازی رو به‌روزرسانی کنی.\nبریم؟", () =>
                    {
                        Application.OpenURL("https://cafebazaar.ir/app/com.sperlous.ctclient/?l=fa");
                        Application.Quit();
                    }, "بریم!");
                    return;
                }
                else if (MyVer < Ver.Latest)
                {
                    VersionCheckTCS = new TaskCompletionSource<bool>();
                    transform.Find("DialogBox").GetComponent<DialogBox>().ShowTwoButton("به‌روزرسانی", "الان می‌تونی به‌روزرسانی جدید رو بگیری.\nبریم؟", () => VersionCheckTCS.SetResult(true), () => VersionCheckTCS.SetResult(false), "بریم!", "باشه بعدا");
                }

                if (VersionCheckTCS != null)
                    if (await VersionCheckTCS.Task)
                    {
                        Application.OpenURL("https://cafebazaar.ir/app/com.sperlous.ctclient/?l=fa");
                        Application.Quit();
                        return;
                    }

                MainThreadDispatcher.Instance.Enqueue(() => { Text.text = PersianTextShaper.PersianTextShaper.ShapeText("در حال دریافت مشخصات کاربر..."); });

                try
                {
                    if (await CM.EndPoint<SystemEndPoint>().GetStartupInfo())
                        SceneManager.LoadSceneAsync("Game");
                    //{
                    //    var Op = 
                    //    Op.completed += LoadGameSceneComplete;
                    //}
                    else
                        SceneManager.LoadScene("Menu");
                }
                catch (InvocationFailureException)
                {
                    transform.Find("DialogBox").GetComponent<DialogBox>().ShowOneButton("به‌روزرسانی سرور", "سرور بازی داره به‌روزرسانی می‌شه.\nلطفا چند دقیقه دیگه دوباره بازی رو باز کن.", () =>
                    {
                        Application.Quit();
                    });
                    return;
                }

                break;
            }
            catch (Exception Ex)
            {
                CM.Disconnect();
                Debug.LogException(Ex);
                await Task.Delay(100);
                continue;
            }
        }
    }

    //void LoadGameSceneComplete(AsyncOperation obj)
    //{
    //    obj.completed -= LoadGameSceneComplete;
    //    FindObjectOfType<Backgammon.GameManager>().RestoreState();
    //}
}
