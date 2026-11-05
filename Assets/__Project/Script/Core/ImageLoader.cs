using System.IO;
using UnityEngine;

public static class ImageLoader
{
    public static Texture2D LoadFromFile(string path)
    {
        if(string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        var texture = new  Texture2D(2, 2, TextureFormat.RGBA32, true);
        if(!texture.LoadImage(File.ReadAllBytes(path)))
        {
            Object.Destroy(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.anisoLevel = 4;
        return texture;
    }
}
