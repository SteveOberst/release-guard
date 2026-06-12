using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BroadPreserveAnalyzerTests
    {
        [Test]
        public void Detects_AssemblyLevel_AllPreserve_InLinkXml()
        {
            const string xml = @"
<linker>
  <assembly fullname=""Game.Runtime"" preserve=""all"" />
</linker>";

            var findings = BroadPreserveAnalyzer.AnalyzeLinkXmlContents(xml, "Assets/link.xml");

            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("Game.Runtime", findings[0].Message);
        }

        [Test]
        public void Detects_Implicit_FullAssemblyPreserve_InLinkXml()
        {
            const string xml = @"
<linker>
  <assembly fullname=""Game.Runtime"" />
</linker>";

            var findings = BroadPreserveAnalyzer.AnalyzeLinkXmlContents(xml, "Assets/link.xml");

            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("preserves the entire assembly", findings[0].Message);
        }

        [Test]
        public void DoesNotFlag_Targeted_LinkXmlRules()
        {
            const string xml = @"
<linker>
  <assembly fullname=""Game.Runtime"" preserve=""nothing"">
    <type fullname=""Game.Runtime.PlayerController"" preserve=""all"" />
  </assembly>
</linker>";

            var findings = BroadPreserveAnalyzer.AnalyzeLinkXmlContents(xml, "Assets/link.xml");

            Assert.AreEqual(0, findings.Count);
        }

        [Test]
        public void Detects_Wildcard_TypePreserve_InLinkXml()
        {
            const string xml = @"
<linker>
  <assembly fullname=""Game.Runtime"" preserve=""nothing"">
    <type fullname=""Game.Runtime.Debug*"" preserve=""all"" />
  </assembly>
</linker>";

            var findings = BroadPreserveAnalyzer.AnalyzeLinkXmlContents(xml, "Assets/link.xml");

            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("Game.Runtime.Debug*", findings.Single().Message);
        }
    }
}