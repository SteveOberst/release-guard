using System.Collections.Generic;

namespace AttackSurfaceFixture.Game.Buffs
{
    /// <summary>
    /// Pre-registered, IL2CPP-safe factory for item buff effects.
    ///
    /// <b>Why this pattern instead of scanning assemblies?</b>
    ///
    /// The "scan all assemblies for <c>IBuffEffect</c> implementations" approach:
    /// <code>
    /// foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    ///     foreach (var type in asm.GetTypes())
    ///         if (typeof(IBuffEffect).IsAssignableFrom(type)) ...
    /// </code>
    /// works in Mono but silently breaks in IL2CPP builds when managed stripping is
    /// enabled at Medium or High. The linker removes types whose only "reference" is
    /// that runtime scan string, because it cannot trace strings as reachable code.
    ///
    /// <b>This registry avoids the problem entirely:</b> the static constructor
    /// instantiates each concrete effect type directly. The linker traces those
    /// instantiations as hard references, preserves every effect class, and managed
    /// stripping works as intended  -- removing everything <em>else</em> that is
    /// genuinely unused.
    ///
    /// <b>When do you still need <c>[Preserve]</c>?</b>
    /// When a type name comes from <em>outside</em> the build  -- a RemoteConfig value,
    /// a save file, a user-mod manifest  -- and you load it with
    /// <c>TypeBinder.FindType(name)</c>. The linker cannot trace those strings.
    /// Add <c>[Preserve]</c> to every concrete type reachable only by name.
    /// See <c>EventResponderHost</c> and <c>BonusRewardResponder</c> for that pattern.
    /// </summary>
    public static class BuffEffectRegistry
    {
        private static readonly Dictionary<string, IBuffEffect> Effects;

        static BuffEffectRegistry()
        {
            // Direct instantiation: the linker sees HealBuff, DamageBoostBuff and
            // SpeedBoostBuff as reachable  -- no [Preserve] required on any of them.
            Effects = new Dictionary<string, IBuffEffect>
            {
                { "heal",         new HealBuff() },
                { "damage_boost", new DamageBoostBuff() },
                { "speed_boost",  new SpeedBoostBuff() },
            };
        }

        /// <summary>
        /// Looks up an effect by id. Returns false (and sets <paramref name="effect"/> to null)
        /// if the id is unknown. Never throws.
        /// </summary>
        public static bool TryGetEffect(string effectId, out IBuffEffect effect)
            => Effects.TryGetValue(effectId ?? string.Empty, out effect);

        /// <summary>Read-only view of every registered effect keyed by effect id.</summary>
        public static IReadOnlyDictionary<string, IBuffEffect> All => Effects;
    }
}
