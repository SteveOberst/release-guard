using System.Collections;
using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Mediates a turn-based combat encounter between the player and one enemy.
    ///
    /// Uses a <c>Coroutine</c>-based tick loop rather than per-frame <c>Update</c> logic.
    /// This decouples combat timing from frame rate, makes it easy to pause or skip turns,
    /// and keeps the loop's intent readable as a sequential state machine in a single method.
    ///
    /// The player's effective damage is read from <see cref="PlayerController.DamageMultiplier"/>
    /// each turn, so active buffs (e.g. damage boost) affect the current encounter without
    /// any additional wiring.
    /// </summary>
    public sealed class CombatSystem : MonoBehaviour
    {
        [SerializeField] private float turnIntervalSeconds = 1.5f;

        public bool InCombat { get; private set; }
        public EnemyController ActiveEnemy { get; private set; }

        private Coroutine _loop;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CombatSystem>();
        }

        // -- Public API

        public void StartCombat(EnemyController enemy)
        {
            if (InCombat || enemy == null || enemy.IsDead) return;

            ActiveEnemy = enemy;
            InCombat = true;
            GameEvents.RaiseCombatStarted(enemy.Definition);

            _loop = StartCoroutine(CombatLoop());
        }

        public void Retreat()
        {
            if (!InCombat) return;
            StopCombat(victorious: false);
        }

        // -- Private helpers

        private IEnumerator CombatLoop()
        {
            if (!ServiceLocator.TryGet<PlayerController>(out var player))
            {
                StopCombat(victorious: false);
                yield break;
            }

            while (InCombat && !player.CurrentHealth.Equals(0) &&
                   ActiveEnemy != null && !ActiveEnemy.IsDead)
            {
                yield return new WaitForSeconds(turnIntervalSeconds);

                if (!InCombat) yield break;

                // -- Player's turn
                var rawDamage = Mathf.RoundToInt(player.DamageMultiplier * 10f);
                var playerDamage = Mathf.Max(1, rawDamage);
                ActiveEnemy.TakeDamage(playerDamage);

                if (ActiveEnemy.IsDead)
                {
                    var reward = ActiveEnemy.Definition != null
                        ? ActiveEnemy.Definition.RewardSoftCurrency
                        : 0;

                    player.AddCurrency(reward);
                    GameEvents.RaiseEnemyDefeated(ActiveEnemy.Definition, reward);

                    // Track the victory in analytics (fire-and-forget)
                    AnalyticsLite.AnalyticsService.Instance?.TrackEvent("enemy_defeated",
                        new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "enemy_id", ActiveEnemy.Definition?.EnemyId ?? "unknown" },
                            { "reward", reward }
                        });

                    StopCombat(victorious: true);
                    yield break;
                }

                // -- Enemy's turn
                var enemyDamage = ActiveEnemy.Definition != null
                    ? ActiveEnemy.Definition.AttackPower
                    : 5;
                player.TakeDamage(enemyDamage);

                if (player.CurrentHealth <= 0)
                {
                    StopCombat(victorious: false);
                    yield break;
                }
            }

            StopCombat(victorious: !InCombat || ActiveEnemy == null || ActiveEnemy.IsDead);
        }

        private void StopCombat(bool victorious)
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }

            InCombat = false;
            var enemy = ActiveEnemy;
            ActiveEnemy = null;
            GameEvents.RaiseCombatEnded(victorious);
        }
    }
}