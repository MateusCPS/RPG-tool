using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionPanel : MonoBehaviour
{
    [SerializeField] SessionService session;
    [SerializeField] GameObject window;
    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_InputField codeField;
    [SerializeField] Button createButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button leaveButton;
    [SerializeField] Button cancelButton;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text roomCodeText;
    [SerializeField] TMP_Text versionText;

    void OnEnable()
    {
        session.Changed += Refresh;
        codeField.onSubmit.AddListener(OnCodeSubmit);
    }

    void OnDisable()
    {
        session.Changed -= Refresh;
        codeField.onSubmit.RemoveListener(OnCodeSubmit);
    }

    void Start()
    {
        versionText.text = $"v{AppVersion.Current}";
        Refresh();
    }

    void OnCodeSubmit(string _) => JoinRoom();

    public void CreateRoom()
    {
        session.Create(nameField.text);
    }

    public void JoinRoom()
    {
        session.Join(nameField.text, codeField.text);
    }

    public void LeaveRoom()
    {
        session.Leave();
    }

    public void CancelReconnect()
    {
        session.CancelReconnect();
    }

    void Refresh()
    {
        bool inSession = session.State == SessionService.SessionState.InSession;
        window.SetActive(!inSession);
        leaveButton.gameObject.SetActive(inSession);

        bool showCode = inSession && session.IsHost;
        roomCodeText.gameObject.SetActive(showCode);
        if (showCode) roomCodeText.text = $"Sala: {session.Code}";

        bool ready = session.State == SessionService.SessionState.Ready;
        bool reconnecting = session.State == SessionService.SessionState.Reconnecting;
        createButton.interactable = ready;
        joinButton.interactable = ready;
        createButton.gameObject.SetActive(!reconnecting);
        joinButton.gameObject.SetActive(!reconnecting);
        cancelButton.gameObject.SetActive(reconnecting);

        statusText.text = session.State switch
        {
            SessionService.SessionState.Starting => "Conectando...",
            SessionService.SessionState.Busy => "Aguarde...",
            SessionService.SessionState.Reconnecting => $"Reconectando... (tentativa {session.ReconnectAttempt} de {session.ReconnectAttempts})",
            _ => session.Error
        };
    }
}
