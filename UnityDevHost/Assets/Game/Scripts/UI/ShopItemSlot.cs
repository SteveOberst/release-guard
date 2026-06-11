using AttackSurfaceFixture.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AttackSurfaceFixture.Game.UI
{
    /// <summary>
    /// Single slot in the shop catalog list. Displays item name, description, price
    /// and affordability state; delegates click events to <see cref="ShopController"/>.
    /// </summary>
    public sealed class ShopItemSlot : MonoBehaviour
    {
        [SerializeField] private Text   itemNameLabel;
        [SerializeField] private Text   descriptionLabel;
        [SerializeField] private Text   priceLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private Image  premiumBadge;

        private ItemDefinition  _item;
        private ShopController  _owner;

        public void Bind(ItemDefinition item, ShopController owner)
        {
            _item  = item;
            _owner = owner;

            if (itemNameLabel    != null) itemNameLabel.text    = item.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = item.Description;
            if (priceLabel       != null) priceLabel.text       = $"{item.SoftCurrencyPrice}g";
            if (premiumBadge     != null) premiumBadge.enabled  = item.PremiumOnly;

            if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        }

        private void OnBuyClicked() => _owner?.OnItemSlotClicked(_item);
    }
}
