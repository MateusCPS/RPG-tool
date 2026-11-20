using TMPro;
using UnityEngine;

public class ImageDownloadLabel : MonoBehaviour
{
    [SerializeField] ImageService images;
    [SerializeField] TMP_Text label;

    void OnEnable()
    {
        images.DownloadProgress += Show;
        label.gameObject.SetActive(false);        
    }

    void OnDisable()
    {
        images.DownloadProgress -= Show;
    }

    void Show(float progress)
    {
        bool done = progress >= 1f;
        label.gameObject.SetActive(!done);
        if (!done) label.SetText("Recebendo imagem: {0:0}%", progress * 100f);
    }
}
