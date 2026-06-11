using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using AttackSurfaceFixture.Game.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace AttackSurfaceFixture.Game.UI
{
    /// <summary>
    /// Gameplay heads-up display. Reflects live player state by subscribing to
    /// <see cref="GameEvents"/> rather than polling <c>PlayerController</c> every frame.
    ///
    /// Each event handler touches only the specific UI element it owns, making it easy
    /// to hot-swap individual UI pieces without cascading re-wiring.
    /// </summary>
    public sealed class GameplayHUD : MonoBehaviour
    {
        [Header("Player Status")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Text   currencyLabel;
        [SerializeField] private Text   buffStatusLabel;

        [Header("Combat")]
        [SerializeField] private Text   killCountLabel;
        [SerializeField] private Text   lastRewardLabel;
        [SerializeField] private Text   combatStatusLabel;

        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button retreatButton;

        private int _killCount;
        private int _maxHealth = 100;

        // -- Unity lifecycle

        private void Start()
        {
            if (shopButton   != null) shopButton.onClick.AddListener(OnShopClicked);
            if (retreatButton != null) retreatButton.onClick.AddListener(OnRetreatClicked);

            // Initialise from current player state if already in scene.
            if (ServiceLocator.TryGet<PlayerController>(out var player))
            {
                _maxHealth = player.MaxHealth;
                UpdateHealthBar(player.CurrentHealth);
                UpdateCurrencyLabel(player.SoftCurrency);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandleHealthChanged;
            GameEvents.OnCurrencyChanged     += HandleCurrencyChanged;
            GameEvents.OnEnemyDefeated       += HandleEnemyDefeated;
            GameEvents.OnBuffApplied         += HandleBuffApplied;
            GameEvents.OnCombatStarted       += HandleCombatStarted;
            GameEvents.OnCombatEnded         += HandleCombatEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandleHealthChanged;
            GameEvents.OnCurrencyChanged     -= HandleCurrencyChanged;
            GameEvents.OnEnemyDefeated       -= HandleEnemyDefeated;
            GameEvents.OnBuffApplied         -= HandleBuffApplied;
            GameEvents.OnCombatStarted       -= HandleCombatStarted;
            GameEvents.OnCombatEnded         -= HandleCombatEnded;
        }

        // -- Button callbacks

        private void OnShopClicked()    => ServiceLocator.Get<GameStateManager>()?.OpenShop();
        private void OnRetreatClicked() => ServiceLocator.Get<CombatSystem>()?.Retreat();

        // -- Event handlers

        private void HandleHealthChanged(int newHp)
        {
            UpdateHealthBar(newHp);
        }

        private void HandleCurrencyChanged(int balance)
        {
            UpdateCurrencyLabel(balance);
        }

        private void HandleEnemyDefeated(EnemyDefinition enemy, int reward)
        {
            _killCount++;
            if (killCountLabel  != null) killCountLabel.text  = $"Kills: {_killCount}";
            if (lastRewardLabel != null) lastRewardLabel.text = $"+{reward}g";
        }

        private void HandleBuffApplied(string effectId)
        {
            if (buffStatusLabel != null) buffStatusLabel.text = $"Buff: {effectId}";
        }

        private void HandleCombatStarted(EnemyDefinition enemy)
        {
            var name = enemy != null ? enemy.DisplayName : "Enemy";
            if (combatStatusLabel != null) combatStatusLabel.text = $"Fighting: {name}";
            if (retreatButton     != null) retreatButton.interactable = true;
        }

        private void HandleCombatEnded(bool victorious)
        {
            if (combatStatusLabel != null)
                combatStatusLabel.text = victorious ? "Victory!" : "Escaped";
            if (retreatButton != null) retreatButton.interactable = false;
        }

        // -- UI helpers

        private void UpdateHealthBar(int hp)
        {
            if (healthBar != null)
                healthBar.value = _maxHealth > 0 ? (float)hp / _maxHealth : 0f;
        }

        private void UpdateCurrencyLabel(int balance)
        {
            if (currencyLabel != null)
                currencyLabel.text = $"{balance}g";
        }
    }
}
