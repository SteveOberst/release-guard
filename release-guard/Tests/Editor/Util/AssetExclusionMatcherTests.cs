using NUnit.Framework;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class AssetExclusionMatcherTests
    {
        private static AssetExclusionMatcher Match(params string[] patterns) =>
            new AssetExclusionMatcher(patterns);

        [Test]
        public void Empty_ExcludesNothing()
        {
            var m = Match();
            Assert.IsFalse(m.HasPatterns);
            Assert.IsFalse(m.IsExcluded("Assets/Foo/Bar.cs"));
        }

        [Test]
        public void Null_Or_Empty_Path_Is_Never_Excluded()
        {
            var m = Match("**");
            Assert.IsFalse(m.IsExcluded(null));
            Assert.IsFalse(m.IsExcluded(""));
            Assert.IsFalse(m.IsExcluded("   "));
        }

        // --- Basename patterns (no slash) match at any depth ---

        [Test]
        public void Basename_Star_Extension_MatchesAtAnyDepth()
        {
            var m = Match("*.tmp");
            Assert.IsTrue(m.IsExcluded("Assets/a.tmp"));
            Assert.IsTrue(m.IsExcluded("Assets/Deep/Nested/a.tmp"));
            Assert.IsFalse(m.IsExcluded("Assets/a.cs"));
        }

        [Test]
        public void Basename_CompoundSuffix()
        {
            var m = Match("*.generated.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Scripts/Foo.generated.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/Scripts/Foo.cs"));
        }

        [Test]
        public void Basename_ExactName()
        {
            var m = Match("Foo.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Foo.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/Sub/Foo.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/FooBar.cs"));
        }

        // --- Anchored patterns (contain a slash) ---

        [Test]
        public void Anchored_Prefix_DoubleStar()
        {
            var m = Match("Assets/ThirdParty/**");
            Assert.IsTrue(m.IsExcluded("Assets/ThirdParty/Plugin/X.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/ThirdParty/readme.txt"));
            Assert.IsFalse(m.IsExcluded("Assets/Game/X.cs"));
        }

        [Test]
        public void Anchored_Is_Not_Matched_Mid_Path()
        {
            // Anchored to the start: 'ThirdParty/**' should not match under Assets/.
            var m = Match("ThirdParty/**");
            Assert.IsFalse(m.IsExcluded("Assets/ThirdParty/X.cs"));
            Assert.IsTrue(m.IsExcluded("ThirdParty/X.cs"));
        }

        [Test]
        public void LeadingSlash_Anchors_And_Is_Stripped()
        {
            var m = Match("/Assets/Editor/Tools.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Editor/Tools.cs"));
            Assert.IsFalse(m.IsExcluded("Other/Assets/Editor/Tools.cs"));
        }

        // --- Single star does not cross directory separators ---

        [Test]
        public void SingleStar_DoesNotCrossSlash()
        {
            var m = Match("Assets/*/X.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Sub/X.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/Sub/Deeper/X.cs"));
        }

        [Test]
        public void DoubleStar_CrossesSlash()
        {
            var m = Match("Assets/**/X.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Sub/X.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/Sub/Deeper/X.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/X.cs")); // **/ matches zero directories
        }

        [Test]
        public void QuestionMark_MatchesSingleChar()
        {
            var m = Match("Assets/File?.cs");
            Assert.IsTrue(m.IsExcluded("Assets/File1.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/File10.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/File/.cs")); // ? does not match '/'
        }

        // --- Directory patterns (trailing slash) ---

        [Test]
        public void TrailingSlash_MatchesDirectoryAndContents()
        {
            var m = Match("Assets/Samples/");
            Assert.IsTrue(m.IsExcluded("Assets/Samples")); // the folder asset itself
            Assert.IsTrue(m.IsExcluded("Assets/Samples/Demo/Y.cs")); // and everything under it
            Assert.IsFalse(m.IsExcluded("Assets/SamplesOther/Y.cs"));
        }

        [Test]
        public void BasenameDirectory_MatchesAtAnyDepth()
        {
            var m = Match("Editor/");
            Assert.IsTrue(m.IsExcluded("Assets/Tools/Editor"));
            Assert.IsTrue(m.IsExcluded("Assets/Tools/Editor/Thing.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/Tools/EditorWindow.cs"));
        }

        // --- Negation / last-match-wins ---

        [Test]
        public void Negation_ReincludesPreviouslyExcluded()
        {
            var m = Match("Assets/Debug/**", "!Assets/Debug/KeepMe.cs");
            Assert.IsTrue(m.IsExcluded("Assets/Debug/Other.cs"));
            Assert.IsFalse(m.IsExcluded("Assets/Debug/KeepMe.cs"));
        }

        [Test]
        public void LastMatchWins_ReexcludeAfterNegation()
        {
            var m = Match("Assets/Debug/**", "!Assets/Debug/**", "Assets/Debug/Secret.cs");
            Assert.IsFalse(m.IsExcluded("Assets/Debug/Other.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/Debug/Secret.cs"));
        }

        // --- Comments / blanks / normalization ---

        [Test]
        public void CommentsAndBlankLines_AreIgnored()
        {
            var m = Match("# a comment", "", "   ", "*.tmp");
            Assert.IsTrue(m.IsExcluded("Assets/x.tmp"));
            Assert.IsFalse(m.IsExcluded("Assets/x.cs"));
        }

        [Test]
        public void Backslashes_AreNormalized()
        {
            var m = Match("Assets\\ThirdParty\\**");
            Assert.IsTrue(m.IsExcluded("Assets\\ThirdParty\\X.cs"));
            Assert.IsTrue(m.IsExcluded("Assets/ThirdParty/X.cs"));
        }

        [Test]
        public void Matching_IsCaseInsensitive()
        {
            var m = Match("Assets/ThirdParty/**");
            Assert.IsTrue(m.IsExcluded("assets/thirdparty/x.cs"));
        }
    }
}