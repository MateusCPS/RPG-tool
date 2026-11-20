using System.IO;
using UnityEngine;

public static class ImageLoader
{
    const int JpgQuality = 85;

    public static Texture2D LoadFromFile(string path, int maxSize, bool keepAlpha, out byte[] bytes)
    {
        bytes = null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        Texture2D texture = LoadFromBytes(File.ReadAllBytes(path), maxSize, keepAlpha, out bytes, out _);
        if (texture != null) texture.name = Path.GetFileNameWithoutExtension(path);
        return texture;
    }

    public static Texture2D LoadFromBytes(byte[] original, int maxSize, bool keepAlpha, out byte[] bytes, out Vector2Int originalSize)
    {
        bytes = null;
        originalSize = Vector2Int.zero;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(original))
        {
            Object.Destroy(texture);
            return null;
        }

        originalSize = new Vector2Int(texture.width, texture.height);
        int longest = Mathf.Max(texture.width, texture.height);

        if(longest > maxSize)
        {
            float scale = (float)maxSize / longest;
            int width = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));

            Texture2D resized = Resize(texture, width, height);
            Object.Destroy(texture);
            texture = resized;
            bytes = Encode(texture, keepAlpha);
        }
        else
        {
            byte[] encoded = Encode(texture, keepAlpha);
            bytes = encoded.Length < original.Length ? encoded : original;
        }

        texture.anisoLevel = 4;
        return texture;
    }

    static byte[] Encode(Texture2D texture, bool keepAlpha) => 
        keepAlpha ? texture.EncodeToPNG() : texture.EncodeToJPG(JpgQuality);
    
    static Texture2D Resize(Texture source, int width, int height)
    {
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, target);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var result = new Texture2D(width, height, TextureFormat.RGBA32, true);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = previous;

        RenderTexture.ReleaseTemporary(target);
        return result;
    }
}
