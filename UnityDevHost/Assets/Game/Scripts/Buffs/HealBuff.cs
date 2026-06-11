using UnityEngine;

namespace AttackSurfaceFixture.Game.Buffs
{
    /// <summary>Restores a fixed amount of HP when a healing consumable is used.</summary>
    public sealed class HealBuff : IBuffEffect
    {
        private const int HealAmount = 30;

        public string EffectId    => "heal";
        public string DisplayName => "Heal";

        public void Apply(IBuffTarget target)
        {
            var missing = target.MaxHealth - target.CurrentHealth;
            var healed  = Mathf.Min(HealAmount, missing);
            if (healed > 0)
                target.Heal(healed);
        }
    }
}
