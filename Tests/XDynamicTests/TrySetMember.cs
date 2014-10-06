using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlDynamicWrapper;

namespace Tests.XDynamicTests
{
    [TestClass]
    public class TrySetMember
    {
        [TestMethod]
        public void ValueTypeAssign_ActualizeXElementImmediately()
        {
            var xRoot = XElement.Parse("<root />");

            var expectedXString = string.Format(@"<root><Id {0}=""System.Int32"">123</Id></root>",
                XDynamic.DataTypeAttributeName);

            //act
            dynamic xDynamic = new XDynamic(xRoot);
            xDynamic.Id = 123;

            //assert
            Assert.AreEqual(expectedXString, xRoot.ToString(SaveOptions.DisableFormatting));
        }

        [TestMethod]
        public void NullValue_ActualizeRemoveXElementImmediately()
        {
            var xRoot =
                XElement.Parse(string.Format(@"<root><Id {0}=""System.Int32"">123</Id></root>",
                    XDynamic.DataTypeAttributeName));

            const string expectedXString = @"<root />";

            //act
            dynamic xDynamic = new XDynamic(xRoot);
            xDynamic.Id = null;

            //assert
            Assert.AreEqual(expectedXString, xRoot.ToString(SaveOptions.DisableFormatting));
        }

        [TestMethod]
        public void ValueTypeAssign_ActualizeXElementImmediatelyAndRemoveOldInnerXItems()
        {
            var xRoot = XElement.Parse(string.Format(@"<root a=""d""><Id type=""1"" _atr=""3"" {0}=""System.Int32"">123</Id></root>", XDynamic.DataTypeAttributeName));
            var expectedXString = string.Format(@"<root a=""d""><Id {0}=""System.Int64"">456</Id></root>", XDynamic.DataTypeAttributeName);

            //act
            dynamic xDynamic = new XDynamic(xRoot);
            xDynamic.Id = (long)456;

            //assert
            Assert.AreEqual(expectedXString, xRoot.ToString(SaveOptions.DisableFormatting));
        }
    }
}
