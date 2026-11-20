using System.Text;
using TMPro;
using UnityEngine;

public class PlayersPanel : MonoBehaviour
{
    [SerializeField] SessionService session;
    [SerializeField] PlayerRoster roster;
    [SerializeField] GameObject listPanel;
    [SerializeField] TMP_Text listText;
    [SerializeField] TMP_Text noticeText;
    [SerializeField] float noticeDuration = 3f;

    readonly StringBuilder builder = new StringBuilder();
    float noticeHideTime;

    void OnEnable()
    {
        session.Changed += Refresh;
        roster.Changed += Refresh;
        roster.PlayerJoined += OnPlayerJoined;
        roster.PlayerLeft += OnPlayerLeft;
        roster.PlayerReturned += OnPlayerReturned;
    }

    void OnDisable()
    {
        session.Changed -= Refresh;
        roster.Changed -= Refresh;
        roster.PlayerJoined -= OnPlayerJoined;
        roster.PlayerLeft -= OnPlayerLeft;
        roster.PlayerReturned -= OnPlayerReturned;
    }

    void Start()
    {
        noticeText.gameObject.SetActive(false);
        Refresh();
    }

    void Update()
    {
        if (noticeText.gameObject.activeSelf && Time.time >= noticeHideTime)
            noticeText.gameObject.SetActive(false);
    }

    void Refresh()
    {
        bool visible = session.State == SessionService.SessionState.InSession && roster.IsSpawned;
        listPanel.SetActive(visible);
        if (!visible) return;

        builder.Clear();
        builder.Append("Na mesa (").Append(roster.Count).Append(')');
        for (int i = 0; i < roster.Count; i++)
            builder.Append('\n').Append(roster[i].Name.ToString());
        listText.SetText(builder);
    }

    void OnPlayerJoined(string playerName) => ShowNotice($"{playerName} entrou na mesa");

    void OnPlayerLeft(string playerName) => ShowNotice($"{playerName} saiu da mesa");

    void OnPlayerReturned(string playerName) => ShowNotice($"{playerName} voltou para a mesa");

    void ShowNotice(string message)
    {
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);
        noticeHideTime = Time.time + noticeDuration;
    }
}
