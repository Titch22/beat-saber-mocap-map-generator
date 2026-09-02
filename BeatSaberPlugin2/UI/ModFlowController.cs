using System;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberPlugin2.Audio;
using BeatSaberPlugin2.Util;
using UnityEngine;

namespace BeatSaberPlugin2.UI;

/// <summary>
/// Root MonoBehaviour for the mod. Wires up the main menu button, the mp3 file picker and,
/// for now, decoding + playback of the chosen track; recording/generation states will be added
/// in later steps.
/// </summary>
[RequireComponent(typeof(AudioSource))]
internal class ModFlowController : MonoBehaviour
{
    internal enum State
    {
        Idle,
        FileSelected,
        Decoding,
        Playing,
    }

    private const float RegistrationRetryIntervalSeconds = 1f;

    public static ModFlowController? Instance { get; private set; }

    public State CurrentState { get; private set; } = State.Idle;

    private MenuButton? _menuButton;
    private float _timeSinceLastRegistrationAttempt = RegistrationRetryIntervalSeconds;
    private AudioSource _audioSource = null!;

    private void Awake()
    {
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (_menuButton != null)
        {
            return;
        }

        // BeatSaberMarkupLanguage.MenuButtons.MenuButtons is a Zenject-bound singleton: its
        // Instance getter *throws* (rather than returning null) until Zenject's DiContainer has
        // finished installing, so we throttle the retries and swallow that expected exception
        // instead of hammering it (and the console) every single frame.
        _timeSinceLastRegistrationAttempt += Time.unscaledDeltaTime;
        if (_timeSinceLastRegistrationAttempt < RegistrationRetryIntervalSeconds)
        {
            return;
        }

        _timeSinceLastRegistrationAttempt = 0f;

        MenuButtons menuButtons;
        try
        {
            menuButtons = MenuButtons.Instance;
        }
        catch (InvalidOperationException)
        {
            // Not ready yet - retried on the next interval.
            return;
        }

        _menuButton = new MenuButton(
            "Générer une map",
            "Génère une map Beat Saber à partir de tes mouvements",
            StartFileSelection,
            interactable: true);
        menuButtons.RegisterButton(_menuButton);
        Plugin.Log.Info("Main menu button registered.");
    }

    private void OnDestroy()
    {
        if (_menuButton != null)
        {
            try
            {
                MenuButtons.Instance.UnregisterButton(_menuButton);
            }
            catch (InvalidOperationException)
            {
                // DiContainer already torn down - nothing to unregister from.
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartFileSelection()
    {
        Plugin.Log.Info("Opening mp3 file picker...");
        FileSelectDialog.PickMp3(OnMp3Picked);
    }

    private void OnMp3Picked(string? path)
    {
        if (path == null)
        {
            Plugin.Log.Info("Mp3 selection cancelled by the user.");
            return;
        }

        Plugin.Log.Info($"Mp3 selected: {path}");
        CurrentState = State.FileSelected;
        DecodeAndPlay(path);
    }

    private void DecodeAndPlay(string path)
    {
        CurrentState = State.Decoding;
        Plugin.Log.Info("Decoding mp3...");

        // Decoding can take a noticeable amount of time for longer tracks - keep it off the
        // main thread so the game doesn't freeze while it runs.
        Task.Run(() =>
        {
            try
            {
                var pcm = Mp3Decoder.Decode(path);
                MainThreadDispatcher.Enqueue(() => OnDecoded(path, pcm));
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Failed to decode mp3 '{path}': {ex}");
                MainThreadDispatcher.Enqueue(() => CurrentState = State.FileSelected);
            }
        });
    }

    private void OnDecoded(string path, PcmAudio pcm)
    {
        var clipName = Path.GetFileNameWithoutExtension(path);
        var clip = AudioClipFactory.Create(clipName, pcm);
        Plugin.Log.Info(
            $"Decoded '{clipName}': {clip.length:F1}s, {pcm.Channels}ch @ {pcm.SampleRate}Hz. Playing...");

        _audioSource.clip = clip;
        _audioSource.Play();
        CurrentState = State.Playing;

        // TODO(next step): start recording hand movements in sync with _audioSource.time.
    }
}
