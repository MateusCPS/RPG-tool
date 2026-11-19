using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Lobbies;
using UnityEngine;

public class SessionService : MonoBehaviour
{
    public enum SessionState {Starting, Ready, Busy, InSession, Failed}

    [SerializeField] NetworkManager network;
    [SerializeField] int maxPlayers = 7;
    [SerializeField] int maxNameLength = 20;

    ISession session;

    public SessionState State { get; private set; } = SessionState.Starting;
    public string Code => session?.Code;
    public bool IsHost => session != null && session.IsHost;
    public string PlayerName { get; private set; } = "";
    public string Error { get; private set; } = "";
    public string PlayerId => AuthenticationService.Instance.PlayerId;

    public event Action Changed;

    void OnEnable()
    {
        network.OnClientStopped += OnClientStopped;
    }

    void OnDisable()
    {
        network.OnClientStopped -= OnClientStopped;
    }

    async void Start() 
    {
        try
        {
            var options = new InitializationOptions();
            options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 8));
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            SetState(SessionState.Ready);
        }    
        catch (Exception e)
        {
            Debug.LogException(e);
            Error = "Não foi possível conectar aos serviços. Confira a internet e abra o jogo novamente.";
            SetState(SessionState.Failed);
        }
    }

    public async void Create(string playerName)
    {
        if (!BeginRequest(playerName)) return;

        try
        {
            var options = new SessionOptions { MaxPlayers = maxPlayers}
                .WithRelayNetwork()
                .WithNetworkOptions(new NetworkOptions{ RelayProtocol = RelayProtocol.WSS });
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            SetState(SessionState.InSession);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EndWithError("Não foi possível criar a sala. Tente de novo.");
        }
    }

    public async void Join(string playerName, string code)
    {
        string cleanCode = code.Trim().ToUpperInvariant();
        if (cleanCode.Length == 0)
        {
            Error = "Digite o código da sala.";
            Changed?.Invoke();
            return;
        }

        if (!BeginRequest(playerName)) return;

        try
        {
            var options = new JoinSessionOptions()
                .WithNetworkOptions(new NetworkOptions{ RelayProtocol = RelayProtocol.WSS});
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(cleanCode, options);
            SetState(SessionState.InSession);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EndWithError(JoinErrorMessage(e));
        }
    }

    public async void Leave()
    {
        if (State != SessionState.InSession) return;

        SetState(SessionState.Busy);
        try
        {
            await session.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        session = null;
        Error = "";
        SetState(SessionState.Ready);
    }

    public async void RemovePlayer(string playerId)
    {
        if (session == null || !session.IsHost || string.IsNullOrEmpty(playerId)) return;

        bool stillMember = false;
        foreach (var player in session.Players)
            if (player.Id == playerId) stillMember = true;
        if (!stillMember) return;

        try
        {
            await session.AsHost().RemovePlayerAsync(playerId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Não foi possível remover o jogador da sala: {e.Message}");
        }
    }

    bool BeginRequest(string playerName)
    {
        if (State != SessionState.Ready) return false;

        string cleanName = playerName.Trim();
        if(cleanName.Length == 0)
        {
            Error = "Digita seu nome.";
            Changed?.Invoke();
            return false;
        }

        PlayerName = cleanName.Length > maxNameLength ? cleanName.Substring(0, maxNameLength) : cleanName;
        SetState(SessionState.Busy);
        return true;
    }

    void EndWithError(string message)
    {
        session = null;
        Error = message;
        SetState(SessionState.Ready);
    }
    void OnClientStopped(bool wasHost)
    {
        if (State != SessionState.InSession) return;
        LeaveAfterDrop(wasHost ? "A sala caiu. Crie uma nova sala." : "A sala foi fechada ou a conexão caiu.");
    }

    async void LeaveAfterDrop(string message)
    {
        ISession dropped = session;
        session = null;
        SetState(SessionState.Busy);

        if (dropped != null)
        {
            try
            {
                await dropped.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Não foi possível sair da sala anterior: {e.Message}");
            }
        }

        Error = message;
        SetState(SessionState.Ready);
    }

    void SetState(SessionState newState)
    {
        State = newState;
        Changed?.Invoke();
    }

    static string JoinErrorMessage(Exception e)
    {
        const string notFound = "Sala não encontrada. Confira o código com o mestre.";

        for (Exception current = e; current != null; current = current.InnerException)
        {
            if (current is LobbyServiceException lobby)
            {
                if (lobby.Reason == LobbyExceptionReason.LobbyFull) return "A sala está cheia.";
                if (lobby.Reason == LobbyExceptionReason.LobbyNotFound || lobby.Reason == LobbyExceptionReason.InvalidJoinCode) return notFound; 
            }

            if (current is SessionException sessionError && sessionError.Error == SessionError.SessionNotFound) return notFound;
        }
        return "Não foi possível entrar. Confira a internet e tente de novo.";
    }
}
