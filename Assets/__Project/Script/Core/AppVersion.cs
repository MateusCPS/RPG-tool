using System.Text;
using UnityEngine;

public static class AppVersion
{
    public const string RejectPrefix = "[versao] ";

    public static string Current => Application.version;

    public static byte[] Payload() => Encoding.UTF8.GetBytes(Current);

    public static string ReadPayload(byte[] payload, int maxBytes)
    {
        if (payload == null || payload.Length == 0 || payload.Length > maxBytes) return "";
        return Encoding.UTF8.GetString(payload);
    }

    public static string Compatibility(string version)
    {
        if (string.IsNullOrEmpty(version)) return "";
        string[] parts = version.Split('.');
        return parts.Length >= 2 ? parts[0] + "." + parts[1] : version;
    }

    public static bool IsCompatible(string a, string b)
    {
        string left = Compatibility(a);
        return left.Length > 0 && left == Compatibility(b);
    }
}
