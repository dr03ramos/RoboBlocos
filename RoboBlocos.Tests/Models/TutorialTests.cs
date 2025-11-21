using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBlocos.Models;

namespace RoboBlocos.Tests.Models
{
    [TestClass]
    public class TutorialTests
    {
        [TestMethod]
        public void Tutorial_Defaults()
        {
            var t = new Tutorial();
            Assert.IsFalse(string.IsNullOrEmpty(t.Id));
            Assert.IsNotNull(t.Steps);
            Assert.AreEqual(0, t.Steps.Count);
        }

        [TestMethod]
        public void TutorialStep_Defaults()
        {
            var step = new TutorialStep();
            Assert.AreEqual(0, step.Order);
            Assert.AreEqual(string.Empty, step.Title);
            Assert.AreEqual(string.Empty, step.Content);
            Assert.IsNull(step.ImagePath);
            Assert.IsNull(step.CodeSnippet);
        }
    }
}