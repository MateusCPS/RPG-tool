using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;


public class TokenInfoPanel : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] PlayerRoster roster;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text ownerText;
    [SerializeField] TMP_Dropdown ownerDropdown;
    [SerializeField] TMP_InputField amountField;
    [SerializeField] GameObject[] hpControls;

    readonly List<string> ownerOptions = new List<string>();
    readonly List<string> optionLabels = new List<string>();

    void Awake()
    {
        amountField.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    void OnEnable()
    {
        tokens.SelectionChanged += Refresh;
        tokens.TokenChanged += OnTokenChanged;
        tokens.PermissionsChanged += Refresh;
        roster.Changed += Refresh;
        ownerDropdown.onValueChanged.AddListener(OnOwnerChose);
        Refresh();
    }

    void OnDisable()
    {
        tokens.SelectionChanged -= Refresh;
        tokens.TokenChanged -= OnTokenChanged;
        tokens.PermissionsChanged -= Refresh;
        roster.Changed -= Refresh;
        ownerDropdown.onValueChanged.RemoveListener(OnOwnerChose);
    }

    void OnTokenChanged(TokenView token)
    {
        if (token == tokens.Selected) Refresh();
    }

    void Refresh()
    {
        TokenView token = tokens.Selected;
        panel.SetActive(token != null);
        if (token == null) return;

        TokenData data = token.Data;
        nameText.text = data.displayName;
        hpText.text = $"Vida: {data.hp} / {data.maxHp}";
        bool canEditHp = tokens.CanEditHpLocally(data);
        foreach(GameObject control in hpControls)
            control.SetActive(canEditHp);

        string ownerName = roster.NameOf(data.ownerId);
        ownerText.text = string.IsNullOrEmpty(data.ownerId) ? "Sem dono"
            : ownerName.Length > 0 ? $"Dono: {ownerName}" : "Dono: fora da mesa";

        bool canAssign = tokens.CanManageTokens && roster.IsSpawned;
        ownerDropdown.gameObject.SetActive(canAssign);
        if (canAssign) RefreshOwnerOption(data.ownerId);
    }

    void RefreshOwnerOption(string currentOwner)
    {
        ownerOptions.Clear();
        optionLabels.Clear();
        ownerOptions.Add("");
        optionLabels.Add("Sem dono");

        int selectedIndex = 0;
        for(int i = 0; i < roster.Count; i++)
        {
            PlayerEntry entry = roster[i];
            if (entry.ClientId == NetworkManager.ServerClientId) continue;

            string id = entry.PlayerId.ToString();
            if (id == currentOwner) selectedIndex = ownerOptions.Count;
            ownerOptions.Add(id);
            optionLabels.Add(entry.Name.ToString());
        }

        ownerDropdown.ClearOptions();
        ownerDropdown.AddOptions(optionLabels);
        ownerDropdown.SetValueWithoutNotify(selectedIndex);
    }

    void OnOwnerChose(int index)
    {
        if (index < 0 || index >= ownerOptions.Count) return;
        tokens.SetOwner(tokens.Selected, ownerOptions[index]);
    }

    public void ApplyDamage() => ChangeHp(-1);
    public void ApplyHealing() => ChangeHp(+1);

    void ChangeHp(int direction)
    {
        TokenView token = tokens.Selected;
        if (token == null) return;
        if (!int.TryParse(amountField.text, out int amount) || amount <= 0) return;

        tokens.ChangeHp(token, amount * direction);
        amountField.text = "";
    }
}
