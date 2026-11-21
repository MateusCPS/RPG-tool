using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class BrowserBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void BrowserBridge_Copy(string text);

    [DllImport("__Internal")]
    static extern string BrowserBridge_UserAgent();
#endif

    public static void CopyToClipboard(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BrowserBridge_Copy(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }

    public static string UserAgent()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return BrowserBridge_UserAgent();
#else
        return "";
#endif
    }
}
