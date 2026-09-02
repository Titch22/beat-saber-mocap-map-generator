using BeatSaberMarkupLanguage;
using HMUI;
using TMPro;
using UnityEngine;

namespace BeatSaberPlugin2.UI;

/// <summary>
/// Full-screen view showing the pre-recording countdown. Presenting a <see cref="FlowCoordinator"/>
/// that owns this as its main view controller replaces whatever was on screen before (e.g. the
/// main menu), which is how we "hide" the menu while the player gets ready.
/// </summary>
internal class CountdownViewController : ViewController
{
    private CurvedTextMeshPro? _text;

    public void SetText(string text)
    {
        if (_text != null)
        {
            _text.text = text;
        }
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

        if (!firstActivation)
        {
            return;
        }

        _text = BeatSaberUI.CreateCurvedUIText(
            rectTransform,
            string.Empty,
            anchorMin: Vector2.zero,
            anchorMax: Vector2.one,
            anchoredPosition: Vector2.zero,
            sizeDelta: Vector2.zero);
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = 10f;
    }
}
