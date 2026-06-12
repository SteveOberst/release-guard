using UnityEngine;
using UnityEngine.SceneManagement;

namespace AttackSurfaceFixture.Game.Core
{
    /// <summary>
    /// Persistent, singleton game-state machine. Survives scene loads via
    /// <c>DontDestroyOnLoad</c> and drives top-level transitions.
    ///
    /// Scene indices mirror the build-settings order registered by <c>GameAssetFactory</c>:
    /// <list type="number">
    ///   <item>Bootstrap (index 0)  -- startup and service registration</item>
    ///   <item>MainMenu  (index 1)</item>
    ///   <item>Gameplay  (index 2)</item>
    ///   <item>Shop      (index 3)  -- loaded additively over Gameplay</item>
    /// </list>
    ///
    /// All state changes raise the appropriate <see cref="GameEvents"/> before loading
    /// so that listeners (HUD, analytics, etc.) can react before the scene unloads.
    /// </summary>
    public sealed class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Boot;

        private const int SceneIndexMainMenu = 1;
        private const int SceneIndexGameplay = 2;
        private const int SceneIndexShop = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameStateManager>();
        }

        // -- Transitions

        public void GoToMainMenu()
        {
            if (CurrentState == GameState.MainMenu) return;
            CurrentState = GameState.MainMenu;
            GameEvents.RaiseMainMenuEntered();
            SceneManager.LoadScene(SceneIndexMainMenu);
        }

        public void StartGameplay()
        {
            if (CurrentState == GameState.Gameplay) return;
            CurrentState = GameState.Gameplay;
            SceneManager.LoadScene(SceneIndexGameplay);
        }

        public void OpenShop()
        {
            if (CurrentState == GameState.Shop) return;
            CurrentState = GameState.Shop;
            GameEvents.RaiseShopOpened();
            // Additive load keeps the Gameplay scene  -- players can see the arena behind the shop UI.
            SceneManager.LoadScene(SceneIndexShop, LoadSceneMode.Additive);
        }

        public void CloseShop()
        {
            if (CurrentState != GameState.Shop) return;
            CurrentState = GameState.Gameplay;
            GameEvents.RaiseShopClosed();
            SceneManager.UnloadSceneAsync(SceneIndexShop);
        }

        public void TriggerGameOver()
        {
            if (CurrentState == GameState.GameOver) return;
            CurrentState = GameState.GameOver;
            GameEvents.RaisePlayerDied();
        }
    }

    public enum GameState
    {
        Boot,
        MainMenu,
        Gameplay,
        Shop,
        GameOver
    }
}