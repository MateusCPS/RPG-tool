using UnityEngine;
using TMPro;


public class TokenInfoPanel : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_InputField amountField;

    void Awake()
    {
        amountField.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    void OnEnable()
    {
        tokens.SelectionChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        tokens.SelectionChanged -= Refresh;
    }

    void Refresh()
    {
        TokenView token = tokens.Selected;
        panel.SetActive(token != null);
        if (token == null) return;

        TokenData data = token.Data;
        nameText.text = data.displayName;
        hpText.text = $"Vida: {data.hp} / {data.maxHp}";
    }

    public void ApplyDamage() => ChangeHp(-1);
    public void ApplyHealing() => ChangeHp(+1);

    void ChangeHp(int direction)
    {
        TokenView token = tokens.Selected;
        if(token == null) return;
        if(!int.TryParse(amountField.text, out int amount) || amount <= 0) return;

        TokenData data = token.Data;
        data.hp = Mathf.Clamp(data.hp + amount * direction, 0, data.maxHp);

        token.Refresh();
        Refresh();
        amountField.text = "";
    }

}
