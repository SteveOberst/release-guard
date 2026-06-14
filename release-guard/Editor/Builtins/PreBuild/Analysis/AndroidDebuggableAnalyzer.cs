using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Pure string analysis for the <c>android_debuggable</c> component: detects explicit
    /// <c>debuggable = true</c> declarations in Android manifest and Gradle template files.
    ///
    /// <para>Detection is deliberately narrow to avoid false positives:</para>
    /// <list type="bullet">
    /// <item>Manifest: only the literal attribute <c>android:debuggable="true"</c>
    /// (single or double quotes), outside XML comments.</item>
    /// <item>Gradle: only a <c>debuggable</c> statement assigning <c>true</c>
    /// (<c>debuggable true</c>, <c>debuggable = true</c>, or <c>debuggable(true)</c>),
    /// outside line and block comments.</item>
    /// </list>
    ///
    /// Commented-out occurrences are never flagged.
    /// </summary>
    internal static class AndroidDebuggableAnalyzer
    {
        private static readonly Regex ManifestDebuggable = new(
            @"android\s*:\s*debuggable\s*=\s*[""']true[""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex GradleDebuggable = new(
            @"(^|[\s{;])debuggable\s*(=\s*true\b|\(\s*true\s*\)|\s+true\b)",
            RegexOptions.Compiled);

        /// <summary>A debuggable=true finding: 1-based line number plus the offending line.</summary>
        internal readonly struct Finding
        {
            public readonly int line;
            public readonly string lineContent;

            public Finding(int line, string lineContent)
            {
                this.line = line;
                this.lineContent = lineContent;
            }
        }

        /// <summary>Scan AndroidManifest.xml content. XML comment blocks are ignored.</summary>
        internal static List<Finding> ScanManifest(string content) =>
            Scan(content, ManifestDebuggable, StripXmlComments(content));

        /// <summary>Scan Gradle file content. Line (//) and block (/* */) comments are ignored.</summary>
        internal static List<Finding> ScanGradle(string content) =>
            Scan(content, GradleDebuggable, StripGradleComments(content));

        // -----------------------------------------------------------------
        // Implementation
        // -----------------------------------------------------------------

        private static List<Finding> Scan(string original, Regex pattern, string commentStripped)
        {
            var findings = new List<Finding>();
            if (string.IsNullOrEmpty(original))
                return findings;

            // Comment stripping preserves line structure (comment chars are blanked, newlines
            // kept), so line numbers in the stripped text map 1:1 to the original file.
            var strippedLines = commentStripped.Split('\n');
            var originalLines = original.Split('\n');

            for (var i = 0; i < strippedLines.Length; i++)
            {
                if (!pattern.IsMatch(strippedLines[i]))
                    continue;

                var originalLine = i < originalLines.Length ? originalLines[i] : strippedLines[i];
                findings.Add(new Finding(i + 1, originalLine.TrimEnd('\r').Trim()));
            }

            return findings;
        }

        /// <summary>Blank out &lt;!-- ... --&gt; ranges while keeping newlines so line numbers hold.</summary>
        private static string StripXmlComments(string content) =>
            BlankRanges(content, "<!--", "-->");

        /// <summary>Blank out /* ... */ blocks and // line tails while keeping newlines.</summary>
        private static string StripGradleComments(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            var withoutBlocks = BlankRanges(content, "/*", "*/");

            var lines = withoutBlocks.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var index = lines[i].IndexOf("//", System.StringComparison.Ordinal);
                if (index >= 0)
                    lines[i] = lines[i][..index];
            }

            return string.Join("\n", lines);
        }

        private static string BlankRanges(string content, string open, string close)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var chars = content.ToCharArray();
            var searchFrom = 0;

            while (true)
            {
                var start = content.IndexOf(open, searchFrom, System.StringComparison.Ordinal);
                if (start < 0)
                    break;

                var end = content.IndexOf(close, start + open.Length, System.StringComparison.Ordinal);
                // Unterminated comment: blank to end of file.
                var blankUntil = end < 0 ? content.Length : end + close.Length;

                for (var i = start; i < blankUntil; i++)
                {
                    if (chars[i] != '\n' && chars[i] != '\r')
                        chars[i] = ' ';
                }

                if (end < 0)
                    break;

                searchFrom = blankUntil;
            }

            return new string(chars);
        }
    }
}
