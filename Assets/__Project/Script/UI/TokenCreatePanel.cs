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
    [SerializeField] int maxImageSize = 512;

    Texture2D chosenImage;
    byte[] chosenBytes;

    void Awake() 
    {
        sizeDropDown.ClearOptions();
        sizeDropDown.AddOptions(new List<string>
        {
            "1x1 (Médio)", "2x2 (Grande)", "3x3 (Enorme)", "4x4 (Imenso)"
        });
        hpField.contentType = TMP_InputField.ContentType.IntegerNumber;    
    }

    void OnEnable() => tokens.PermissionsChanged += OnPermissionsChanged;

    void OnDisable() => tokens.PermissionsChanged -= OnPermissionsChanged;

    void OnPermissionsChanged()
    {
        if (!tokens.CanManageTokens) Close();
    }

    public void Open()
    {
        if (!tokens.CanManageTokens) return;

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
        FilePicker.PickImage((fileBytes, fileName) =>
        {
            if (fileBytes == null) return;

            Texture2D texture = ImageLoader.LoadFromBytes(fileBytes, maxImageSize, true, out byte[] bytes, out _);
            if (texture == null)
            {
                imageLabel.text = "Não foi possível abrir a imagem";
                return;
            }

            texture.name = fileName;
            SetImage(texture, bytes);
        });
    }

    void SetImage(Texture2D texture, byte[] bytes = null)
    {
        if(chosenImage != null && chosenImage != texture) Destroy(chosenImage);

        chosenImage = texture;
        chosenBytes = bytes;
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

        tokens.SpawnAtScreenCenter(data, chosenImage, chosenImage != null, chosenBytes);
        chosenImage = null;
        chosenBytes = null;
        gameObject.SetActive(false);
    }
}
