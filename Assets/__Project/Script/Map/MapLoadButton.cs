using UnityEngine;

public class MapLoadButton : MonoBehaviour
{
    [SerializeField] MapBoard map;

    public void LoadMap()
    {
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
