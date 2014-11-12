using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlDynamicWrapper;

namespace Tests.XDynamicTests.TryGetMember
{
    [TestClass]
    public class IList
    {
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

            dynamic dXml = new XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.Address, typeof(IList<object>));
            foreach (var iAddress in dXml.Address)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XDynamic));
            }
            Assert.IsInstanceOfType(dXml.NotPartOfList, typeof(XDynamic));
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

            dynamic dXml = new XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(IList<object>));
            foreach (var iAddress in dXml.AddressList)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XDynamic));
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

            dynamic dXml = new XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(XDynamic));
            Assert.IsInstanceOfType(dXml.AddressList.NotPartOfList, typeof(XDynamic));
            Assert.IsInstanceOfType(dXml.AddressList.Address, typeof(IList<object>));

            foreach (var iAddress in dXml.AddressList.Address)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XDynamic));
            }
        }

        [TestMethod]
        public void ListWithDefinedAttribute_ReturnsList()
        {
            const string xStr = @"
<Root>
    <AddressList {0}=""array"">
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
            var xElement = XElement.Parse(string.Format(xStr, XDynamic.DataTypeAttributeName));

            dynamic dXml = new XDynamic(xElement);

            Assert.IsNotNull(dXml);
            Assert.IsInstanceOfType(dXml.AddressList, typeof(IList<object>));
            foreach (var iAddress in dXml.AddressList)
            {
                Assert.IsInstanceOfType(iAddress, typeof(XDynamic));
            }
            dynamic lastElementInList = dXml.AddressList[dXml.AddressList.Count - 1];
            Assert.IsInstanceOfType(lastElementInList, typeof(XDynamic));
            Assert.AreEqual(lastElementInList.NewId, 5);
            Assert.AreEqual(lastElementInList.NewName, "5 name value");
            Assert.AreEqual(lastElementInList.NewDescription, "5 description value");
        }
    }
}
