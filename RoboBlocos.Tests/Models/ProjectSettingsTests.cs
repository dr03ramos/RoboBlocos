using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBlocos.Models;

namespace RoboBlocos.Tests.Models
{
    [TestClass]
    public class ProjectSettingsTests
    {
        [TestMethod]
        public void ProjectSettings_DefaultState_New()
        {
            var ps = new ProjectSettings();
            Assert.AreEqual(ProjectState.New, ps.State);
        }

        [TestMethod]
        public void RobotSettings_DefaultModel_Rcx2()
        {
            var rs = new RobotSettings();
            Assert.AreEqual("RCX2", rs.Model);
            Assert.AreEqual(FirmwareOption.Recommended, rs.FirmwareOption);
        }

        [TestMethod]
        public void ConnectionSettings_Defaults()
        {
            var cs = new ConnectionSettings();
            Assert.AreEqual("COM1", cs.SerialPort);
            Assert.IsTrue(cs.ConnectionAttempts > 0);
        }

        [TestMethod]
        public void LoggingSettings_Defaults()
        {
            var ls = new LoggingSettings();
            Assert.IsFalse(ls.LogFirmwareDownload);
            Assert.IsTrue(ls.LogInterface);
            Assert.IsTrue(ls.LogMandatory);
        }
    }
}