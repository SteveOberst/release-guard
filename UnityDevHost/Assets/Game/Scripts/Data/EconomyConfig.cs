using UnityEngine;

namespace AttackSurfaceFixture.Game.Data
{
    [CreateAssetMenu(menuName = "Attack Surface Fixture/Data/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField] private int startingSoftCurrency = 100;
        [SerializeField] private int shopRefreshCost = 25;
        [SerializeField] private int reviveCost = 50;
        [SerializeField] private int[] upgradeCosts = { 25, 50, 100 };

        public int StartingSoftCurrency => startingSoftCurrency;
        public int ShopRefreshCost => shopRefreshCost;
        public int ReviveCost => reviveCost;
        public int[] UpgradeCosts => upgradeCosts;
    }
}