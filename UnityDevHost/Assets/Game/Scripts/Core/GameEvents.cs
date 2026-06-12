using System;
using AttackSurfaceFixture.Game.Data;

namespace AttackSurfaceFixture.Game.Core
{
    /// <summary>
    /// Central typed event hub. All game systems communicate through events declared here,
    /// keeping them decoupled from each other without a heap of direct component references.
    ///
    /// <b>IL2CPP note:</b> C# <c>event Action&lt;T&gt;</c> delegates compile to a direct
    /// function-pointer table in IL2CPP  -- no reflection, no boxing for value-type parameters
    /// passed by value. This pattern is unconditionally safe under managed stripping because
    /// the linker traces every subscriber and publisher as a direct field reference.
    ///
    /// Compare to the alternatives:
    /// <list type="bullet">
    ///   <item><c>UnityEvent</c>  -- serializable and Inspector-friendly, but boxes every
    ///   argument and adds allocation overhead.</item>
    ///   <item><c>SendMessage</c>  -- string-keyed lookup, no compile-time safety, slower.</item>
    ///   <item>ScriptableObject event channels  -- great for cross-scene references, but
    ///   requires editor assets per event; overkill for single-binary communication.</item>
    /// </list>
    /// </summary>
    public static class GameEvents
    {
        // -- Player

        /// <summary>Raised whenever the player's HP changes. Parameter: new health value.</summary>
        public static event Action<int> OnPlayerHealthChanged;

        /// <summary>Raised when the player dies (health reaches zero).</summary>
        public static event Action OnPlayerDied;

        /// <summary>Raised when a timed buff is applied. Parameter: effect id string.</summary>
        public static event Action<string> OnBuffApplied;

        // -- Economy

        /// <summary>Raised on any currency change. Parameter: new soft-currency balance.</summary>
        public static event Action<int> OnCurrencyChanged;

        /// <summary>Raised after a successful item purchase. Parameters: item, remaining balance.</summary>
        public static event Action<ItemDefinition, int> OnItemPurchased;

        /// <summary>Raised when a consumable item is used from inventory.</summary>
        public static event Action<ItemDefinition> OnItemUsed;

        // -- Combat

        /// <summary>Raised when combat with an enemy begins.</summary>
        public static event Action<EnemyDefinition> OnCombatStarted;

        /// <summary>
        /// Raised when a combat encounter ends.
        /// Parameter: true if the player won, false if they retreated or died.
        /// </summary>
        public static event Action<bool> OnCombatEnded;

        /// <summary>Raised when an enemy is defeated. Parameters: definition, soft-currency reward.</summary>
        public static event Action<EnemyDefinition, int> OnEnemyDefeated;

        // -- Navigation

        /// <summary>Raised immediately before loading the main-menu scene.</summary>
        public static event Action OnMainMenuEntered;

        /// <summary>Raised when the shop overlay is opened.</summary>
        public static event Action OnShopOpened;

        /// <summary>Raised when the shop overlay is closed.</summary>
        public static event Action OnShopClosed;

        // -- Raise helpers
        // Centralising the null-conditional invoke here means callers never need to
        // perform the null check themselves.

        public static void RaisePlayerHealthChanged(int hp) => OnPlayerHealthChanged?.Invoke(hp);
        public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
        public static void RaiseBuffApplied(string effectId) => OnBuffApplied?.Invoke(effectId);

        public static void RaiseCurrencyChanged(int balance) => OnCurrencyChanged?.Invoke(balance);

        public static void RaiseItemPurchased(ItemDefinition item, int remainingBalance) =>
            OnItemPurchased?.Invoke(item, remainingBalance);

        public static void RaiseItemUsed(ItemDefinition item) => OnItemUsed?.Invoke(item);

        public static void RaiseCombatStarted(EnemyDefinition enemy) => OnCombatStarted?.Invoke(enemy);
        public static void RaiseCombatEnded(bool victorious) => OnCombatEnded?.Invoke(victorious);

        public static void RaiseEnemyDefeated(EnemyDefinition enemy, int reward) =>
            OnEnemyDefeated?.Invoke(enemy, reward);

        public static void RaiseMainMenuEntered() => OnMainMenuEntered?.Invoke();
        public static void RaiseShopOpened() => OnShopOpened?.Invoke();
        public static void RaiseShopClosed() => OnShopClosed?.Invoke();

        // -- Teardown

        /// <summary>
        /// Clears all subscribers. Call during scene unload or test teardown to prevent
        /// stale delegates holding scene objects alive across scene transitions.
        /// </summary>
        public static void ClearAll()
        {
            OnPlayerHealthChanged = null;
            OnPlayerDied = null;
            OnBuffApplied = null;
            OnCurrencyChanged = null;
            OnItemPurchased = null;
            OnItemUsed = null;
            OnCombatStarted = null;
            OnCombatEnded = null;
            OnEnemyDefeated = null;
            OnMainMenuEntered = null;
            OnShopOpened = null;
            OnShopClosed = null;
        }
    }
}