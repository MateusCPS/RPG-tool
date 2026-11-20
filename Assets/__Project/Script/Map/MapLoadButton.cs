using UnityEngine;

public class MapLoadButton : MonoBehaviour
{
    [SerializeField] MapBoard map;
    [SerializeField] TokenManager tokens;

    public void LoadMap()
    {
        if (!tokens.CanManageTokens) return;

        FilePicker.PickImage((bytes, fileName) =>
        {
            if (bytes == null) return;
            if (!map.LoadFromBytes(bytes, fileName))
                Debug.LogError($"Falha ao carregar a imagem: {fileName}");
        });
    }
}
