using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlDynamicWrapper;

namespace Tests.XDynamicTests.TrySetMember
{
    [TestClass]
    public class Object
    {
        [TestMethod]
        public void ElementXDynamicAddedNewProperty_XElementChanged()
        {
            const string xStr =
@"<Root>
	<Id>1202</Id>
	<Name>name value</Name>
	<Description>description value</Description>	
</Root>";

            var resultXStr = string.Format(
@"<Root>
	<Id>1202</Id>
	<Name>name value</Name>
	<Description>description value</Description>	
    <NewPropertyInt {0}=""System.Int32"">1234</NewPropertyInt>
    <NewPropertyFloat {0}=""System.Single"">0,0546</NewPropertyFloat>
</Root>", XDynamic.DataTypeAttributeName);

            var xResult = XElement.Parse(resultXStr);

            var xElement = XElement.Parse(xStr);

            //act
            dynamic dXml = new XDynamic(xElement);
            dXml.NewPropertyInt = 1234;
            dXml.NewPropertyFloat = (float)0.0546;

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml, typeof(XDynamic));

            Assert.AreEqual(xElement.ToString(SaveOptions.DisableFormatting), xResult.ToString(SaveOptions.DisableFormatting));
        }

        [TestMethod]
        public void ChangeValuePropertyType_XElementChanged()
        {
            const string xStr =
@"<Root>
	<Id>1202</Id>
	<Name>name value</Name>
	<Description>description value</Description>	
</Root>";

            var resultXStr = string.Format(
@"<Root>
	<Id {0}=""System.Single"">0,555</Id>
	<Name>name value</Name>
	<Description>description value</Description>    
</Root>", XDynamic.DataTypeAttributeName);

            var xResult = XElement.Parse(resultXStr);

            var xElement = XElement.Parse(xStr);

            //act
            dynamic dXml = new XDynamic(xElement);
            dXml.Id = 0.555f;

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml, typeof(XDynamic));

            Assert.AreEqual(xElement.ToString(SaveOptions.DisableFormatting), xResult.ToString(SaveOptions.DisableFormatting));
        }
    }
}
