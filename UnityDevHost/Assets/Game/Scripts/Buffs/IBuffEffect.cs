namespace AttackSurfaceFixture.Game.Buffs
{
    /// <summary>
    /// Effect applied to a player when a consumable item is used.
    ///
    /// <b>IL2CPP note:</b> Interface dispatch uses a v-table, <em>not</em> reflection.
    /// Concrete types listed in <see cref="BuffEffectRegistry"/> are directly referenced
    /// there and therefore preserved by the linker without any <c>[Preserve]</c> attribute.
    ///
    /// If you ever needed to instantiate a concrete type by its string name at runtime
    /// (e.g. from a RemoteConfig value or a user-defined mod), you would need
    /// <c>[Preserve]</c> on <em>that specific type</em>. See
    /// <c>EventResponderHost</c> for a concrete example of that pattern.
    /// </summary>
    public interface IBuffEffect
    {
        /// <summary>Short identifier matching the key used in <see cref="BuffEffectRegistry"/>.</summary>
        string EffectId { get; }

        /// <summary>Human-readable name for UI display.</summary>
        string DisplayName { get; }

        /// <summary>Applies the effect to <paramref name="target"/>.</summary>
        void Apply(IBuffTarget target);
    }

    /// <summary>
    /// Exposes only the subset of player state that buff effects need to modify.
    /// Keeping this narrow means the buff system does not take a hard dependency on
    /// <c>PlayerController</c>, making effects independently testable.
    /// </summary>
    public interface IBuffTarget
    {
        int CurrentHealth { get; }
        int MaxHealth     { get; }

        void Heal(int amount);
        void ApplyDamageMultiplier(float multiplier, float durationSeconds);
        void ApplySpeedBoost(float bonusUnitsPerSecond, float durationSeconds);
    }
}
