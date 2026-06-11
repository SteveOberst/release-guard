using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Util
{
    /// <summary>
    /// Matches Unity asset paths against a list of gitignore-style glob patterns. Used to exclude
    /// specific assets from release issues (see <see cref="ReleaseAuditContext.Report"/>).
    ///
    /// Patterns are compiled once to <see cref="Regex"/> (the idiomatic, dependency-free way to do
    /// glob matching in C#). Supported syntax, modelled on .gitignore:
    /// <list type="bullet">
    /// <item><c>*</c> matches any run of characters except <c>/</c>.</item>
    /// <item><c>**</c> matches any run including <c>/</c> (recursive); <c>**/</c> matches zero or
    /// more directories.</item>
    /// <item><c>?</c> matches a single character except <c>/</c>.</item>
    /// <item>A pattern with no <c>/</c> matches by file/folder name at any depth
    /// (e.g. <c>*.tmp</c>).</item>
    /// <item>A pattern containing <c>/</c> (or a leading <c>/</c>) is anchored to the start of the
    /// asset path (which begins with <c>Assets/</c>).</item>
    /// <item>A trailing <c>/</c> matches a directory and everything under it.</item>
    /// <item>A leading <c>!</c> negates (re-includes) a previously excluded path. The last pattern
    /// that matches wins.</item>
    /// <item>Blank lines and lines beginning with <c>#</c> are ignored.</item>
    /// </list>
    /// Matching is case-insensitive (Unity's primary platforms use case-insensitive filesystems).
    /// </summary>
    public sealed class AssetExclusionMatcher
    {
        private readonly struct Rule
        {
            public readonly Regex regex;
            public readonly bool negate;

            public Rule(Regex regex, bool negate)
            {
                this.regex = regex;
                this.negate = negate;
            }
        }

        private readonly List<Rule> _rules = new();

        public AssetExclusionMatcher(IEnumerable<string> patterns)
        {
            if (patterns == null)
                return;

            foreach (var raw in patterns)
            {
                var rule = TryCompile(raw);
                if (rule.HasValue)
                    _rules.Add(rule.Value);
            }
        }

        public bool HasPatterns => _rules.Count > 0;

        /// <summary>
        /// True if <paramref name="assetPath"/> should be excluded. The last pattern that matches
        /// decides: a normal pattern excludes, a <c>!</c> pattern re-includes.
        /// </summary>
        public bool IsExcluded(string assetPath)
        {
            var path = NormalizePath(assetPath);
            if (path == null || _rules.Count == 0)
                return false;

            var excluded = false;
            foreach (var rule in _rules.Where(rule => rule.regex.IsMatch(path)))
            {
                excluded = !rule.negate;
            }

            return excluded;
        }

        /// <summary>Trim, convert back-slashes to forward-slashes. Returns null for empty input.</summary>
        public static string NormalizePath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath) ? null : assetPath.Trim().Replace('\\', '/');
        }

        private static Rule? TryCompile(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var pattern = raw.Trim();
            if (pattern.StartsWith("#"))
                return null; // comment

            var negate = pattern.StartsWith("!");
            if (negate)
                pattern = pattern[1..].Trim();

            pattern = pattern.Replace('\\', '/');
            if (pattern.Length == 0)
                return null;

            var isDirectory = pattern.EndsWith("/");
            if (isDirectory)
                pattern = pattern.TrimEnd('/');

            // Anchored if it contains a slash anywhere (after trimming a trailing one) or starts
            // with one; otherwise it is a bare name matched at any depth.
            var anchored = pattern.StartsWith("/") || pattern.Contains("/");
            if (pattern.StartsWith("/"))
                pattern = pattern[1..];

            var body = GlobToRegex(pattern);
            var sb = new StringBuilder();
            sb.Append(anchored ? "^" : "(^|/)");
            sb.Append(body);
            sb.Append(isDirectory ? "(/.*)?$" : "$");

            return new Rule(
                new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                negate);
        }

        private static string GlobToRegex(string glob)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < glob.Length; i++)
            {
                var c = glob[i];
                switch (c)
                {
                    case '*':
                    {
                        var doubleStar = i + 1 < glob.Length && glob[i + 1] == '*';
                        if (doubleStar)
                        {
                            i++; // consume the second '*'
                            if (i + 1 < glob.Length && glob[i + 1] == '/')
                            {
                                i++; // consume the '/'
                                sb.Append("(?:.*/)?"); // "**/" -> zero or more directories
                            }
                            else
                            {
                                sb.Append(".*"); // "**" -> anything, including separators
                            }
                        }
                        else
                        {
                            sb.Append("[^/]*"); // "*" -> a run of non-separator characters
                        }

                        break;
                    }
                    case '?':
                        sb.Append("[^/]");
                        break;
                    case '/':
                        sb.Append('/');
                        break;
                    default:
                        sb.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            return sb.ToString();
        }
    }
}