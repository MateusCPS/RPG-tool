using System;
using Unity.Netcode;
using UnityEngine;

public class MusicSync : NetworkBehaviour
{
    [SerializeField] MusicPlayer player;
    [SerializeField] MusicLibrary library;
    [SerializeField] NetworkManager network;

    readonly NetworkVariable<MusicState> state = new NetworkVariable<MusicState>();
    MusicState current;

    public event Action Changed;
    public MusicState Current => current;
    public bool CanControl => !network.IsListening || network.IsServer;

    void OnEnable() => player.TrackFinished += OnTrackFinished;
    void OnDisable() => player.TrackFinished -= OnTrackFinished;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            current.StartTime = Now() - player.Position;
            state.Value = current;
            return;
        }

        current = default;
        player.Stop();
        state.OnValueChanged += OnStateChanged;
        Apply(state.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) return;

        state.OnValueChanged -= OnStateChanged;
        current = default;
        player.Stop();
        Changed?.Invoke();
    }

    public void PlaySituation(MusicSituation situation)
    {
        if (!CanControl) return;
        if (current.Playing && current.Situation == (int)situation) return;

        int count = library.Count(situation);
        if (count == 0)
        {
            StopMusic();
            return;
        }

        MusicState next = current;
        next.Situation = (int)situation;
        next.Track = current.Mode == (int)MusicMode.Shuffle ? UnityEngine.Random.Range(0, count) : 0;
        next.PlayId++;
        next.Playing = true;
        next.StartTime = Now();
        Publish(next);
    }

    public void SetMode(MusicMode mode)
    {
        if (!CanControl) return;

        MusicState next = current;
        next.Mode = (int)mode;
        next.StartTime = Now() - player.Position;
        Publish(next);
    }

    public void Next()
    {
        if (!CanControl || !current.Playing) return;
        MusicMode mode = (MusicMode)current.Mode;
        Advance(mode == MusicMode.Loop ? MusicMode.Sequence : mode);
    }

    public void StopMusic()
    {
        if (!CanControl) return;

        MusicState next = current;
        next.Playing = false;
        Publish(next);
    }

    void OnTrackFinished()
    {
        if (!CanControl || !current.Playing) return;
        Advance((MusicMode)current.Mode);
    }

    void Advance(MusicMode mode)
    {
        int count = library.Count((MusicSituation)current.Situation);
        if (count == 0) return;

        MusicState next = current;
        next.Track = MusicRules.NextTrack(mode, current.Track, count, UnityEngine.Random.Range(0, int.MaxValue));
        next.PlayId++;
        next.StartTime = Now();
        Publish(next);
    }

    void Publish(MusicState next)
    {
        Apply(next);
        if (IsSpawned && IsServer) state.Value = next;
    }

    void OnStateChanged(MusicState previous, MusicState next) => Apply(next);

    void Apply(MusicState next)
    {
        bool loop = next.Mode == (int)MusicMode.Loop;

        if (!next.Playing)
        {
            if (current.Playing) player.Stop();
        }
        else if (!current.Playing || next.PlayId != current.PlayId)
        {
            AudioClip clip = library.Get((MusicSituation)next.Situation, next.Track);
            player.Play(clip, Offset(next, clip, loop), loop);
        }
        else
        {
            player.SetLoop(loop);
        }

        current = next;
        Changed?.Invoke();
    }

    float Offset(MusicState music, AudioClip clip, bool loop)
    {
        if (clip == null) return 0f;
        double elapsed = Now() - music.StartTime;
        if (elapsed <= 0.5) return 0f;
        return loop ? (float)(elapsed % clip.length) : Mathf.Min((float)elapsed, clip.length);
    }

    double Now() => network.IsListening ? network.ServerTime.Time : Time.realtimeSinceStartupAsDouble;
}
