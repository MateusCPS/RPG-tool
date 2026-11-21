using System;
using System.Text;
using UnityEngine;

public class LogRecorder : MonoBehaviour
{
    [SerializeField] int capacity = 200;
    [SerializeField] int stackLines = 3;

    string[] lines;
    int next;
    int count;

    void Awake()
    {
        lines = new string[Mathf.Max(1, capacity)];
    }

    void OnEnable() => Application.logMessageReceived += OnLog;

    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string message, string stackTrace, LogType type)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {Label(type)}: {message}";
        if (type != LogType.Log && type != LogType.Warning && !string.IsNullOrEmpty(stackTrace))
            line += "\n" + FirstLines(stackTrace, stackLines);

        lines[next] = line;
        next = (next + 1) % lines.Length;
        if (count < lines.Length) count++;
    }

    public void AppendTo(StringBuilder builder)
    {
        int start = (next - count + lines.Length) % lines.Length;
        for (int i = 0; i < count; i++)
            builder.Append(lines[(start + i) % lines.Length]).Append('\n');
    }

    static string Label(LogType type) => type switch
    {
        LogType.Error => "Erro",
        LogType.Exception => "Exceção",
        LogType.Assert => "Falha",
        LogType.Warning => "Aviso",
        _ => "Info"
    };

    static string FirstLines(string text, int max)
    {
        int index = 0;
        for (int i = 0; i < max; i++)
        {
            int found = text.IndexOf('\n', index);
            if (found < 0) return text.TrimEnd();
            index = found + 1;
        }
        return text.Substring(0, index).TrimEnd();
    }
}
