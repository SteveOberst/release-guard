using System.Collections.Generic;
using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Owns the shop catalog and mediates purchase transactions.
    ///
    /// Currency is deducted through <see cref="PlayerController"/> (resolved via
    /// <see cref="ServiceLocator"/>) so the shop never needs to own a direct reference
    /// to the player object, which may live in a different scene hierarchy.
    ///
    /// Analytics events are fired on successful purchase via
    /// <see cref="AnalyticsLite.AnalyticsService.Instance"/>  -- an optional singleton
    /// that is only referenced if present, so the shop compiles and runs without it.
    /// </summary>
    public sealed class ShopSystem : MonoBehaviour
    {
        [SerializeField] private EconomyConfig  economyConfig;
        [SerializeField] private FeatureFlags   featureFlags;
        [SerializeField] private ItemDefinition[] catalogItems = System.Array.Empty<ItemDefinition>();

        private readonly List<ItemDefinition> _catalog = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> Catalog    => _catalog;
        public int  RefreshCost => economyConfig != null ? economyConfig.ShopRefreshCost : 0;
        public bool IsOpen      => featureFlags  == null || featureFlags.ShopEnabled;

        // -- Unity lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
            PopulateCatalog(catalogItems);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ShopSystem>();
        }

        // -- Public API

        /// <summary>
        /// Attempts to purchase <paramref name="item"/> for the player.
        /// Returns a <see cref="PurchaseResult"/> describing the outcome.
        /// </summary>
        public PurchaseResult TryPurchase(ItemDefinition item)
        {
            if (!IsOpen)                        return PurchaseResult.ShopClosed;
            if (item == null)                   return PurchaseResult.InvalidItem;
            if (!_catalog.Contains(item))       return PurchaseResult.ItemNotInCatalog;

            if (item.PremiumOnly &&
                (featureFlags == null || !featureFlags.PremiumOffersEnabled))
                return PurchaseResult.PremiumDisabled;

            if (!ServiceLocator.TryGet<PlayerController>(out var player))
                return PurchaseResult.SystemUnavailable;

            if (!player.TryDeductCurrency(item.SoftCurrencyPrice))
                return PurchaseResult.InsufficientFunds;

            player.AddItem(item);
            GameEvents.RaiseItemPurchased(item, player.SoftCurrency);

            AnalyticsLite.AnalyticsService.Instance?.TrackEvent("item_purchased",
                new Dictionary<string, object>
                {
                    { "item_id",      item.ItemId },
                    { "price",        item.SoftCurrencyPrice },
                    { "premium_only", item.PremiumOnly }
                });

            return PurchaseResult.Success;
        }

        /// <summary>
        /// Spends the refresh fee and swaps in a new catalog if affordable.
        /// Returns false if the player cannot afford the refresh cost.
        /// </summary>
        public bool TryRefreshCatalog(ItemDefinition[] newCatalog)
        {
            if (!ServiceLocator.TryGet<PlayerController>(out var player)) return false;
            if (!player.TryDeductCurrency(RefreshCost)) return false;
            PopulateCatalog(newCatalog);
            return true;
        }

        /// <summary>
        /// Replaces the active catalog. Called by <c>Bootstrapper</c> on startup
        /// and may be called again when the player refreshes the shop.
        /// </summary>
        public void SetCatalog(EconomyConfig config, ItemDefinition[] items)
        {
            economyConfig = config;
            PopulateCatalog(items);
        }

        // -- Factory helpers

        /// <summary>Called by <c>GameAssetFactory</c> to seed serialized fields.</summary>
        public void ApplyFixtureCatalog(EconomyConfig config, ItemDefinition[] items)
        {
            economyConfig = config;
            catalogItems  = items ?? System.Array.Empty<ItemDefinition>();
        }

        // -- Private helpers

        private void PopulateCatalog(ItemDefinition[] items)
        {
            _catalog.Clear();
            if (items == null) return;
            foreach (var item in items)
                if (item != null) _catalog.Add(item);
        }
    }

    /// <summary>Outcome of a <see cref="ShopSystem.TryPurchase"/> call.</summary>
    public enum PurchaseResult
    {
        Success,
        InsufficientFunds,
        ItemNotInCatalog,
        PremiumDisabled,
        ShopClosed,
        InvalidItem,
        SystemUnavailable
    }
}
