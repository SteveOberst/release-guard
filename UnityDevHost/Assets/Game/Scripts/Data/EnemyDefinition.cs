using UnityEngine;

namespace AttackSurfaceFixture.Game.Data
{
    [CreateAssetMenu(menuName = "Attack Surface Fixture/Data/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string enemyId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int attackPower = 1;
        [SerializeField] private int rewardSoftCurrency;
        [SerializeField] private bool boss;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public int AttackPower => attackPower;
        public int RewardSoftCurrency => rewardSoftCurrency;
        public bool Boss => boss;
    }
}
