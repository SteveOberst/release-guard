using NUnit.Framework;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class AndroidDebuggableAnalyzerTests
    {
        // -- Manifest scanning

        [Test]
        public void Manifest_Flags_DebuggableTrue()
        {
            const string xml =
                "<manifest>\n" +
                "  <application android:debuggable=\"true\" android:label=\"Game\">\n" +
                "  </application>\n" +
                "</manifest>";

            var findings = AndroidDebuggableAnalyzer.ScanManifest(xml);

            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(2, findings[0].line);
        }

        [Test]
        public void Manifest_Flags_SingleQuotes()
        {
            var findings = AndroidDebuggableAnalyzer.ScanManifest(
                "<application android:debuggable='true' />");
            Assert.AreEqual(1, findings.Count);
        }

        [Test]
        public void Manifest_Ignores_DebuggableFalse()
        {
            var findings = AndroidDebuggableAnalyzer.ScanManifest(
                "<application android:debuggable=\"false\" />");
            Assert.IsEmpty(findings);
        }

        [Test]
        public void Manifest_Ignores_CommentedOutDeclaration()
        {
            const string xml =
                "<manifest>\n" +
                "  <!-- <application android:debuggable=\"true\" /> -->\n" +
                "  <application android:debuggable=\"false\" />\n" +
                "</manifest>";

            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanManifest(xml));
        }

        [Test]
        public void Manifest_Ignores_MultiLineComment()
        {
            const string xml =
                "<manifest>\n" +
                "  <!--\n" +
                "  <application android:debuggable=\"true\" />\n" +
                "  -->\n" +
                "</manifest>";

            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanManifest(xml));
        }

        // -- Gradle scanning

        [Test]
        public void Gradle_Flags_DebuggableTrue_StatementForm()
        {
            const string gradle =
                "android {\n" +
                "    buildTypes {\n" +
                "        release {\n" +
                "            debuggable true\n" +
                "        }\n" +
                "    }\n" +
                "}";

            var findings = AndroidDebuggableAnalyzer.ScanGradle(gradle);

            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(4, findings[0].line);
        }

        [Test]
        public void Gradle_Flags_AssignmentAndCallForms()
        {
            Assert.AreEqual(1, AndroidDebuggableAnalyzer.ScanGradle("debuggable = true").Count,
                "Assignment form must be flagged.");
            Assert.AreEqual(1, AndroidDebuggableAnalyzer.ScanGradle("debuggable(true)").Count,
                "Call form must be flagged.");
        }

        [Test]
        public void Gradle_Ignores_DebuggableFalse()
        {
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("debuggable false"));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("debuggable = false"));
        }

        [Test]
        public void Gradle_Ignores_LineComment()
        {
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("// debuggable true"));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("release { } // debuggable true"));
        }

        [Test]
        public void Gradle_Ignores_BlockComment()
        {
            const string gradle =
                "/*\n" +
                "debuggable true\n" +
                "*/\n" +
                "debuggable false";

            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle(gradle));
        }

        [Test]
        public void Gradle_Ignores_SimilarIdentifiers()
        {
            // Words that merely contain "debuggable" must not match.
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("isDebuggable true"));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle("def debuggableHint = trueish"));
        }

        [Test]
        public void EmptyAndNullContent_ProduceNoFindings()
        {
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanManifest(""));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanManifest(null));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle(""));
            Assert.IsEmpty(AndroidDebuggableAnalyzer.ScanGradle(null));
        }
    }
}
