using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SpikeSession : MonoBehaviour
{
    [SerializeField] NetworkObject tokenPrefab;
    [SerializeField] int maxPlayers = 7;

    ISession session;
    string joinCode = "";
    string status = "Iniciando serviços...";
    bool ready;
    bool busy;

    async void Start()
    {
        try
        {
            var options = new InitializationOptions();
            options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 8));
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            ready = true;
            status = "Pronto";
        }
        catch (Exception e)
        {
            status = "Erro ao iniciar: " + e.Message;
            Debug.LogException(e);
        }
    }

    async void CreateSession()
    {
        busy = true;
        status = "Criando sala...";
        try
        {
            NetworkManager.Singleton.OnServerStarted += SpawnToken;
            var options = new SessionOptions { MaxPlayers = maxPlayers }
                .WithRelayNetwork()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            status = "Sala criada. Código: " + session.Code;
        }
        catch (Exception e)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnToken;
            status = "Erro ao criar: " + e.Message;
            Debug.LogException(e);
        }
        busy = false;
    }

    async void JoinSession()
    {
        busy = true;
        status = "Entrando...";
        try
        {
            var options = new JoinSessionOptions()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim().ToUpperInvariant(), options);
            status = "Conectado à sala " + session.Code;
        }
        catch (Exception e)
        {
            status = "Erro ao entrar: " + e.Message;
            Debug.LogException(e);
        }
        busy = false;
    }

    void SpawnToken()
    {
        NetworkManager.Singleton.OnServerStarted -= SpawnToken;
        NetworkObject token = Instantiate(tokenPrefab);
        token.Spawn();
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        GUI.skin.button.fontSize = 22;
        GUI.skin.textField.fontSize = 22;

        GUILayout.BeginArea(new Rect(20, 20, 420, 300));
        GUILayout.Label(status);

        if (session != null)
        {
            string role = session.IsHost ? "Mestre (host)" : "Jogador (cliente)";
            GUILayout.Label($"{role} · {session.PlayerCount} na sala · rede: {session.Network?.State}");
            if (session.IsHost) GUILayout.TextField(session.Code);
        }
        else
        {
            GUI.enabled = ready && !busy;
            if (GUILayout.Button("Criar sala", GUILayout.Height(44))) CreateSession();
            GUILayout.Space(10);
            GUILayout.Label("Código da sala:");
            joinCode = GUILayout.TextField(joinCode, GUILayout.Height(40));
            if (GUILayout.Button("Entrar", GUILayout.Height(44))) JoinSession();
            GUI.enabled = true;
        }

        GUILayout.EndArea();
    }
}
