using System;
using System.Text;
using TMPro;
using UnityEngine;

public class ProblemReport : MonoBehaviour
{
    [SerializeField] LogRecorder log;
    [SerializeField] SessionService session;
    [SerializeField] PlayerRoster roster;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] float feedbackDuration = 2f;

    readonly StringBuilder builder = new StringBuilder();
    float feedbackHideTime;

    void Start()
    {
        feedbackText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (feedbackText.gameObject.activeSelf && Time.time >= feedbackHideTime)
            feedbackText.gameObject.SetActive(false);
    }

    public void Copy()
    {
        builder.Clear();
        builder.Append(Application.productName).Append(' ').Append(AppVersion.Current)
            .Append(" | ").Append(Application.platform)
            .Append(" | ").Append(SystemInfo.operatingSystem).Append('\n');

        string browser = BrowserBridge.UserAgent();
        if (browser.Length > 0) builder.Append("Navegador: ").Append(browser).Append('\n');

        builder.Append("Estado: ").Append(session.State);
        if (session.State == SessionService.SessionState.InSession)
            builder.Append(session.IsHost ? " (mestre)" : " (jogador)");
        builder.Append(" | Jogadores: ").Append(roster.IsSpawned ? roster.Count : 0).Append('\n');
        builder.Append("Hora: ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append('\n');
        builder.Append("--- log ---\n");
        log.AppendTo(builder);

        BrowserBridge.CopyToClipboard(builder.ToString());

        feedbackText.text = "Relatório copiado. Cole no Discord para o mestre.";
        feedbackText.gameObject.SetActive(true);
        feedbackHideTime = Time.time + feedbackDuration;
    }
}
