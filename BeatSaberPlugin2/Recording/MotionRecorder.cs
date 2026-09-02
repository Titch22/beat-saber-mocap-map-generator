using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberPlugin2.Recording;

/// <summary>
/// Samples both hand poses every frame while recording, timestamped against a playing
/// <see cref="AudioSource"/> so the captured motion stays in sync with the song even if frame
/// timing drifts. Recording stops automatically once the audio source stops playing.
/// </summary>
internal class MotionRecorder : MonoBehaviour
{
    private readonly List<PoseSample> _samples = new();
    private AudioSource? _timeSource;

    public IReadOnlyList<PoseSample> Samples => _samples;

    public bool IsRecording { get; private set; }

    /// <summary>Starts a new recording, sampling poses in sync with <paramref name="timeSource"/>.time.</summary>
    public void StartRecording(AudioSource timeSource)
    {
        _samples.Clear();
        _timeSource = timeSource;
        IsRecording = true;
        Plugin.Log.Info("Motion recording started.");
    }

    public void StopRecording()
    {
        if (!IsRecording)
        {
            return;
        }

        IsRecording = false;
        Plugin.Log.Info(
            $"Motion recording stopped: {_samples.Count} samples captured over {(_samples.Count > 0 ? _samples[_samples.Count - 1].Time : 0f):F1}s.");
    }

    private void Update()
    {
        if (!IsRecording || _timeSource == null)
        {
            return;
        }

        if (!_timeSource.isPlaying)
        {
            StopRecording();
            return;
        }

        var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var leftPosition);
        leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var leftRotation);
        rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var rightPosition);
        rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rightRotation);

        _samples.Add(new PoseSample(_timeSource.time, leftPosition, leftRotation, rightPosition, rightRotation));
    }
}
