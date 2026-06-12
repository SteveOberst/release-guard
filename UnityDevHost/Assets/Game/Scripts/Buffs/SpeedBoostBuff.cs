namespace AttackSurfaceFixture.Game.Buffs
{
    /// <summary>Adds a flat movement-speed bonus for a short duration.</summary>
    public sealed class SpeedBoostBuff : IBuffEffect
    {
        private const float BonusSpeed = 3f;
        private const float Duration = 8f;

        public string EffectId => "speed_boost";
        public string DisplayName => "Speed Boost";

        public void Apply(IBuffTarget target) =>
            target.ApplySpeedBoost(BonusSpeed, Duration);
    }
}