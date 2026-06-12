namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Implemented by classes that react to in-game events and are loaded
    /// by type name from server configuration.
    ///
    /// Concrete implementations must be decorated with <c>[Preserve]</c> from
    /// <c>UnityEngine.Scripting</c> because they are discovered at runtime via
    /// <c>TypeBinder.FindType</c>  -- the linker cannot trace those string references.
    /// See <see cref="EventResponderHost"/> for the full explanation.
    /// </summary>
    public interface IEventResponder
    {
        /// <summary>Subscribes to relevant <see cref="Core.GameEvents"/> events.</summary>
        void Activate();

        /// <summary>Unsubscribes and cleans up.</summary>
        void Deactivate();
    }
}