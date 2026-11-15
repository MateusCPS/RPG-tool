using System.IO;
using UnityEngine;

public static class ImageLoader
{
    public static Texture2D LoadFromFile(string path) => LoadFromFile(path, out _);

    public static Texture2D LoadFromFile(string path, out byte[] bytes)
    {
        bytes = null;
        if(string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        byte[] fileBytes = File.ReadAllBytes(path);
        var texture = new  Texture2D(2, 2, TextureFormat.RGBA32, true);
        if(!texture.LoadImage(fileBytes))
        {
            Object.Destroy(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.anisoLevel = 4;
        bytes = fileBytes;
        return texture;
    }
}
