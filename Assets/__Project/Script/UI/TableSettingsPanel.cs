using TMPro;
using UnityEngine;

// Painel de configurações da mesa: modo de movimento, grade visível e calibração do mapa.
// Como o painel de vida, o script fica num objeto sempre ligado e só liga/desliga o "panel".
public class TableSettingsPanel : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TableGrid grid;
    [SerializeField] MapBoard map;
    [SerializeField] GameObject panel;

    [Header("Textos")]
    [SerializeField] TMP_Text modeLabel;   // texto do botão de modo
    [SerializeField] TMP_Text gridLabel;   // texto do botão de grade
    [SerializeField] TMP_Text pixelsLabel; // "Pixels por quadrado: 70"
    [SerializeField] TMP_Text offsetLabel; // "Deslocamento: 0, 0"

    void Start()
    {
        panel.SetActive(false);
        Refresh();
    }

    // Botão "Configurações": abre e fecha o painel.
    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        Refresh();
    }

    // Botão de modo: alterna entre Grade e Livre.
    public void ToggleMode()
    {
        var next = tokens.Mode == TokenManager.MovementMode.Grip
            ? TokenManager.MovementMode.Free
            : TokenManager.MovementMode.Grip;
        tokens.SetMode(next);
        Refresh();
    }

    // Botão de grade: mostra ou esconde.
    public void ToggleGrid()
    {
        grid.SetVisible(!grid.Visible);
        Refresh();
    }

    // Botões de calibração. O valor vem do próprio botão, no On Click (ex.: -1, +1, -10, +10).
    public void ChangePixels(float delta)
    {
        map.SetCalibration(Mathf.Max(1f, map.PixelsPerCell + delta), map.OffsetPixels);
        AfterCalibration();
    }

    public void ChangeOffsetX(float delta)
    {
        map.SetCalibration(map.PixelsPerCell, map.OffsetPixels + new Vector2(delta, 0f));
        AfterCalibration();
    }

    public void ChangeOffsetY(float delta)
    {
        map.SetCalibration(map.PixelsPerCell, map.OffsetPixels + new Vector2(0f, delta));
        AfterCalibration();
    }

    // A grade mudou de lugar ou de tamanho: os tokens precisam reencaixar nela.
    void AfterCalibration()
    {
        tokens.SetMode(tokens.Mode);
        Refresh();
    }

    // Atualiza os textos com o estado atual.
    void Refresh()
    {
        modeLabel.text = tokens.Mode == TokenManager.MovementMode.Grip ? "Modo: Grade" : "Modo: Livre";
        gridLabel.text = grid.Visible ? "Esconder grade" : "Mostrar grade";
        pixelsLabel.text = $"Pixels por quadrado: {map.PixelsPerCell:0.#}";
        offsetLabel.text = $"Deslocamento: {map.OffsetPixels.x:0}, {map.OffsetPixels.y:0}";
    }
}
