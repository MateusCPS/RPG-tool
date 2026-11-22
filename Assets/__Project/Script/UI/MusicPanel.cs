using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicPanel : MonoBehaviour
{
    public static readonly string[] SituationNames = 
        { "Exploração", "Combate", "Boss: entrada", "Boss: luta", "Investigação", "Furtividade" };
    static readonly string[] ModeNames = { "Modo: repetir", "Modo: sequência", "Modo: aleatório" };

    [SerializeField] MusicSync music;
    [SerializeField] MusicPlayer player;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text nowPlayingText;
    [SerializeField] TMP_Text modeLabel;
    [SerializeField] Slider volumeSlider;

    void OnEnable()
    {
        music.Changed += Refresh;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void OnDisable()
    {
        music.Changed -= Refresh;
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    void Start()
    {
        panel.SetActive(false);
        volumeSlider.SetValueWithoutNotify(player.Volume);
        Refresh();
    }

    public void TogglePanel() => panel.SetActive(!panel.activeSelf);

    public void PlaySituation(int situation) => music.PlaySituation((MusicSituation)situation);

    public void CycleMode() => music.SetMode((MusicMode)((music.Current.Mode + 1) % ModeNames.Length));

    public void Next() => music.Next();

    public void Stop() => music.StopMusic();

    void OnVolumeChanged(float value) => player.SetVolume(value);

    void Refresh()
    {
        MusicState state = music.Current;
        bool known = state.Situation >= 0 && state.Situation < SituationNames.Length;
        nowPlayingText.text = state.Playing && known
            ? $"Tocando: {SituationNames[state.Situation]} ({state.Track + 1})"
            : "Sem música";
        modeLabel.text = ModeNames[Mathf.Clamp(state.Mode, 0, ModeNames.Length - 1)];
    }
}
