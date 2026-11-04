using System;

public static class FilePicker
{
    public static void PickImage(Action<string> onPicked)
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Escolher Imagem", "", new[] { "Imagens", "png,jpg,jpeg" });
        onPicked(string.IsNullOrEmpty(path) ? null : path);
#elif UNITY_STANDALONE_WIN
        onPicked(WindowsFileDialog.OpenFile("Escolher Imagem"));
#else
        onPicked(null);
#endif
    }
}
