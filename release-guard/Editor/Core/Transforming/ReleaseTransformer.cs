using ReleaseGuard.Editor.Core.Registries;

namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>
    /// Base class for a build artifact transformer. Derive from this to perform operations
    /// that modify the build artifacts at a low level -- IL manipulation, binary patching,
    /// code obfuscation, native-library processing, and similar. Transformers run after the
    /// build completes and before post-processors, so their output is what the post-processor
    /// pipeline (cleanup, manifest writing, etc.) operates on.
    ///
    /// <para>No built-in transformers are shipped with Release Guard -- this is the base type
    /// for advanced, project-specific build hardening beyond the auditor checks.</para>
    ///
    /// <para>Quick start:</para>
    /// <code>
    /// // In any Editor-platform assembly.
    /// public sealed class MyTransformer : ReleaseTransformer
    /// {
    ///     public override string Id => "myteam.my_transformer";
    ///
    ///     public override void Transform(ReleaseTransformContext context)
    ///     {
    ///         context.Info($"Transforming {context.OutputPath}");
    ///         // Modify assemblies, patch binaries, etc.
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Key contract:</b> transformers run via <c>IPostprocessBuildWithReport</c>
    /// AFTER the build succeeds and BEFORE post-processors. Exceptions are caught so one
    /// bad transformer never silently prevents others from running.</para>
    ///
    /// <para><b>Safe defaults:</b> implementations should be non-destructive by default.
    /// Any irreversible modification should be opt-in via settings.</para>
    /// </summary>
    public abstract class ReleaseTransformer : IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and de-duplication.</summary>
        public abstract string Id { get; }

        /// <summary>Human-friendly name shown in the Release Guard window. Defaults to the type name.</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Execution order. Lower values run first. Use a negative value to run before any
        /// default-priority transformers, or a positive value to run after them.
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Optional gate. Return false to skip this transformer for the current run (e.g.
        /// platform-specific transforms, or a transform controlled by a settings flag).
        /// </summary>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        // ReSharper disable once UnusedParameter.Global
        public virtual bool ShouldRun(ReleaseTransformContext context) => true;

        /// <summary>
        /// Perform the transformation. Use <c>context.Info</c>, <c>context.Warning</c>, and
        /// <c>context.Error</c> to record what the transformer did. Do not throw -- exceptions
        /// are caught by the executor and turned into warnings.
        /// </summary>
        // ReSharper disable once UnusedParameter.Global
        public abstract void Transform(ReleaseTransformContext context);
    }
}