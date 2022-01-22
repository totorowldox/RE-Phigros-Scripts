using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Utils
{
    public static void MakeToast(string info)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
            Toast.CallStatic<AndroidJavaObject>("makeText", currentActivity, info, Toast.GetStatic<int>("LENGTH_LONG")).Call("show");
        }));

        /*
        // 匿名方法中第二个参数是安卓上下文对象，除了用currentActivity，还可用安卓中的GetApplicationContext()获得上下文。
        AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        */
    }
    public static IEnumerator SwitchSceneAfterSeconds(float sec, string scene)
    {
        AsyncOperation a = SceneManager.LoadSceneAsync(scene);
        a.allowSceneActivation = false;
        yield return new WaitForSeconds(sec);
        a.allowSceneActivation = true;
    }
}