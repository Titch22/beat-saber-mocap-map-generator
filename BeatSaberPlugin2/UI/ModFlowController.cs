using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using BeatSaberPlugin2.Audio;
using BeatSaberPlugin2.Recording;
using BeatSaberPlugin2.Util;
using HMUI;
using UnityEngine;

namespace BeatSaberPlugin2.UI;

/// <summary>
/// Root MonoBehaviour for the mod. Wires up the main menu button, the mp3 file picker,
/// decoding, a pre-song countdown, and recording the player's hand movements while the song
/// plays; map generation will be added in a later step.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MotionRecorder))]
internal class ModFlowController : MonoBehaviour
{
    internal enum State
    {
        Idle,
        FileSelected,
        Decoding,
        CountingDown,
        Playing,
    }

    private const float RegistrationRetryIntervalSeconds = 1f;
    private const int CountdownSeconds = 10;

    public static ModFlowController? Instance { get; private set; }

    public State CurrentState { get; private set; } = State.Idle;

    private MenuButton? _menuButton;
    private float _timeSinceLastRegistrationAttempt = RegistrationRetryIntervalSeconds;
    private AudioSource _audioSource = null!;
    private MotionRecorder _motionRecorder = null!;
    private CountdownFlowCoordinator? _countdownFlowCoordinator;

    private void Awake()
    {
        Instance = this;

        // [RequireComponent] doesn't guarantee these are attached before Awake runs on this
        // component, so fetch-or-add explicitly rather than relying on GetComponent alone.
        _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _motionRecorder = GetComponent<MotionRecorder>() ?? gameObject.AddComponent<MotionRecorder>();
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
        Plugin.Log.Info($"Decoded '{clipName}': {clip.length:F1}s, {pcm.Channels}ch @ {pcm.SampleRate}Hz.");

        _audioSource.clip = clip;
        CurrentState = State.CountingDown;

        // Presenting our own flow coordinator over the main menu's is what hides the menu -
        // it replaces whatever was on screen, the same way opening any BS menu (settings,
        // song list, ...) does.
        _countdownFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<CountdownFlowCoordinator>();
        BeatSaberUI.PresentFlowCoordinator(
            BeatSaberUI.MainFlowCoordinator,
            _countdownFlowCoordinator,
            finishedCallback: null,
            animationDirection: ViewController.AnimationDirection.Horizontal,
            immediately: false,
            replaceTopViewController: false);

        StartCoroutine(CountdownThenPlayAndRecord());
    }

    private IEnumerator CountdownThenPlayAndRecord()
    {
        for (var remaining = CountdownSeconds; remaining > 0; remaining--)
        {
            _countdownFlowCoordinator!.CountdownView.SetText(
                $"Musique dans {remaining}...\nPrépare-toi à agiter les bras en rythme !");
            yield return new WaitForSeconds(1f);
        }

        // Start playback and recording on the same frame so the recorded poses stay aligned
        // with AudioSource.time from the very first sample.
        _audioSource.Play();
        _motionRecorder.StartRecording(_audioSource);
        CurrentState = State.Playing;
        _countdownFlowCoordinator!.CountdownView.SetText("Enregistrement en cours...");

        // TODO(next step): generate the map from the samples instead of just logging them.
        yield return new WaitUntil(() => !_motionRecorder.IsRecording);

        _countdownFlowCoordinator!.CountdownView.SetText("Terminé !");
        BeatSaberUI.DismissFlowCoordinator(
            BeatSaberUI.MainFlowCoordinator,
            _countdownFlowCoordinator,
            finishedCallback: null,
            animationDirection: ViewController.AnimationDirection.Horizontal,
            immediately: false);
        _countdownFlowCoordinator = null;
        CurrentState = State.Idle;
    }
}
