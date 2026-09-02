using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberPlugin2.Diagnostics;

/// <summary>
/// Temporary technical spike: logs hand tracking poses roughly once per second so we can
/// confirm, from the BSIPA logs, that the XR input devices report non-zero, moving poses
/// outside of an actual gameplay scene (e.g. from the main menu). This is the biggest
/// technical unknown of the whole "generate a map from the player's movements" project - if
/// this doesn't work reliably, the capture approach needs to be rethought before anything else
/// is built on top of it.
/// </summary>
internal class XrTrackingSpike : MonoBehaviour
{
    private const float LogIntervalSeconds = 1f;
    private float _timeSinceLastLog;

    private void Update()
    {
        _timeSinceLastLog += Time.unscaledDeltaTime;
        if (_timeSinceLastLog < LogIntervalSeconds)
        {
            return;
        }

        _timeSinceLastLog = 0f;
        LogHandPose(XRNode.LeftHand, "LeftHand");
        LogHandPose(XRNode.RightHand, "RightHand");
    }

    private static void LogHandPose(XRNode node, string label)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            Plugin.Log.Info($"[XrTrackingSpike] {label}: no valid XR device at this node.");
            return;
        }

        var hasPosition = device.TryGetFeatureValue(CommonUsages.devicePosition, out var position);
        var hasRotation = device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation);
        var hasTracked = device.TryGetFeatureValue(CommonUsages.isTracked, out var isTracked);

        Plugin.Log.Info(
            $"[XrTrackingSpike] {label} ({device.name}): tracked={(hasTracked ? isTracked.ToString() : "?")} " +
            $"pos={(hasPosition ? position.ToString("F3") : "n/a")} " +
            $"rot={(hasRotation ? rotation.ToString("F3") : "n/a")}");
    }
}
