using System.Collections.Generic;
using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Tracks the player's owned items in memory and keeps in sync with
    /// <see cref="GameEvents"/> so UI and other systems always reflect
    /// the current inventory without polling.
    ///
    /// Registers itself with <see cref="ServiceLocator"/> so the shop and
    /// combat systems can query the bag without needing an Inspector reference.
    /// </summary>
    public sealed class InventorySystem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition[] startingItems = System.Array.Empty<ItemDefinition>();

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> Items => _items;
        public int Count => _items.Count;

        // -- Unity lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
            foreach (var item in startingItems)
                if (item != null)
                    _items.Add(item);
        }

        private void OnEnable()
        {
            GameEvents.OnItemPurchased += HandleItemPurchased;
            GameEvents.OnItemUsed += HandleItemUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnItemPurchased -= HandleItemPurchased;
            GameEvents.OnItemUsed -= HandleItemUsed;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<InventorySystem>();
        }

        // -- Public API

        public void AddItem(ItemDefinition item)
        {
            if (item != null) _items.Add(item);
        }

        public bool RemoveItem(ItemDefinition item) =>
            item != null && _items.Remove(item);

        public bool Contains(ItemDefinition item) =>
            item != null && _items.Contains(item);

        // -- Factory helper

        /// <summary>Called by <c>GameAssetFactory</c> to seed the Inspector field.</summary>
        public void SetStartingItems(ItemDefinition[] items) =>
            startingItems = items ?? System.Array.Empty<ItemDefinition>();

        // -- Event handlers

        private void HandleItemPurchased(ItemDefinition item, int _) => AddItem(item);
        private void HandleItemUsed(ItemDefinition item) => RemoveItem(item);
    }
}