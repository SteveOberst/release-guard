using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Simple three-state FSM for a single enemy: Idle → Chasing → Attacking.
    ///
    /// <b>IL2CPP note on FSM design:</b> Enum-based FSMs are among the most AOT-friendly
    /// patterns. The <c>switch</c> statement compiles to a jump table or flat conditional
    /// chain  -- no virtual dispatch, no delegate allocation, no reflection. Function pointer
    /// tables (used by delegate-based state machines) are slightly more flexible but
    /// require the AOT compiler to generate stubs for every possible callback signature,
    /// which can bloat binary size under aggressive optimisation. For enemy AI at scale
    /// the enum FSM is the pragmatic choice.
    /// </summary>
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private float detectionRadius = 6f;
        [SerializeField] private float attackRadius = 1.8f;
        [SerializeField] private float moveSpeed = 2.5f;

        public EnemyDefinition Definition => definition;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private EnemyState _state = EnemyState.Idle;
        private Transform _playerTransform;
        private float _stateTimer;

        private enum EnemyState
        {
            Idle,
            Chasing,
            Attacking
        }

        // -- Unity lifecycle

        private void Start()
        {
            CurrentHealth = definition != null ? definition.MaxHealth : 20;

            if (ServiceLocator.TryGet<PlayerController>(out var player))
                _playerTransform = player.transform;
        }

        private void Update()
        {
            if (IsDead) return;

            _stateTimer += Time.deltaTime;

            switch (_state)
            {
                case EnemyState.Idle: TickIdle(); break;
                case EnemyState.Chasing: TickChasing(); break;
                case EnemyState.Attacking: TickAttacking(); break;
            }
        }

        // -- Public API

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
            if (IsDead)
            {
                _state = EnemyState.Idle;
                gameObject.SetActive(false);
            }
        }

        /// <summary>Swaps the enemy's data definition and resets health to the new maximum.</summary>
        public void SetDefinition(EnemyDefinition def)
        {
            definition = def;
            CurrentHealth = def != null ? def.MaxHealth : 20;
            _state = EnemyState.Idle;
        }

        // -- State ticks

        private void TickIdle()
        {
            if (_playerTransform == null) return;
            if (DistToPlayer() <= detectionRadius)
                TransitionTo(EnemyState.Chasing);
        }

        private void TickChasing()
        {
            if (_playerTransform == null)
            {
                TransitionTo(EnemyState.Idle);
                return;
            }

            var dist = DistToPlayer();

            if (dist > detectionRadius * 1.5f)
            {
                TransitionTo(EnemyState.Idle);
                return;
            }

            if (dist <= attackRadius)
            {
                TransitionTo(EnemyState.Attacking);
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position, _playerTransform.position, moveSpeed * Time.deltaTime);
        }

        private void TickAttacking()
        {
            // The actual damage is applied by CombatSystem on a turn interval  --
            // the enemy just stays in Attacking state until the player moves away.
            if (_playerTransform == null || DistToPlayer() > attackRadius)
                TransitionTo(EnemyState.Chasing);
        }

        private void TransitionTo(EnemyState next)
        {
            _state = next;
            _stateTimer = 0f;
        }

        private float DistToPlayer() =>
            _playerTransform != null
                ? Vector3.Distance(transform.position, _playerTransform.position)
                : float.MaxValue;
    }
}