using System.Collections.Generic;
using Unity.Netcode;
using System;
using Unity.Collections;


public class PlayerRoster : NetworkBehaviour
{
    [UnityEngine.SerializeField] SessionService session;
    [UnityEngine.SerializeField] int maxNameLength = 20;
    
    NetworkList<PlayerEntry> players;
    readonly Dictionary<ulong, string> playerIds = new Dictionary<ulong, string>();

    public event Action Changed;
    public event Action<string> PlayerJoined;
    public event Action<string> PlayerLeft;

    public int Count => players.Count;
    public PlayerEntry this[int index] => players[index];

    void Awake()
    {
        players = new NetworkList<PlayerEntry>();
    }

    public override void OnNetworkSpawn()
    {
        players.OnListChanged += OnListChanged;
        if (IsServer)
        {
            players.Clear();
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        RegisterRpc(session.PlayerName, session.PlayerId);
        Changed?.Invoke();
    }
    public override void OnNetworkDespawn()
    {
        players.OnListChanged -= OnListChanged;
        if (IsServer) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        playerIds.Clear();
        Changed?.Invoke();
    }

    [Rpc(SendTo.Server)]
    void RegisterRpc(string playerName, string playerId, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (IndexOf(clientId) >= 0) return;

        playerIds[clientId] = playerId;
        var entry = new PlayerEntry { ClientId = clientId };
        entry.Name.CopyFromTruncated(UniqueName(CleanName(playerName)));
        players.Add(entry);
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return;

        int index = IndexOf(clientId);
        if (index >= 0) players.RemoveAt(index);

        if (playerIds.TryGetValue(clientId, out string playerId))
        {
            playerIds.Remove(clientId);
            session.RemovePlayer(playerId);
        }
    }

    void OnListChanged(NetworkListEvent<PlayerEntry> change)
    {
        if (change.Type == NetworkListEvent<PlayerEntry>.EventType.Add)
            PlayerJoined?.Invoke(change.Value.Name.ToString());
        else if (change.Type == NetworkListEvent<PlayerEntry>.EventType.RemoveAt)
            PlayerLeft?.Invoke(change.Value.Name.ToString());

        Changed?.Invoke();
    }
    int IndexOf(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].ClientId == clientId) return i;

        return -1;
    }

    string CleanName(string playerName)
    {
        string clean = string.IsNullOrWhiteSpace(playerName) ? "Jogador" : playerName.Trim();
        return clean.Length > maxNameLength ? clean.Substring(0, maxNameLength) : clean;
    }

    string UniqueName(string playerName)
    {
        string candidate = playerName;
        int number = 2;
        while (NameTaken(candidate)) candidate = ($"{playerName} ({number++})");
        return candidate;
    }

    bool NameTaken(string playerName)
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].Name.ToString() == playerName) return true;
        return false;
    }
}
