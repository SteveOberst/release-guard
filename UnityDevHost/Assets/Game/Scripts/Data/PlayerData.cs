using System;
using System.Collections.Generic;

namespace AttackSurfaceFixture.Game.Data
{
    /// <summary>
    /// Serializable player-state snapshot persisted via <c>JsonUtility</c>.
    ///
    /// <b>IL2CPP serialization note:</b> <c>JsonUtility.ToJson / FromJson</c> is Unity's
    /// AOT-safe serializer. It generates C++ serialization stubs at IL2CPP build time rather
    /// than calling <c>System.Reflection</c> at runtime. This means:
    /// <list type="bullet">
    ///   <item>No <c>[Preserve]</c> attributes are needed on this class or its fields  --
    ///   the serializer accesses them through the generated stubs, not via
    ///   <c>FieldInfo.GetValue</c>.</item>
    ///   <item>Only <c>[Serializable]</c> value types and Unity types are supported.</item>
    ///   <item><c>Dictionary&lt;K,V&gt;</c> is <b>not</b> serializable; use
    ///   <c>List&lt;T&gt;</c> plus an index mapping instead.</item>
    ///   <item>Polymorphism (base-class references) is not supported  -- all fields must be
    ///   concrete serializable types.</item>
    /// </list>
    ///
    /// For production builds: wrap the JSON in a checksum envelope and store in an
    /// encrypted file (e.g. via <c>AES-GCM</c> with a key derived from a device identifier)
    /// to resist trivial save-file editing. The serialization pattern here remains the same.
    /// </summary>
    [Serializable]
    public sealed class PlayerData
    {
        public int   softCurrency          = 100;
        public int   hardCurrency          = 0;
        public int   currentHealth         = 100;
        public int   maxHealth             = 100;
        public int   totalKills            = 0;
        public int   highestLevelReached   = 1;
        public float totalPlaytimeSeconds  = 0f;

        /// <summary>Item IDs of consumables and equipment in the player's bag.</summary>
        public List<string> ownedItemIds = new List<string>();

        /// <summary>Five-step tutorial progression; index corresponds to tutorial step index.</summary>
        public bool[] completedTutorialSteps = new bool[5];

        /// <summary>Returns a freshly initialised data set for a brand-new player.</summary>
        public static PlayerData CreateDefault() => new PlayerData
        {
            softCurrency         = 100,
            hardCurrency         = 0,
            currentHealth        = 100,
            maxHealth            = 100,
            ownedItemIds         = new List<string>(),
            completedTutorialSteps = new bool[5]
        };
    }
}
