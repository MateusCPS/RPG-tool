using System.Collections.Generic;
using Unity.Netcode;
using System;
using Unity.Collections;
using Unity.Services.Lobbies.Models;


public class PlayerRoster : NetworkBehaviour
{
    [UnityEngine.SerializeField] SessionService session;
    [UnityEngine.SerializeField] int maxNameLength = 20;
    
    NetworkList<PlayerEntry> players;
    readonly Dictionary<ulong, string> playerIds = new Dictionary<ulong, string>();

    public event Action Changed;
    public event Action<string> PlayerJoined;
    public event Action<string> PlayerLeft;
    public event Action<string> PlayerReturned;

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
        if (ReplacePrevious(clientId, playerId)) return;

        playerIds[clientId] = playerId;
        var entry = new PlayerEntry { ClientId = clientId };
        entry.Name.CopyFromTruncated(UniqueName(CleanName(playerName)));
        entry.PlayerId.CopyFromTruncated(playerId);
        players.Add(entry);
    }

    bool ReplacePrevious(ulong clientId, string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return false;

        ulong previousClient = 0;
        bool found = false;
        foreach (var pair in playerIds)
        {
            if (pair.Value != playerId) continue;
            previousClient = pair.Key;
            found = true;
            break;
        }
        if (!found) return false;

        int index = IndexOf(previousClient);
        if (index < 0) return false;

        playerIds.Remove(previousClient);
        playerIds[clientId] = playerId;

        PlayerEntry entry = players[index];
        entry.ClientId = clientId;
        players[index] = entry;

        if (NetworkManager.ConnectedClients.ContainsKey(previousClient))
            NetworkManager.DisconnectClient(previousClient);
        return true;
    }

    public string NameOf(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return "";
        for (int i = 0; i < players.Count; i++)
            if(players[i].PlayerId.ToString() == playerId) return players[i].Name.ToString();
        return "";
    }

    public bool TryGetPlayerId(ulong clientId, out string playerId) => 
        playerIds.TryGetValue(clientId, out playerId);

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
        else if (change.Type == NetworkListEvent<PlayerEntry>.EventType.Value)
            PlayerReturned?.Invoke(change.Value.Name.ToString());

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
