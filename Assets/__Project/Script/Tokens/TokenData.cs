using UnityEngine;

[System.Serializable]
public class TokenData
{
    public string id = System.Guid.NewGuid().ToString();
    public string displayName = "Token";
    public float x, z;
    public int size = 1;
    public float speed = 9f;
    public string ownerId = "";
    public int hp = 10, maxHp = 10;

    public Vector3 Position
    {
        get => new Vector3(x, 0f, z);
        set
        {
            x = value.x;
            z = value.z;
        }
    }
}
