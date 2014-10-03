using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.XDynamicTests
{
    [TestClass]
    public class TryGetMember
    {
        [TestMethod]
        public void ElementWithPrimitiveTextValue_ReturnText()
        {
            const string xStr =@"<Root><Description>description text</Description></Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            var desc = dXml.Description;
            Assert.AreEqual(desc, "description text");
        }
        
        [TestMethod]
        public void ElementWithPrimitiveTextValueAndIncorrectName_ReturnsNull()
        {
            const string xStr = @"<Root><Description>description text</Description></Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            var descFake = dXml.DescriptionFake;
            Assert.IsNull(descFake);
        }

        [TestMethod]
        public void ElementWithIntValue_ReturnInt()
        {
            var xStr = string.Format(@"<Root><Id {0}=""System.Int32"">12345</Id></Root>", XmlDynamicWrapper.XDynamic.DataTypeAttributeName);

            var xElement = XElement.Parse(xStr);
            
            dynamic dynamicXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.AreEqual(dynamicXml.Id, 12345);
        }

        [TestMethod]
        public void ElementWithAttributes_ReturnExpandoObject()
        {
            var xElement = new XElement("Root");
            var guid = Guid.NewGuid();
            var xChildElement = new XElement("Struct");
            xChildElement.Add(new XAttribute("Id", 1));
            xChildElement.Add(new XAttribute("Name", "struct name"));
            xChildElement.Add(new XAttribute("Guid", guid));
            xChildElement.Add(new XAttribute("Float", 0.2124f));

            xElement.Add(xChildElement);

            dynamic dynamicXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.IsNotNull(dynamicXml.Struct);
            Assert.IsInstanceOfType(dynamicXml.Struct, typeof(ExpandoObject));
            Assert.AreEqual(dynamicXml.Struct._Id, "1");
            Assert.AreEqual(dynamicXml.Struct._Name, "struct name");
            Assert.AreEqual(dynamicXml.Struct._Guid, guid.ToString());
            Assert.AreEqual(dynamicXml.Struct._Float, "0.2124");
        }

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

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml, typeof(XmlDynamicWrapper.XDynamic));

            Assert.AreEqual(dXml.Id, "1202");
            Assert.AreEqual(dXml.Name, "name value");
            Assert.AreEqual(dXml.Description, "description value");
            
            Assert.IsNotNull(dXml.Child);
            Assert.IsInstanceOfType(dXml.Child, typeof(XmlDynamicWrapper.XDynamic));
            
            Assert.AreEqual(dXml.Child.Id, "123");
            Assert.AreEqual(dXml.Child.Name, "child name value");
            Assert.AreEqual(dXml.Child.Description, "child description value");
        }

        [TestMethod]
        public void ListWithoutParent_ReturnsList()
        {
            const string xStr = @"
<Root>
	<Address>
		<Id>1</Id>
		<Name>1 name value</Name>
		<Description>1 description value</Description>
	</Address>
	<Address>
		<Id>2</Id>
		<Name>2 name value</Name>
		<Description>2 description value</Description>
	</Address>
	<Address>
		<Id>3</Id>
		<Name>3 name value</Name>
		<Description>3 description value</Description>
	</Address>
	<Address>
		<Id>4</Id>
		<Name>4 name value</Name>
		<Description>4 description value</Description>
	</Address>
    <NotPartOfList>
		<Id>0</Id>
		<Name>name value</Name>
		<Description>description value</Description>
	</NotPartOfList>
    <Description>description text</Description>
</Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);
            
            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.Address, typeof(List<object>));
            foreach (var iAddress in dXml.Address)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XmlDynamicWrapper.XDynamic));
            }
            Assert.IsInstanceOfType(dXml.NotPartOfList, typeof(XmlDynamicWrapper.XDynamic));
            Assert.AreEqual(dXml.NotPartOfList.Id, "0");
            Assert.AreEqual(dXml.NotPartOfList.Name, "name value");
            Assert.AreEqual(dXml.NotPartOfList.Description, "description value");
            Assert.AreEqual(dXml.Description, "description text");
        }

        [TestMethod]
        public void ListWithoutDefinedAttribute_ReturnsList()
        {
            const string xStr = @"
<Root>
    <AddressList>
	    <Address>
		    <Id>1</Id>
		    <Name>1 name value</Name>
		    <Description>1 description value</Description>
	    </Address>
	    <Address>
		    <Id>2</Id>
		    <Name>2 name value</Name>
		    <Description>2 description value</Description>
	    </Address>
	    <Address>
		    <Id>3</Id>
		    <Name>3 name value</Name>
		    <Description>3 description value</Description>
	    </Address>
	    <Address>
		    <Id>4</Id>
		    <Name>4 name value</Name>
		    <Description>4 description value</Description>
	    </Address>
    </AddressList>
</Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(List<object>));
            foreach (var iAddress in dXml.AddressList)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XmlDynamicWrapper.XDynamic));
            }
        }

        [TestMethod]
        public void ListWithoutDefinedAttributeButWithInnerListWithoutParent_ReturnsDynamicXmlPropertyList()
        {
            const string xStr = @"
<Root>
    <AddressList>
	    <Address>
		    <Id>1</Id>
		    <Name>1 name value</Name>
		    <Description>1 description value</Description>
	    </Address>
	    <Address>
		    <Id>2</Id>
		    <Name>2 name value</Name>
		    <Description>2 description value</Description>
	    </Address>
	    <Address>
		    <Id>3</Id>
		    <Name>3 name value</Name>
		    <Description>3 description value</Description>
	    </Address>
	    <Address>
		    <Id>4</Id>
		    <Name>4 name value</Name>
		    <Description>4 description value</Description>
	    </Address>
        <NotPartOfList>
		    <Id>4</Id>
		    <Name>4 name value</Name>
		    <Description>4 description value</Description>
	    </NotPartOfList>
    </AddressList>
</Root>";
            var xElement = XElement.Parse(xStr);

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(XmlDynamicWrapper.XDynamic));
            Assert.IsInstanceOfType(dXml.AddressList.NotPartOfList, typeof(XmlDynamicWrapper.XDynamic));
            Assert.IsInstanceOfType(dXml.AddressList.Address, typeof(List<object>));

            foreach (var iAddress in dXml.AddressList.Address)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XmlDynamicWrapper.XDynamic));
            }
        }

        [TestMethod]
        public void ListWithDefinedAttribute_ReturnsList()
        {
            const string xStr = @"
<Root>
    <AddressList {0}=""List"">
	    <Address>
		    <Id>1</Id>
		    <Name>1 name value</Name>
		    <Description>1 description value</Description>
	    </Address>
	    <Address>
		    <Id>2</Id>
		    <Name>2 name value</Name>
		    <Description>2 description value</Description>
	    </Address>
	    <Address>
		    <Id>3</Id>
		    <Name>3 name value</Name>
		    <Description>3 description value</Description>
	    </Address>
	    <Address>
		    <Id>4</Id>
		    <Name>4 name value</Name>
		    <Description>4 description value</Description>
	    </Address>
        <NotPartOfList>
		    <NewId {0}=""System.Int32"">5</NewId>
		    <NewName>5 name value</NewName>
		    <NewDescription>5 description value</NewDescription>
	    </NotPartOfList>
    </AddressList>
</Root>";
            var xElement = XElement.Parse(string.Format(xStr, XmlDynamicWrapper.XDynamic.DataTypeAttributeName));

            dynamic dXml = new XmlDynamicWrapper.XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(List<object>));
            foreach (var iAddress in dXml.AddressList)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XmlDynamicWrapper.XDynamic));
            }
            dynamic lastElementInList = dXml.AddressList[dXml.AddressList.Count - 1];
            Assert.IsInstanceOfType(lastElementInList, typeof(XmlDynamicWrapper.XDynamic));
            Assert.AreEqual(lastElementInList.NewId, 5);
            Assert.AreEqual(lastElementInList.NewName, "5 name value");
            Assert.AreEqual(lastElementInList.NewDescription, "5 description value");
        }
    }
}
