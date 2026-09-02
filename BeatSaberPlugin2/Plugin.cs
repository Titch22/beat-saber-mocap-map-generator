using BeatSaberPlugin2.Diagnostics;
using BeatSaberPlugin2.UI;
using IPA;
using IPA.Loader;
using UnityEngine;
using IpaLogger = IPA.Logging.Logger;

namespace BeatSaberPlugin2;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;

    private GameObject? _rootObject;

    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    [OnStart]
    public void OnApplicationStart()
    {
        _rootObject = new GameObject(nameof(BeatSaberPlugin2));
        Object.DontDestroyOnLoad(_rootObject);

        // Technical spike: confirm hand tracking poses are readable outside of gameplay.
        // Remove once the movement recorder (later step) supersedes it.
        _rootObject.AddComponent<XrTrackingSpike>();

        _rootObject.AddComponent<ModFlowController>();
    }

    [OnExit]
    public void OnApplicationQuit()
    {
        Log.Debug("OnApplicationQuit");
    }
}