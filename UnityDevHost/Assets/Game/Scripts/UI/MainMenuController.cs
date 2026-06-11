using AttackSurfaceFixture.Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AttackSurfaceFixture.Game.UI
{
    /// <summary>
    /// Drives the main-menu screen. Subscribes to <see cref="GameEvents.OnMainMenuEntered"/>
    /// so it can animate in or refresh whenever the player returns to the menu (e.g. after
    /// a game-over) without needing to know which scene triggered the transition.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text   titleLabel;
        [SerializeField] private Text   versionLabel;

        private void Start()
        {
            if (titleLabel   != null) titleLabel.text   = "Attack Surface Demo";
            if (versionLabel != null) versionLabel.text = $"v{Application.version}";

            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnEnable()  => GameEvents.OnMainMenuEntered += HandleMainMenuEntered;
        private void OnDisable() => GameEvents.OnMainMenuEntered -= HandleMainMenuEntered;

        private void OnPlayClicked()
        {
            ServiceLocator.Get<GameStateManager>()?.StartGameplay();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleMainMenuEntered()
        {
            // Re-enable buttons in case a previous session left them disabled.
            if (playButton != null) playButton.interactable = true;
        }
    }
}
