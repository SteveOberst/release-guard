using ReleaseGuard.Editor.Core.Registries;

namespace ReleaseGuard.Editor.Core.Audit
{
    /// <summary>
    /// Base class for a single release check. Derive from this in an Editor assembly and
    /// register it through a plugin, or enable auditor auto-discovery in settings.
    ///
    /// <para><b>Quick start:</b></para>
    /// <code>
    /// // In any Editor-platform assembly (Editor folder, or asmdef with Editor include).
    /// public sealed class MyAuditor : ReleaseAuditor
    /// {
    ///     public override string Id          => "my_auditor";       // stable snake_case id
    ///     public override string DisplayName => "My custom check";  // shown in the window
    ///
    ///     public override void Evaluate(ReleaseAuditContext context)
    ///     {
    ///         if (somethingIsWrong)
    ///             context.Error("What is wrong.", fixHint: "How to fix it.");
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Platform filtering:</b> override <see cref="ShouldRun"/> and call
    /// <c>context.IsForPlatform(BuildTarget.Android)</c> to restrict to a platform.</para>
    ///
    /// <para><b>Per-auditor configuration:</b> load a ScriptableObject from a known asset path
    /// inside <see cref="Evaluate"/> via <c>AssetDatabase.LoadAssetAtPath&lt;T&gt;(path)</c>.
    /// Use <c>Assets/ReleaseGuard/</c> as a convention - that folder already holds the settings.</para>
    ///
    /// <para><b>Multiple auditors from one entry point</b> (e.g. one per entry in a config list):
    /// create a <see cref="ReleaseGuardPlugin"/> subclass and call
    /// <c>context.ReleaseGuard.Registries.Auditors.Register(...)</c> from its
    /// <c>Register()</c> method.</para>
    ///
    /// <para><b>Assembly note:</b> <c>ReleaseGuard.Editor</c> is <c>autoReferenced: true</c>, so
    /// every Editor assembly in the project can see <see cref="ReleaseAuditor"/>. Add an explicit
    /// asmdef reference when using an <c>[InitializeOnLoad]</c> plugin loader so Unity initializes
    /// Release Guard first.</para>
    /// </summary>
    public abstract class ReleaseAuditor : IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and de-duplication.</summary>
        public abstract string Id { get; }

        /// <summary>Human-friendly name shown in the audit window. Defaults to the type name.</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Execution order. Lower values run first. Use a negative value to run before built-ins
        /// (all of which default to 0), or a positive value to run after them.
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Optional gate. Return false to skip this auditor for the current run (e.g. a check
        /// that is only relevant for a certain platform or is toggled off in settings). Use
        /// <see cref="ReleaseAuditContext.IsForPlatform"/> to filter by build target.
        /// </summary>
        public virtual bool ShouldRun(ReleaseAuditContext context) => true;

        /// <summary>Run the check, reporting findings via <paramref name="context"/>.</summary>
        public abstract void Evaluate(ReleaseAuditContext context);
    }
}