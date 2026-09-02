using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BeatSaberPlugin2.Util;

namespace BeatSaberPlugin2.UI;

/// <summary>
/// Opens a native Windows "open file" dialog to let the player pick an .mp3 from their PC.
/// <see cref="OpenFileDialog"/> requires an STA thread, which the Unity/Beat Saber process is
/// not running on, so the dialog is shown on a dedicated background thread and the result is
/// marshalled back onto Unity's main thread via <see cref="MainThreadDispatcher"/>.
/// </summary>
internal static class FileSelectDialog
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    /// <summary>
    /// Shows the file picker filtered to .mp3 files. <paramref name="onPicked"/> is invoked on
    /// Unity's main thread with the chosen path, or <c>null</c> if the user cancelled.
    /// </summary>
    public static void PickMp3(Action<string?> onPicked)
    {
        var thread = new Thread(() => RunDialog(onPicked))
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private static void RunDialog(Action<string?> onPicked)
    {
        string? result = null;
        try
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choisis une musique (.mp3)",
                Filter = "Fichiers MP3 (*.mp3)|*.mp3",
                CheckFileExists = true,
                Multiselect = false,
            };

            // Parent the dialog to whatever window currently has focus (Beat Saber's window)
            // so it reliably shows up in the foreground instead of behind the game.
            var owner = new WindowHandleWrapper(GetActiveWindow());
            var dialogResult = dialog.ShowDialog(owner);

            if (dialogResult == DialogResult.OK)
            {
                result = dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to show the mp3 file picker: {ex}");
        }
        finally
        {
            MainThreadDispatcher.Enqueue(() => onPicked(result));
        }
    }

    /// <summary>Minimal <see cref="IWin32Window"/> wrapper around a raw HWND.</summary>
    private sealed class WindowHandleWrapper(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}
