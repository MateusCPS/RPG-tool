using UnityEngine;

public class MapLoadButton : MonoBehaviour
{
    [SerializeField] MapBoard map;
    [SerializeField] TokenManager tokens;

    public void LoadMap()
    {
        if (!tokens.CanManageTokens) return;
        
        FilePicker.PickImage(path =>
        {
            if (path == null) return;
            if(!map.LoadFromFile(path))
            {
                Debug.LogError("Falha ao carregar a imagem: {path}");
            }
        });
    }
}
