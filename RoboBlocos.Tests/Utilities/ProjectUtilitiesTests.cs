using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBlocos.Utilities;
using RoboBlocos.Models;

namespace RoboBlocos.Tests.Utilities
{
    [TestClass]
    public class ProjectUtilitiesTests
    {
        [TestMethod]
        public void CleanFileName_RemovesInvalidChars_AndTruncates()
        {
            var input = "inva<lid>:name?*with|chars";
            var cleaned = ProjectUtilities.CleanFileName(input, defaultName: "Default", maxLength: 10);
            // Invalid chars removed, truncated to length 10.
            Assert.IsFalse(cleaned.Contains('<') || cleaned.Contains('>') || cleaned.Contains(':') || cleaned.Contains('?') || cleaned.Contains('*') || cleaned.Contains('|'));
            Assert.IsTrue(cleaned.Length <= 10);
        }

        [TestMethod]
        public void CleanFileName_Empty_ReturnsDefault()
        {
            var cleaned = ProjectUtilities.CleanFileName("", defaultName: "Default");
            Assert.AreEqual("Default", cleaned);
        }

        [TestMethod]
        public void GetProjectDisplayName_NullProject_ReturnsFallback()
        {
            var name = ProjectUtilities.GetProjectDisplayName(null);
            Assert.AreEqual("Projeto sem nome", name);
        }

        [TestMethod]
        public void GetProjectDisplayName_UsesProjectName()
        {
            var ps = new ProjectSettings { ProjectName = "MeuProjeto" };
            Assert.AreEqual("MeuProjeto", ProjectUtilities.GetProjectDisplayName(ps));
        }

        [TestMethod]
        public void GetProjectDisplayDescription_ReturnsFormattedString()
        {
            var ps = new ProjectSettings
            {
                ProjectName = "Teste",
                RobotSettings = new RobotSettings { Model = "RCX2" },
                ConnectionSettings = new ConnectionSettings { SerialPort = "COM3" }
            };
            var desc = ProjectUtilities.GetProjectDisplayDescription(ps);
            Assert.IsTrue(desc.Contains("Robô RCX2"));
            Assert.IsTrue(desc.Contains("Porta COM3"));
        }

        [TestMethod]
        public void CreateDefaultProject_DefaultsSet()
        {
            var ps = ProjectUtilities.CreateDefaultProject();
            Assert.IsNotNull(ps.ProjectName);
            Assert.IsNotNull(ps.RobotSettings);
            Assert.IsNotNull(ps.ConnectionSettings);
            Assert.IsNotNull(ps.LoggingSettings);
        }
    }
}