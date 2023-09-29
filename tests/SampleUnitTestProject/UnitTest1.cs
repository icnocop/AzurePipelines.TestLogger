using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SampleUnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod()
        {
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void DataTestMethod(int parameter)
        {
        }
    }
}
