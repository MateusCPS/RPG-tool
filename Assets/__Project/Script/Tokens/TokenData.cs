using UnityEngine;

// Os dados de um token, separados da aparência.
// É isto que vai pela rede e para o arquivo salvo; o TokenView só mostra.
[System.Serializable]
public class TokenData
{
    public string id = System.Guid.NewGuid().ToString(); // identificador único
    public string displayName = "Token";
    public float x, z;          // posição do centro, em metros
    public int size = 1;        // em quadrados: 1 = 1x1, 2 = 2x2...
    public float speed = 9f;    // deslocamento, em metros
    public string ownerId = ""; // jogador dono (vazio = só o mestre)
    public int hp = 10, maxHp = 10;

    // Atalho para ler e escrever a posição como Vector3 (no chão, Y = 0).
    public Vector3 Position
    {
        get => new Vector3(x, 0f, z);
        set { x = value.x; z = value.z; }
    }
}
