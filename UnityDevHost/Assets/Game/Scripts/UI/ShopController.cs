using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using AttackSurfaceFixture.Game.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace AttackSurfaceFixture.Game.UI
{
    /// <summary>
    /// Renders the shop catalog and dispatches purchase requests to <see cref="ShopSystem"/>.
    ///
    /// Each catalog slot is driven by a child <see cref="ShopItemSlot"/> component so the
    /// layout can be changed in the Prefab without touching this controller. This controller
    /// is only responsible for populating slots and showing purchase feedback.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [Header("Layout")] [SerializeField] private Transform itemListRoot;
        [SerializeField] private ShopItemSlot itemSlotPrefab;

        [Header("Header / Footer")] [SerializeField]
        private Text currencyLabel;

        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;

        private ShopSystem _shop;

        // -- Unity lifecycle

        private void Awake()
        {
            ServiceLocator.TryGet(out _shop);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnCurrencyChanged += HandleCurrencyChanged;
            GameEvents.OnShopOpened += RefreshDisplay;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnCurrencyChanged -= HandleCurrencyChanged;
            GameEvents.OnShopOpened -= RefreshDisplay;
        }

        // -- Button callbacks

        private void OnCloseClicked() => ServiceLocator.Get<GameStateManager>()?.CloseShop();

        private void OnRefreshClicked()
        {
            if (_shop == null) return;
            // In a real game pass a freshly generated catalog here.
            // For this demo we simply re-show the existing one after paying the fee.
            if (_shop.TryRefreshCatalog(_shop.Catalog as ItemDefinition[]))
                RefreshDisplay();
            else
                ShowFeedback("Not enough gold to refresh!");
        }

        public void OnItemSlotClicked(ItemDefinition item)
        {
            if (_shop == null) return;
            var result = _shop.TryPurchase(item);
            ShowFeedback(result == PurchaseResult.Success
                ? $"Purchased {item.DisplayName}!"
                : $"Purchase failed: {result}");
        }

        // -- Event handlers

        private void HandleCurrencyChanged(int balance)
        {
            if (currencyLabel != null) currencyLabel.text = $"{balance}g";
        }

        // -- UI helpers

        private void RefreshDisplay()
        {
            if (itemListRoot == null) return;

            // Clear old slots
            for (var i = itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(itemListRoot.GetChild(i).gameObject);

            if (_shop == null) return;

            // Refresh currency label
            if (ServiceLocator.TryGet<PlayerController>(out var player) && currencyLabel != null)
                currencyLabel.text = $"{player.SoftCurrency}g";

            // Spawn a slot per catalog entry
            foreach (var item in _shop.Catalog)
            {
                if (itemSlotPrefab != null)
                {
                    var slot = Instantiate(itemSlotPrefab, itemListRoot);
                    slot.Bind(item, this);
                }
                else
                {
                    // Fallback when no prefab is assigned: just log the catalog state.
                    Debug.Log($"[ShopController] Catalog: {item.DisplayName}  -- {item.SoftCurrencyPrice}g");
                }
            }
        }

        private void ShowFeedback(string message)
        {
            if (feedbackLabel != null) feedbackLabel.text = message;
            Debug.Log($"[Shop] {message}");
        }
    }
}