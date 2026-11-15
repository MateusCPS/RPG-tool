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
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text roomCodeText;

    void OnEnable()
    {
        session.Changed += Refresh;
    }

    void OnDisable()
    {
        session.Changed -= Refresh;
    }

    void Start()
    {
        Refresh();
    }

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

    void Refresh()
    {
        bool inSession = session.State == SessionService.SessionState.InSession;
        window.SetActive(!inSession);
        leaveButton.gameObject.SetActive(inSession);

        bool showCode = inSession && session.IsHost;
        roomCodeText.gameObject.SetActive(showCode);
        if (showCode) roomCodeText.text = $"Sala: {session.Code}";

        bool ready = session.State == SessionService.SessionState.Ready;
        createButton.interactable = ready;
        joinButton.interactable = ready;

        statusText.text = session.State switch
        {
            SessionService.SessionState.Starting => "Conectando...",
            SessionService.SessionState.Busy => "Aguarde...",
            _ => session.Error
        };
    }
}
