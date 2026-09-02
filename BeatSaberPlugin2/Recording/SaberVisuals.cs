using System;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberPlugin2.Recording;

/// <summary>
/// Draws two simple placeholder blades that follow the tracked hand poses, so the player has
/// something to aim with while recording - normally the player only sees bare controllers here
/// since we deliberately don't load the real gameplay scene/saber prefabs (see project notes on
/// why). Not tied to <see cref="MotionRecorder"/> - purely visual, shown/hidden independently.
/// Every step here is defensive: if blade creation fails for any reason (e.g. no usable shader),
/// we log it and simply don't show a blade rather than crashing every frame afterwards.
/// </summary>
internal class SaberVisuals : MonoBehaviour
{
    private const float BladeLength = 0.8f;
    private const float BladeThickness = 0.03f;

    /// <summary>
    /// A VR controller's raw tracked rotation doesn't point "forward" the way a naturally-held
    /// blade would - controllers are typically gripped at a downward tilt relative to their own
    /// local forward axis. This rotates the blade's forward direction relative to the
    /// controller's raw pose to compensate. Calibrated empirically in-game.
    /// </summary>
    private static readonly Quaternion GripTiltCorrection = Quaternion.Euler(20f, 0f, 0f);

    private static readonly string[] ShaderCandidates =
    {
        "Standard", "Unlit/Color", "Sprites/Default", "UI/Default", "Legacy Shaders/Diffuse",
    };

    private static Shader? _cachedShader;
    private static bool _shaderLookupDone;

    private Transform? _leftBlade;
    private Transform? _rightBlade;

    private void Awake()
    {
        _leftBlade = CreateBlade(new Color(1f, 0.1f, 0.1f));
        _rightBlade = CreateBlade(new Color(0.1f, 0.6f, 1f));
    }

    public void Show()
    {
        SetActive(_leftBlade, true);
        SetActive(_rightBlade, true);
    }

    public void Hide()
    {
        SetActive(_leftBlade, false);
        SetActive(_rightBlade, false);
    }

    private void Update()
    {
        if (_leftBlade != null && _leftBlade.gameObject.activeSelf)
        {
            UpdateBladePose(_leftBlade, XRNode.LeftHand);
        }

        if (_rightBlade != null && _rightBlade.gameObject.activeSelf)
        {
            UpdateBladePose(_rightBlade, XRNode.RightHand);
        }
    }

    private static void SetActive(Transform? blade, bool active)
    {
        if (blade != null)
        {
            blade.gameObject.SetActive(active);
        }
    }

    private static void UpdateBladePose(Transform blade, XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            return;
        }

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out var position) ||
            !device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation))
        {
            return;
        }

        var bladeRotation = rotation * GripTiltCorrection;
        blade.SetPositionAndRotation(position + (bladeRotation * Vector3.forward * (BladeLength / 2f)), bladeRotation);
    }

    private Transform? CreateBlade(Color color)
    {
        try
        {
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "BeatSaberPlugin2_SaberVisual";
            blade.transform.SetParent(transform, worldPositionStays: false);

            var collider = blade.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            blade.transform.localScale = new Vector3(BladeThickness, BladeThickness, BladeLength);

            var shader = FindBladeShader();
            if (shader != null)
            {
                blade.GetComponent<Renderer>().material = new Material(shader) { color = color };
            }

            blade.SetActive(false);
            return blade.transform;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"[SaberVisuals] Failed to create a blade visual, sabers won't be shown: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Beat Saber's shipped player build may not include every common built-in shader (unused
    /// ones can be stripped), so this tries a few known-safe fallbacks instead of assuming
    /// "Standard" is always available.
    /// </summary>
    private static Shader? FindBladeShader()
    {
        if (_shaderLookupDone)
        {
            return _cachedShader;
        }

        _shaderLookupDone = true;
        foreach (var name in ShaderCandidates)
        {
            var shader = Shader.Find(name);
            if (shader != null)
            {
                Plugin.Log.Info($"[SaberVisuals] Using shader '{name}' for the blade material.");
                _cachedShader = shader;
                return shader;
            }
        }

        Plugin.Log.Warn("[SaberVisuals] No known shader available - blades will use the primitive's default material.");
        return null;
    }
}
