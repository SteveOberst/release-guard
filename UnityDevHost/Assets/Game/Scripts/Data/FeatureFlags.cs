using UnityEngine;

namespace AttackSurfaceFixture.Game.Data
{
    [CreateAssetMenu(menuName = "Attack Surface Fixture/Data/Feature Flags", fileName = "FeatureFlags")]
    public sealed class FeatureFlags : ScriptableObject
    {
        [SerializeField] private bool shopEnabled = true;
        [SerializeField] private bool eventBannerEnabled = true;
        [SerializeField] private bool debugPanelEnabled;
        [SerializeField] private bool premiumOffersEnabled = true;

        public bool ShopEnabled => shopEnabled;
        public bool EventBannerEnabled => eventBannerEnabled;
        public bool DebugPanelEnabled => debugPanelEnabled;
        public bool PremiumOffersEnabled => premiumOffersEnabled;
    }
}