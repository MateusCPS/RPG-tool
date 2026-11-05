using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TokenCreatePanel : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_Dropdown sizeDropDown;
    [SerializeField] TMP_InputField hpField;
    [SerializeField] RawImage preview;
    [SerializeField] TMP_Text imageLabel;

    Texture2D chosenImage;

    void Awake() 
    {
        sizeDropDown.ClearOptions();
        sizeDropDown.AddOptions(new List<string>
        {
            "1x1 (Médio)", "2x2 (Grande)", "3x3 (Enorme)", "4x4 (Imenso)"
        });
        hpField.contentType = TMP_InputField.ContentType.IntegerNumber;    
    }

    public void Open()
    {
        gameObject.SetActive(true);
        nameField.text = "";
        sizeDropDown.value = 0;
        hpField.text = "10";
        SetImage(null);
        nameField.Select();
    }

    public void Close()
    {
        SetImage(null);
        gameObject.SetActive(false);
    }

    public void ChooseImage()
    {
        FilePicker.PickImage(path =>
        {
            if (path == null) return;

            Texture2D texture = ImageLoader.LoadFromFile(path);
            if(texture == null)
            {
                imageLabel.text = "Não foi possível abrir a image";
                return;
            }
            SetImage(texture);
        });
    }

    void SetImage(Texture2D texture)
    {
        if(chosenImage != null && chosenImage != texture) Destroy(chosenImage);

        chosenImage = texture;
        preview.texture = texture;
        preview.enabled = texture != null;
        imageLabel.text = texture != null ? texture.name : "Nenhuma Imagem";
    }

    public void Create()
    {
        string tokenName = string.IsNullOrWhiteSpace(nameField.text) ? "Token" : nameField.text.Trim();
        int hp = int.TryParse(hpField.text, out int value) && value > 0 ? value : 10;
        
        var data = new TokenData
        {
            displayName = tokenName,
            size = sizeDropDown.value + 1, 
            hp = hp,
            maxHp = hp
        };

        tokens.SpawnAtScreenCenter(data, chosenImage, ownsTexture: chosenImage != null);
        chosenImage = null;
        gameObject.SetActive(false);
    }
}
