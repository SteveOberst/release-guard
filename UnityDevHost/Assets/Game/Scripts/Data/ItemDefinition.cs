using UnityEngine;

namespace AttackSurfaceFixture.Game.Data
{
    [CreateAssetMenu(menuName = "Attack Surface Fixture/Data/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] [TextArea] private string description = string.Empty;
        [SerializeField] private int softCurrencyPrice;
        [SerializeField] private bool premiumOnly;

        /// <summary>
        /// Maps to a key in <see cref="Buffs.BuffEffectRegistry"/>.
        /// Leave empty for passive items (weapons, armour) that have no use-activated effect.
        /// </summary>
        [SerializeField] private string effectId = string.Empty;

        public string ItemId             => itemId;
        public string DisplayName        => displayName;
        public string Description        => description;
        public int    SoftCurrencyPrice  => softCurrencyPrice;
        public bool   PremiumOnly        => premiumOnly;
        public string EffectId           => effectId;
    }
}
