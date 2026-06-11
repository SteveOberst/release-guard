using AttackSurfaceFixture.Game.Core;
using UnityEngine;
using UnityEngine.Scripting;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Grants a flat bonus currency on every enemy defeat.
    /// Activated by name from a RemoteConfig value via <see cref="EventResponderHost"/>.
    ///
    /// <b>[Preserve] is required on this class.</b>
    /// <c>EventResponderHost</c> loads it by its full type name string
    /// (<c>"AttackSurfaceFixture.Game.Runtime.BonusRewardResponder"</c>) using
    /// <c>TypeBinder.FindType</c>. The managed linker cannot trace that string as a
    /// code reference, so without <c>[Preserve]</c> the class is eligible for removal
    /// at Medium or High stripping levels.
    ///
    /// This is <em>targeted</em> preservation: one attribute on one class that genuinely
    /// needs to survive dynamic loading. Contrast with the broad (and dangerous)
    /// <c>[assembly: Preserve]</c> which disables all stripping for the whole assembly.
    /// </summary>
    [Preserve]
    public sealed class BonusRewardResponder : IEventResponder
    {
        private const int BonusPerKill = 5;

        public void Activate()
        {
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
            Debug.Log($"[BonusReward] Activated  -- +{BonusPerKill} bonus currency per kill.");
        }

        public void Deactivate()
        {
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void HandleEnemyDefeated(
            AttackSurfaceFixture.Game.Data.EnemyDefinition enemy, int baseReward)
        {
            if (!ServiceLocator.TryGet<PlayerController>(out var player)) return;
            player.AddCurrency(BonusPerKill);
        }
    }
}
