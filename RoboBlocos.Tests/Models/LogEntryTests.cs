using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBlocos.Models;

namespace RoboBlocos.Tests.Models
{
    [TestClass]
    public class LogEntryTests
    {
        [TestMethod]
        public void LogEntry_Constructor_SetsProperties()
        {
            var entry = new LogEntry("Mensagem", LogSeverity.Success, LogCategory.Interface);
            Assert.AreEqual("Mensagem", entry.Message);
            Assert.AreEqual(LogSeverity.Success, entry.Severity);
            Assert.AreEqual(LogCategory.Interface, entry.Category);
            Assert.IsTrue(entry.Timestamp <= System.DateTime.Now);
        }
    }
}