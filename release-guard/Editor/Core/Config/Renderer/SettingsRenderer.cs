using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Reader;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable VirtualMemberNeverOverridden.Global
namespace ReleaseGuard.Editor.Core.Config.Renderer
{
    /// <summary>
    ///     Layout values used by <see cref="SettingsRenderer" />. Custom renderers can provide a
    ///     different implementation when they need different spacing without hard-coded literals.
    /// </summary>
    public interface ISettingsRendererLayout
    {
        float PageLeftPadding { get; }
        float PageRightPadding { get; }
        float PageTopSpacing { get; }
        float PageBottomSpacing { get; }
        float SectionTopSpacing { get; }
        float RelatedFieldSpacing { get; }
        float ActionButtonHeight { get; }
        float FieldLabelWidth { get; }
        float LineListDefaultMinLines { get; }
        float LineListHeightPadding { get; }
    }

    /// <summary>Default Release Guard settings layout.</summary>
    // ReSharper disable MemberCanBePrivate.Global
    public sealed class DefaultSettingsRendererLayout : ISettingsRendererLayout
    {
        public const float DefaultPageLeftPadding = 10f;
        public const float DefaultPageRightPadding = 4f;
        public const float DefaultPageTopSpacing = 4f;
        public const float DefaultPageBottomSpacing = 12f;
        public const float DefaultSectionTopSpacing = 14f;
        public const float DefaultRelatedFieldSpacing = 8f;
        public const float DefaultActionButtonHeight = 24f;
        public const float DefaultFieldLabelWidth = 220f;
        public const float DefaultLineListMinLines = 3f;
        public const float DefaultLineListHeightPadding = 6f;

        public static readonly DefaultSettingsRendererLayout Instance = new();

        private DefaultSettingsRendererLayout()
        {
        }

        public float PageLeftPadding => DefaultPageLeftPadding;
        public float PageRightPadding => DefaultPageRightPadding;
        public float PageTopSpacing => DefaultPageTopSpacing;
        public float PageBottomSpacing => DefaultPageBottomSpacing;
        public float SectionTopSpacing => DefaultSectionTopSpacing;
        public float RelatedFieldSpacing => DefaultRelatedFieldSpacing;
        public float ActionButtonHeight => DefaultActionButtonHeight;
        public float FieldLabelWidth => DefaultFieldLabelWidth;
        public float LineListDefaultMinLines => DefaultLineListMinLines;
        public float LineListHeightPadding => DefaultLineListHeightPadding;
    }
    // ReSharper restore MemberCanBePrivate.Global

    /// <summary>
    ///     Canonical IMGUI renderer for Release Guard settings objects. Inherits layout and
    ///     list-field helpers from <see cref="RenderPrimitives" /> and uses a
    ///     <see cref="SettingsComponentReader" /> (via <see cref="SettingsComponentRenderer" />) to
    ///     read and render the component tree for a settings object.
    /// </summary>
    public class SettingsRenderer : RenderPrimitives
    {
        public SettingsRenderer(ISettingsRendererLayout layout = null) : base(layout)
        {
            ComponentRenderer = new SettingsComponentRenderer();
        }

        /// <summary>Shared instance -- used by Release Guard's built-in settings pages.</summary>
        public static SettingsRenderer Default { get; } = new();

        /// <summary>Provides access to the underlying <see cref="SettingsComponentReader" />.</summary>
        public SettingsComponentRenderer ComponentRenderer { get; }

        // ------------------------------------------------------------------
        // Provider tree generation
        // ------------------------------------------------------------------

        /// <summary>
        ///     Generates a <see cref="SettingsProvider" /> for the root overview page and one for each
        ///     child <see cref="ScreenComponent" /> discovered in the component tree of
        ///     <paramref name="settings" />.
        /// </summary>
        public IEnumerable<SettingsProvider> CreateProviders(
            string rootPath,
            ScriptableObject settings)
        {
            var root = ReadRootScreen(settings, rootPath);

            yield return root.CreateProvider(settings, this, GetRootKeywords());

            foreach (var child in root.Children)
                if (child is ScreenComponent screen)
                    foreach (var p in screen.CreateProviders(settings, this))
                        yield return p;

            if (root.DynamicProviderResolver == null) yield break;
            foreach (var p in root.DynamicProviderResolver(settings, this))
                yield return p;
        }

        /// <summary>
        ///     Generates a <see cref="SettingsProvider" /> for the root overview page and one for each
        ///     child <see cref="ScreenComponent" />, using a getter that is called on each draw so
        ///     that profile switches are reflected without re-registering providers.
        /// </summary>
        public IEnumerable<SettingsProvider> CreateProviders(
            string rootPath,
            Func<ScriptableObject> settingsGetter)
        {
            var initial = settingsGetter();
            if (initial == null) yield break;
            var root = ReadRootScreen(initial, rootPath);

            yield return root.CreateProvider(settingsGetter, this, GetRootKeywords());

            foreach (var child in root.Children)
                if (child is ScreenComponent screen)
                    foreach (var p in screen.CreateProviders(settingsGetter, this))
                        yield return p;

            if (root.DynamicProviderResolver == null) yield break;
            var current = settingsGetter();
            if (current != null)
                foreach (var p in root.DynamicProviderResolver(current, this))
                    yield return p;
        }

        /// <summary>
        ///     Reads the component tree for <paramref name="settings" /> and returns the root
        ///     <see cref="ScreenComponent" />. Exposed so callers (e.g. <c>CreateAll</c>) can
        ///     attach a <c>DynamicProviderResolver</c> before generating providers.
        /// </summary>
        internal ScreenComponent ReadRootScreen(ScriptableObject settings, string rootPath)
        {
            var reader = ComponentRenderer.ComponentReader;
            var label = rootPath.Split('/').Last();
            return reader.Read(settings, rootPath, label);
        }

        /// <summary>
        ///     Search keywords for the root overview page. Override to add domain-specific terms.
        /// </summary>
        protected virtual HashSet<string> GetRootKeywords()
        {
            return new HashSet<string>();
        }

        /// <summary>
        ///     Called after settings are modified and applied. Default: reloads the Release Guard
        ///     environment. Override to customize or suppress.
        /// </summary>
        internal virtual void OnSettingsChanged()
        {
            ReleaseGuardStartup.Reload();
        }
    }
}