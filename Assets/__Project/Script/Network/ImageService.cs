using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class ImageService : NetworkBehaviour
{
    [SerializeField] int chunkSize = 4 * 1024;
    [SerializeField] int bytesPerSecond = 256 * 1024;
    [SerializeField] int maxBurstChunks = 4;


    class Download
    {
        public byte[] Buffer;
        public int Recieved;
        public readonly List<Action<Texture2D>> Callbacks = new List<Action<Texture2D>>();
    }

    readonly Dictionary<string, byte[]> served = new Dictionary<string, byte[]>(); 
    readonly Dictionary<string, Texture2D> recieved = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, Download> downloads = new Dictionary<string, Download>();

    float sendBudged;

    void Update()
    {
        if (!IsServer) return;
        sendBudged = Mathf.Min(sendBudged + bytesPerSecond * Time.unscaledDeltaTime, chunkSize * maxBurstChunks);
    }

    bool CanSendTo(ulong clientId) => IsServer && NetworkManager.ConnectedClients.ContainsKey(clientId);
    public event Action<float> DownloadProgress;

    public string Register(byte[] bytes)
    {
        string hash = Hash128.Compute(bytes).ToString();
        served[hash] = bytes;
        return hash;
    }

    public void Get(string hash, Action<Texture2D> onReady)
    {
        if (recieved.TryGetValue(hash, out Texture2D cached))
        {
            onReady(cached);
            return;
        }

        if (downloads.TryGetValue(hash, out Download pending))
        {
            pending.Callbacks.Add(onReady);
            return;
        }

        var download = new Download();
        download.Callbacks.Add(onReady);
        downloads[hash] = download;
        RequestImageRpc(hash);
    }

    [Rpc(SendTo.Server)]
    void RequestImageRpc(string hash, RpcParams rpcParams = default)
    {
        if (!served.TryGetValue(hash, out byte[] bytes)) return;
        StartCoroutine(SendChunks(hash, bytes, rpcParams.Receive.SenderClientId));
    }

    IEnumerator SendChunks(string hash, byte[] bytes, ulong clientId)
    {
        for(int offset = 0; offset < bytes.Length; offset += chunkSize)
        {
            int lenght = Mathf.Min(chunkSize, bytes.Length - offset);

            while (sendBudged < lenght)
            {
                yield return null;
                if (!CanSendTo(clientId)) yield break;
            }       
            if(!CanSendTo(clientId)) yield break;

            sendBudged -= lenght;
            byte[] chunk = new byte[lenght];
            Buffer.BlockCopy(bytes, offset, chunk, 0, lenght);
            RecieveChunkRpc(hash, offset, bytes.Length, chunk, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void RecieveChunkRpc(string hash, int offset, int totalLength, byte[] chunk, RpcParams rpcParams)
    {
        if (!downloads.TryGetValue(hash, out Download download)) return;

        if (download.Buffer == null) download.Buffer = new byte[totalLength];
        Buffer.BlockCopy(chunk, 0, download.Buffer, offset, chunk.Length);
        download.Recieved += chunk.Length;
        DownloadProgress?.Invoke((float)download.Recieved / totalLength);

        if (download.Recieved < totalLength) return;

        downloads.Remove(hash);
        Texture2D texture = Decode(hash, download.Buffer);
        if (texture != null) recieved[hash] = texture;
        foreach (Action<Texture2D> callback in download.Callbacks) callback(texture);
    }

    static Texture2D Decode(string hash, byte[] bytes)
    {
        if (Hash128.Compute(bytes).ToString() != hash)
        {
            Debug.LogError($"Imagem recebida não confere com o hash {hash}.");
            return null;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }
        
        texture.anisoLevel = 4;
        return texture;
    }

    public override void OnDestroy()
    {
        foreach(Texture2D texture in recieved.Values) Destroy(texture);
        base.OnDestroy();
    }
}
