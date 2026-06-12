using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Scene-zero entry point. Registers all long-lived services into
    /// <see cref="ServiceLocator"/> and coordinates the initial scene transition.
    ///
    /// <b>DI pattern note:</b> Dependencies are supplied via serialized Inspector fields
    /// set either by the designer or by <c>GameAssetFactory</c> during development.
    /// This is the DI pattern that works cleanly under IL2CPP  -- no reflection scanning,
    /// no container magic. The <see cref="ServiceLocator"/> is fed explicitly typed
    /// instances that the linker can trace as direct references.
    ///
    /// Systems that self-register in their own <c>Awake</c> (e.g.
    /// <see cref="SaveSystem"/>, <see cref="AudioManager"/>) do not need a field here  --
    /// they register themselves before this <c>Awake</c> runs (Unity processes
    /// <c>Awake</c> depth-first through the hierarchy).
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private FeatureFlags featureFlags;

        [Header("Content Catalogs")] [SerializeField]
        private ItemDefinition[] starterItems;

        [SerializeField] private EnemyDefinition[] enemyCatalog;

        [Header("Persistent System Roots (DontDestroyOnLoad)")] [SerializeField]
        private GameStateManager gameStateManager;

        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private AudioManager audioManager;

        [Header("Scene Systems")] [SerializeField]
        private InventorySystem inventorySystem;

        [SerializeField] private ShopSystem shopSystem;
        [SerializeField] private RemoteConfigKit.RemoteConfigClient remoteConfigClient;

        // Inspector-readable accessors used by GameAssetFactory
        public EconomyConfig EconomyConfig => economyConfig;
        public FeatureFlags FeatureFlags => featureFlags;
        public ItemDefinition[] StarterItems => starterItems;
        public EnemyDefinition[] EnemyCatalog => enemyCatalog;

        private void Awake()
        {
            // Register config ScriptableObjects so any system can resolve them
            // without carrying a serialized field reference.
            if (economyConfig != null) ServiceLocator.Register(economyConfig);
            if (featureFlags != null) ServiceLocator.Register(featureFlags);

            // Opt the RemoteConfigClient into the service locator so EventResponderHost
            // can resolve it without a direct scene reference.
            if (remoteConfigClient != null) ServiceLocator.Register(remoteConfigClient);
        }

        private void Start()
        {
            // Seed the shop catalog from the authoritative config.
            shopSystem?.SetCatalog(economyConfig, starterItems);

            // Pre-populate the inventory with starter items for new players.
            inventorySystem?.SetStartingItems(starterItems);

            // Begin game-state machine  -- transitions to the main-menu scene.
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.GoToMainMenu();
            else if (gameStateManager != null)
                gameStateManager.GoToMainMenu();
        }

        // -- Factory helpers

        /// <summary>
        /// Called by <c>GameAssetFactory</c> to wire up serialized fields without
        /// manually setting each property through the Inspector.
        /// </summary>
        public void ApplyFixtureReferences(
            EconomyConfig economy,
            FeatureFlags flags,
            ItemDefinition[] starterItemCatalog,
            EnemyDefinition[] enemies,
            InventorySystem inventory,
            ShopSystem shop)
        {
            economyConfig = economy;
            featureFlags = flags;
            starterItems = starterItemCatalog;
            enemyCatalog = enemies;
            inventorySystem = inventory;
            shopSystem = shop;
        }
    }
}