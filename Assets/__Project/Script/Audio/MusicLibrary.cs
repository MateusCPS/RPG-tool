using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Mesa Virtual/Biblioteca de músicas")]
public class MusicLibrary : ScriptableObject
{
    [SerializeField] AudioClip[] exploration;
    [SerializeField] AudioClip[] combat;
    [SerializeField] AudioClip[] bossIntro;
    [SerializeField] AudioClip[] bossFight;
    [SerializeField] AudioClip[] investigation;
    [SerializeField] AudioClip[] stealth;

    public int Count(MusicSituation situation)
    {
        AudioClip[] tracks = Tracks(situation);
        return tracks != null ? tracks.Length : 0;
    }

    public AudioClip Get(MusicSituation situation, int index)
    {
        AudioClip[] tracks = Tracks(situation);
        return tracks != null && index >= 0 && index < tracks.Length ? tracks[index] : null;
    }

    AudioClip[] Tracks(MusicSituation situation) => situation switch
    {
        MusicSituation.Exploration => exploration,
        MusicSituation.Combat => combat,
        MusicSituation.BossIntro => bossIntro,
        MusicSituation.BossFight => bossFight,
        MusicSituation.Investigation => investigation,
        MusicSituation.Stealth => stealth,
        _ => null
    };
}
