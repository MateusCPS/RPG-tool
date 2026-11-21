using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatBar : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text toggleLabel;
    [SerializeField] Button newRoundButton;
    [SerializeField] Button undoButton;

    void OnEnable()
    {
        tokens.CombatChanged += Refresh;
        Refresh();
    }

    void OnDisable() => tokens.CombatChanged -= Refresh;

    public void ToggleCombat() => tokens.SetCombat(!tokens.CombatActive);

    public void NewRound() => tokens.NewRound();

    public void UndoMove() => tokens.UndoLastMove();

    void Refresh()
    {
        statusText.gameObject.SetActive(tokens.CombatActive);
        statusText.SetText("Combate · Rodada {0}", tokens.Round);
        toggleLabel.text = tokens.CombatActive ? "Encerrar combate" : "Iniciar combate";
        newRoundButton.interactable = tokens.CombatActive;
        undoButton.gameObject.SetActive(tokens.CombatActive);
    }
}
