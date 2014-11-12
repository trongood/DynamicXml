using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using XmlDynamicWrapper;

namespace Tests.XDynamicTests.TryGetMember
{
    [TestClass]
    public class Value
    {
        [TestMethod]
        public void ElementWithPrimitiveTextValue_ReturnText()
        {
            const string xStr = @"<Root><Description>description text</Description></Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XDynamic(xElement);

            var desc = dXml.Description;
            Assert.AreEqual(desc, "description text");
        }

        [TestMethod]
        public void ElementWithPrimitiveTextValueAndIncorrectName_ReturnsNull()
        {
            const string xStr = @"<Root><Description>description text</Description></Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XDynamic(xElement);

            var descFake = dXml.DescriptionFake;
            Assert.IsNull(descFake);
        }

        [TestMethod]
        public void ElementWithIntValue_ReturnInt()
        {
            var xStr = string.Format(@"<Root><Id {0}=""System.Int32"">12345</Id></Root>", XDynamic.DataTypeAttributeName);

            var xElement = XElement.Parse(xStr);

            dynamic dynamicXml = new XDynamic(xElement);

            Assert.AreEqual(dynamicXml.Id, 12345);
            Assert.IsTrue((dynamicXml.Id is int));
        }

        [TestMethod]
        public void ElementWithAttributes_ReturnXDynamicObject()
        {
            var xElement = new XElement("Root");
            var guid = Guid.NewGuid();
            var xChildElement = new XElement("Struct");
            xChildElement.Add(new XAttribute("id", 1));
            xChildElement.Add(new XAttribute("name", "struct name"));
            xChildElement.Add(new XAttribute("guid", guid));
            xChildElement.Add(new XAttribute("floatValue", 0.2124f));

            xElement.Add(xChildElement);

            dynamic dynamicXml = new XDynamic(xElement);

            Assert.IsNotNull(dynamicXml.Struct);
            Assert.IsInstanceOfType(dynamicXml.Struct, typeof(XDynamic));
            Assert.AreEqual(dynamicXml.Struct.id, "1");
            Assert.AreEqual(dynamicXml.Struct.name, "struct name");
            Assert.AreEqual(dynamicXml.Struct.guid, guid.ToString());
            Assert.AreEqual(dynamicXml.Struct.floatValue, "0.2124");
        }

        [TestMethod]
        public void SecondGet_ReadFromCache()
        {
            var xElement = XElement.Parse("<root></root>");

            var dXmlMock = new Mock<XDynamic>(xElement) {CallBase = true};
            int counter = 0;

            dXmlMock.Protected().Setup<object>(
                "GetValue", ItExpr.IsAny<string>(), ItExpr.IsAny<XElement>())
                .Returns(() => 1)
                .Callback(() => counter++);

            dynamic dXml = dXmlMock.Object;

            //act
            var val1 = dXml.Id;
            var val2 = dXml.Id;

            //assert
            Assert.AreEqual(1, counter);
        }


        [TestMethod]
        public void WeakReferenceTest()
        {
            var xElement = XElement.Parse("<root></root>");

            var dXmlMock = new Mock<XDynamic>(xElement) { CallBase = true };
            
            int counter = 0;
            dXmlMock.Protected().Setup<object>(
                "GetValue", ItExpr.IsAny<string>(), ItExpr.IsAny<XElement>())
                .Returns(() => new { Id = 1, Name = "This is name"})
                .Callback(() => counter++);

            dynamic dXml = dXmlMock.Object;

            //act
            var target = dXml.Target;
            target = null;
            GC.Collect(0, GCCollectionMode.Default, true);
            GC.Collect(1, GCCollectionMode.Default, true);
            GC.Collect(2, GCCollectionMode.Default, true);
            var target2 = dXml.Target;
            
            //assert
            Assert.AreEqual(target,target2);
        }
    }
}
