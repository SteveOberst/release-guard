using System.Collections.Generic;
using AttackSurfaceFixture.Game.Buffs;
using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Owns the player's in-session state: health, active buffs, currency and inventory.
    /// Implements <see cref="IBuffTarget"/> so the buff system can modify the player
    /// without taking a hard dependency on this class.
    ///
    /// Registers itself with <see cref="ServiceLocator"/> so other systems (shop, combat,
    /// UI) can resolve it without direct Inspector wiring across scene hierarchies.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour, IBuffTarget
    {
        [SerializeField] private EconomyConfig economyConfig;

        // -- Public read-only state

        public int CurrentHealth { get; private set; }
        public int MaxHealth     { get; private set; }
        public int SoftCurrency  { get; private set; }

        /// <summary>
        /// Effective damage multiplier after applying all active damage-boost buffs.
        /// Combat systems read this value to scale outgoing damage.
        /// </summary>
        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>Effective movement speed after applying all active speed-boost buffs.</summary>
        public float MoveSpeed { get; private set; } = 5f;

        // -- Private state

        private readonly List<string>     _ownedItemIds = new List<string>();
        private readonly List<ActiveBuff> _activeBuffs  = new List<ActiveBuff>();

        private const float BaseSpeed  = 5f;
        private const float BaseDamage = 1f;   // multiplier baseline

        // -- Unity lifecycle

        private void Awake()
        {
            ServiceLocator.Register<PlayerController>(this);
        }

        private void Start()
        {
            // Load persisted state if a SaveSystem is present, otherwise use defaults.
            if (ServiceLocator.TryGet<SaveSystem>(out var save))
                ApplyPlayerData(save.Load());
            else
                ApplyDefaults();
        }

        private void Update()
        {
            TickBuffs(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerController>();
        }

        // -- Damage / healing

        public void TakeDamage(int amount)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(1, amount));
            GameEvents.RaisePlayerHealthChanged(CurrentHealth);

            if (CurrentHealth <= 0)
                ServiceLocator.Get<GameStateManager>()?.TriggerGameOver();
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + Mathf.Max(0, amount));
            GameEvents.RaisePlayerHealthChanged(CurrentHealth);
        }

        // -- IBuffTarget implementation

        public void ApplyDamageMultiplier(float multiplier, float durationSeconds)
        {
            _activeBuffs.Add(new ActiveBuff(BuffType.DamageBoost, durationSeconds, multiplier));
            RefreshStats();
            GameEvents.RaiseBuffApplied("damage_boost");
        }

        public void ApplySpeedBoost(float bonusUnitsPerSecond, float durationSeconds)
        {
            _activeBuffs.Add(new ActiveBuff(BuffType.SpeedBoost, durationSeconds, bonusUnitsPerSecond));
            RefreshStats();
            GameEvents.RaiseBuffApplied("speed_boost");
        }

        // -- Item usage

        /// <summary>
        /// Attempts to use <paramref name="item"/> from the player's bag.
        /// Returns false if the item is not owned or has no registered effect.
        /// </summary>
        public bool TryUseItem(ItemDefinition item)
        {
            if (item == null || !_ownedItemIds.Contains(item.ItemId))
                return false;

            if (!string.IsNullOrEmpty(item.EffectId) &&
                BuffEffectRegistry.TryGetEffect(item.EffectId, out var effect))
            {
                effect.Apply(this);
            }

            _ownedItemIds.Remove(item.ItemId);
            GameEvents.RaiseItemUsed(item);
            return true;
        }

        public void AddItem(ItemDefinition item)
        {
            if (item != null) _ownedItemIds.Add(item.ItemId);
        }

        public bool OwnsItem(string itemId) => _ownedItemIds.Contains(itemId);
        public IReadOnlyList<string> OwnedItemIds => _ownedItemIds;

        // -- Currency

        /// <summary>
        /// Deducts <paramref name="amount"/> from soft currency.
        /// Returns false without modifying state if insufficient funds.
        /// </summary>
        public bool TryDeductCurrency(int amount)
        {
            if (SoftCurrency < amount) return false;
            SoftCurrency -= amount;
            GameEvents.RaiseCurrencyChanged(SoftCurrency);
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            SoftCurrency += amount;
            GameEvents.RaiseCurrencyChanged(SoftCurrency);
        }

        // -- Persistence

        /// <summary>
        /// Captures the current player state into a <see cref="PlayerData"/> snapshot.
        /// Pass to <see cref="SaveSystem.Save"/> to persist.
        /// </summary>
        public PlayerData CaptureData() => new PlayerData
        {
            softCurrency   = SoftCurrency,
            currentHealth  = CurrentHealth,
            maxHealth      = MaxHealth,
            ownedItemIds   = new List<string>(_ownedItemIds)
        };

        // -- Private helpers

        private void ApplyPlayerData(PlayerData data)
        {
            MaxHealth     = data.maxHealth;
            CurrentHealth = data.currentHealth;
            SoftCurrency  = data.softCurrency;

            _ownedItemIds.Clear();
            if (data.ownedItemIds != null)
                _ownedItemIds.AddRange(data.ownedItemIds);
        }

        private void ApplyDefaults()
        {
            MaxHealth     = 100;
            CurrentHealth = 100;
            SoftCurrency  = economyConfig != null ? economyConfig.StartingSoftCurrency : 100;
        }

        private void TickBuffs(float deltaTime)
        {
            var dirty = false;
            for (var i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var b = _activeBuffs[i];
                b.RemainingSeconds -= deltaTime;
                if (b.RemainingSeconds <= 0f)
                {
                    _activeBuffs.RemoveAt(i);
                    dirty = true;
                }
                else
                {
                    _activeBuffs[i] = b;
                }
            }

            if (dirty) RefreshStats();
        }

        private void RefreshStats()
        {
            var dmg   = BaseDamage;
            var speed = BaseSpeed;

            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == BuffType.DamageBoost)
                    dmg   *= buff.Value;
                else if (buff.Type == BuffType.SpeedBoost)
                    speed += buff.Value;
            }

            DamageMultiplier = dmg;
            MoveSpeed        = speed;
        }

        // -- Inner types

        private enum BuffType { DamageBoost, SpeedBoost }

        /// <summary>
        /// Value type tracking one active buff.
        ///
        /// Using a struct here avoids per-tick heap allocation; the list itself lives
        /// on the heap but individual buff updates are stack-only operations.
        /// </summary>
        private struct ActiveBuff
        {
            public BuffType Type;
            public float    RemainingSeconds;
            public float    Value;

            public ActiveBuff(BuffType type, float duration, float value)
            {
                Type             = type;
                RemainingSeconds = duration;
                Value            = value;
            }
        }
    }
}
