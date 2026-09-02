using BeatSaberMarkupLanguage;
using HMUI;

namespace BeatSaberPlugin2.UI;

/// <summary>
/// Dedicated flow coordinator presented over the main menu while the player is getting ready
/// and while the song plays/records - presenting it is what actually hides the main menu.
/// </summary>
internal class CountdownFlowCoordinator : FlowCoordinator
{
    // Named differently from HMUI.ViewController on purpose - a property named ViewController
    // here would shadow the type name and break references to HMUI.ViewController.AnimationType.
    public CountdownViewController CountdownView { get; private set; } = null!;

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            SetTitle("Générer une map", global::HMUI.ViewController.AnimationType.None);
            showBackButton = false;
            CountdownView = BeatSaberUI.CreateViewController<CountdownViewController>();
        }

        ProvideInitialViewControllers(CountdownView);
    }
}
