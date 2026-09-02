using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatSaberPlugin2.Util;

/// <summary>
/// Bridges work done on background threads (e.g. the STA thread used for
/// <see cref="System.Windows.Forms.OpenFileDialog"/>, or audio decoding) back onto
/// Unity's main thread, since almost every Unity/UnityEngine API can only be touched there.
/// </summary>
internal class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher? _instance;
    private readonly Queue<Action> _pending = new();
    private readonly object _lock = new();

    private static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            var go = new GameObject(nameof(MainThreadDispatcher));
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThreadDispatcher>();
            return _instance;
        }
    }

    /// <summary>
    /// Queues an action to run on Unity's main thread during the next <c>Update</c>.
    /// Safe to call from any thread.
    /// </summary>
    public static void Enqueue(Action action)
    {
        var dispatcher = Instance;
        lock (dispatcher._lock)
        {
            dispatcher._pending.Enqueue(action);
        }
    }

    private void Update()
    {
        while (true)
        {
            Action action;
            lock (_lock)
            {
                if (_pending.Count == 0)
                {
                    return;
                }

                action = _pending.Dequeue();
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Unhandled exception running dispatched main-thread action: {ex}");
            }
        }
    }
}
