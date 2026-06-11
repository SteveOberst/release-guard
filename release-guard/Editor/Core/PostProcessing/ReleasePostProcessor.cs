using ReleaseGuard.Editor.Core.Registries;

namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>
    /// Base class for a single post-build output processor. Derive from this in any Editor
    /// assembly to hook into the post-processor pipeline -- no registration needed.
    ///
    /// <para>Post-processors operate on the completed build output: cleaning up debug
    /// artifacts, writing CI metadata, patching manifests, and similar output-folder
    /// operations. They run after <see cref="ReleaseTransformer"/> instances so they
    /// always see the final, transformed state of the build folder.</para>
    ///
    /// <para>Quick start:</para>
    /// <code>
    /// // In any Editor-platform assembly.
    /// public sealed class MyPostProcessor : ReleasePostProcessor
    /// {
    ///     public override string Id => "myteam.my_postprocessor";
    ///
    ///     public override void PostProcess(ReleasePostProcessContext context)
    ///     {
    ///         context.Info($"Post-processing {context.OutputPath}");
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Key contract:</b> post-processors run via <c>IPostprocessBuildWithReport</c>
    /// AFTER transformers and AFTER the build succeeds. Exceptions are caught and logged
    /// so one bad post-processor never silently prevents others from running.</para>
    ///
    /// <para><b>Safe defaults:</b> implementations should be non-destructive by default.
    /// Any modification of build output should be opt-in via settings.</para>
    /// </summary>
    public abstract class ReleasePostProcessor : IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and de-duplication.</summary>
        public abstract string Id { get; }

        /// <summary>Human-friendly name shown in the Release Guard window. Defaults to the type name.</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Execution order. Lower values run first. Use a negative value to run before built-ins
        /// (all of which default to 0), or a positive value to run after them.
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Optional gate. Return false to skip this post-processor for the current run (e.g.
        /// platform-specific operations, or behavior gated on a settings flag).
        /// </summary>
        public virtual bool ShouldRun(ReleasePostProcessContext context) => true;

        /// <summary>
        /// Perform the post-processing operation. Use <c>context.Info</c>, <c>context.Warning</c>,
        /// and <c>context.Error</c> to record what the post-processor did. Do not throw -- exceptions
        /// are caught by the executor and turned into warnings.
        /// </summary>
        public abstract void PostProcess(ReleasePostProcessContext context);
    }
}