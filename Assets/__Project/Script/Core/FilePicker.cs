using System;
using System.IO;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif

public static class FilePicker
{
    public static void PickImage(Action<byte[], string> onPicked)
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Escolher imagem", "", new[] { "Imagens", "png,jpg,jpeg" });
        Deliver(path, onPicked);
#elif UNITY_STANDALONE_WIN
        Deliver(WindowsFileDialog.OpenFile("Escolher imagem"), onPicked);
#elif UNITY_WEBGL
        pending = onPicked;
        FilePicker_Open("image/png,image/jpeg", OnBrowserPicked);
#else
        onPicked(null, null);
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    static void Deliver(string path, Action<byte[], string> onPicked)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            onPicked(null, null);
            return;
        }

        onPicked(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path));
    }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    delegate void PickedCallback(IntPtr data, int length, IntPtr name);

    static Action<byte[], string> pending;

    [DllImport("__Internal")]
    static extern void FilePicker_Open(string accept, PickedCallback callback);

    [MonoPInvokeCallback(typeof(PickedCallback))]
    static void OnBrowserPicked(IntPtr data, int length, IntPtr name)
    {
        Action<byte[], string> callback = pending;
        pending = null;
        if (callback == null) return;

        if (data == IntPtr.Zero)
        {
            DeliverNextFrame(callback, null, null);
            return;
        }

        byte[] bytes = new byte[length];
        Marshal.Copy(data, bytes, 0, length);
        string fileName = Path.GetFileNameWithoutExtension(Marshal.PtrToStringUTF8(name));
        DeliverNextFrame(callback, bytes, fileName);
    }

    static async void DeliverNextFrame(Action<byte[], string> callback, byte[] bytes, string fileName)
    {
        await Awaitable.NextFrameAsync();
        callback(bytes, fileName);
    }
#endif
}
