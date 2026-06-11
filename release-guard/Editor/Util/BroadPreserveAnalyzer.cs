using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace ReleaseGuard.Editor.Util
{
    internal readonly struct BroadPreserveFinding
    {
        public BroadPreserveFinding(string message, string assetPath)
        {
            Message = message;
            AssetPath = assetPath;
        }

        public string Message { get; }
        public string AssetPath { get; }
    }

    internal static class BroadPreserveAnalyzer
    {
        private static readonly string PreserveAttributeFullName = typeof(PreserveAttribute).FullName;

        public static List<BroadPreserveFinding> AnalyzeProject()
        {
            // Only assemblies that actually ship in the player can defeat runtime stripping. The
            // editor has many more assemblies loaded (editor tooling, packages, tests) that never
            // ship; scanning those would raise false positives that are not even suppressible via
            // the asset-exclusion list, since assembly-level findings carry no asset path.
            var shippingAssemblies = GetShippingAssemblyNames();

            var findings = (
                    from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    where shippingAssemblies.Contains(assembly.GetName().Name)
                    where HasAssemblyWidePreserve(assembly)
                    select new BroadPreserveFinding(
                        $"Assembly '{assembly.GetName().Name}' is annotated with [Preserve], which keeps the entire assembly reachable for stripping.",
                        assetPath: null))
                .ToList();

            var assetsRoot = Application.dataPath;
            if (!Directory.Exists(assetsRoot))
                return findings;

            foreach (var path in Directory.EnumerateFiles(assetsRoot, "link.xml", SearchOption.AllDirectories))
                findings.AddRange(AnalyzeLinkXmlFile(path));

            return findings;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static List<BroadPreserveFinding> AnalyzeLinkXmlFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<BroadPreserveFinding>();

            var assetPath = ToAssetPath(absolutePath);
            var xml = File.ReadAllText(absolutePath);
            return AnalyzeLinkXmlContents(xml, assetPath);
        }

        public static List<BroadPreserveFinding> AnalyzeLinkXmlContents(string xml, string assetPath)
        {
            var findings = new List<BroadPreserveFinding>();
            if (string.IsNullOrWhiteSpace(xml))
                return findings;

            XDocument document;
            try
            {
                document = XDocument.Parse(xml, LoadOptions.None);
            }
            catch
            {
                return findings;
            }

            foreach (var assembly in document.Descendants("assembly"))
            {
                var assemblyName = (string)assembly.Attribute("fullname") ??
                                   (string)assembly.Attribute("name") ??
                                   "<unknown assembly>";
                var preserve = (string)assembly.Attribute("preserve");

                if (string.Equals(preserve, "all", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new BroadPreserveFinding(
                        $"link.xml preserves the entire assembly '{assemblyName}'.",
                        assetPath));
                    continue;
                }

                if (!assembly.Elements().Any() && string.IsNullOrEmpty(preserve))
                {
                    findings.Add(new BroadPreserveFinding(
                        $"link.xml preserves the entire assembly '{assemblyName}' with an empty assembly rule.",
                        assetPath));
                }
            }

            findings.AddRange(
                from type in document.Descendants("type")
                select (string)type.Attribute("fullname") ?? (string)type.Attribute("name")
                into fullName
                where !string.IsNullOrEmpty(fullName) && fullName.Contains("*")
                select new BroadPreserveFinding($"link.xml preserves a wildcard type pattern '{fullName}'.",
                    assetPath));

            return findings;
        }

        private static HashSet<string> GetShippingAssemblyNames()
        {
            var names = new HashSet<string>();
            try
            {
                // Fully qualified so this file's System.Reflection.Assembly usage stays unambiguous
                // (UnityEditor.Compilation also defines an 'Assembly' type).
                var playerAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies(
                    UnityEditor.Compilation.AssembliesType.Player);
                foreach (var assembly in playerAssemblies)
                    names.Add(assembly.name);
            }
            catch
            {
                // CompilationPipeline can be unavailable in some edge contexts; fall back to no
                // assembly-level findings rather than scanning every editor assembly.
            }

            return names;
        }

        private static bool HasAssemblyWidePreserve(Assembly assembly)
        {
            try
            {
                return assembly
                    .GetCustomAttributesData()
                    .Any(attribute => attribute.AttributeType.FullName == PreserveAttributeFullName);
            }
            catch
            {
                return false;
            }
        }

        private static string ToAssetPath(string absolutePath)
        {
            var normalizedPath = absolutePath.Replace('\\', '/');
            var normalizedAssetsRoot = Application.dataPath.Replace('\\', '/');

            if (!normalizedPath.StartsWith(normalizedAssetsRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            return "Assets" + normalizedPath[normalizedAssetsRoot.Length..];
        }
    }
}