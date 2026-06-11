namespace AttackSurfaceFixture.Game.Buffs
{
    /// <summary>Multiplies outgoing damage for a short duration.</summary>
    public sealed class DamageBoostBuff : IBuffEffect
    {
        private const float Multiplier = 1.5f;
        private const float Duration   = 10f;

        public string EffectId    => "damage_boost";
        public string DisplayName => "Damage Boost";

        public void Apply(IBuffTarget target) =>
            target.ApplyDamageMultiplier(Multiplier, Duration);
    }
}
