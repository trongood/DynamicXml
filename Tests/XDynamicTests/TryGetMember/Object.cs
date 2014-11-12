using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlDynamicWrapper;

namespace Tests.XDynamicTests.TryGetMember
{
    [TestClass]
    public class Object
    {
        [TestMethod]
        public void ElementWithComplexChildElement_ReturnsDynamicXmlWithDynamicXmlProperty()
        {
            const string xStr =
@"<Root>
	<Id>1202</Id>
	<Name>name value</Name>
	<Description>description value</Description>
	<Child>
		<Id>123</Id>
		<Name>child name value</Name>
		<Description>child description value</Description>
	</Child>
</Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml, typeof(XDynamic));

            Assert.AreEqual(dXml.Id, "1202");
            Assert.AreEqual(dXml.Name, "name value");
            Assert.AreEqual(dXml.Description, "description value");

            Assert.IsNotNull(dXml.Child);
            Assert.IsInstanceOfType(dXml.Child, typeof(XDynamic));

            Assert.AreEqual(dXml.Child.Id, "123");
            Assert.AreEqual(dXml.Child.Name, "child name value");
            Assert.AreEqual(dXml.Child.Description, "child description value");
        }
    }
}
