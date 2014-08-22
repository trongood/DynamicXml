using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace XmlDynamicWrapper.Models
{
    public class DynamicXml : DynamicObject
    {
        #region enums

        protected internal enum XElementListTypeEnum
        {
            ListWithoutParent = 1,
            ListDefinedByAttribute = 2,
            List = 3
        }

        protected internal enum XElementSingleTypeEnum
        {
            Text = 1,
            ValueFromAttribute = 2,
            FieldsExpando = 3,
            DynamicXml = 4
        }

        #endregion

        #region private fields

        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();

        private readonly XElement _xElement;

        #endregion

        #region const

        public const string DataTypeAttributeName = "_dataType";

        #endregion

        #region construct

        public DynamicXml(XElement xElement)
        {
            _xElement = xElement;
            if (_xElement == null)
                throw new ArgumentNullException("xElement");
        }

        #endregion

        #region internal logic

        protected internal XElementListTypeEnum? GetXElementListType(IList<XElement> elements)
        {
            if (elements.Count > 1) return XElementListTypeEnum.ListWithoutParent;

            var xElement = elements.Single();

            var dataTypeAttribute = xElement.Attributes(DataTypeAttributeName).FirstOrDefault();
            if (dataTypeAttribute != null)
            {
                if (dataTypeAttribute.Value.Equals("list", StringComparison.InvariantCultureIgnoreCase) ||
                    dataTypeAttribute.Value.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                {
                    return XElementListTypeEnum.ListDefinedByAttribute;
                }

                return null;
            }

            var childElements = xElement.Elements().ToList();
            if (childElements.Select(x => x.Name).Distinct().Count() == 1 && childElements.Count > 1)
                return XElementListTypeEnum.List;

            return null;
        }

        protected internal XElementSingleTypeEnum GetXElementSingleType(XElement element)
        {
            if (element.HasElements)
                return XElementSingleTypeEnum.DynamicXml;

            if (!string.IsNullOrEmpty(element.Value))
            {
                var dataTypeAttribute = element.Attributes(DataTypeAttributeName).FirstOrDefault();
                return dataTypeAttribute != null
                    ? XElementSingleTypeEnum.ValueFromAttribute
                    : XElementSingleTypeEnum.Text;
            }

            var fieldAttributes = element.Attributes().Where(x => x.Name != DataTypeAttributeName).ToList();
            return fieldAttributes.Count > 0
                ? XElementSingleTypeEnum.FieldsExpando
                : XElementSingleTypeEnum.Text;
        }

        protected internal XElementListTypeEnum? GetObjectListType(object value)
        {
            return null;
        }

        #region parse elements as list

        protected internal IList<object> ParseElementsAsList(IList<XElement> elements, XElementListTypeEnum listType)
        {
            switch (listType)
            {
                case XElementListTypeEnum.ListWithoutParent:
                    return ParseElementsAsList_ListWithoutXParent(elements);

                case XElementListTypeEnum.ListDefinedByAttribute:
                    return ParseElementsAsList_ListWithParent(elements);

                case XElementListTypeEnum.List:
                    return ParseElementsAsList_ListWithParent(elements);

                default:
                    throw new NotImplementedException();
            }
        }

        protected internal IList<object> ParseElementsAsList_ListWithoutXParent(IEnumerable<XElement> elements)
        {
            return elements.Select(ParseElementAsSingle).ToList();
        }

        protected internal IList<object> ParseElementsAsList_ListWithParent(IList<XElement> elements)
        {
            if (elements.Count != 1)
                throw new Exception("ParseElementsAsList_ListWithParent: elements.Count != 1");

            var childElements = elements.Single().Elements();

            return childElements.Select(ParseElementAsSingle).ToList();
        }

        #endregion

        #region parse element as single

        protected internal object ParseElementAsSingle(XElement element)
        {
            var singleType = GetXElementSingleType(element);

            switch (singleType)
            {
                case XElementSingleTypeEnum.Text:
                    return element.Value;

                case XElementSingleTypeEnum.ValueFromAttribute:
                    return ParseElementAsSingle_PrimitiveByAttribute(element);

                case XElementSingleTypeEnum.FieldsExpando:
                    return ParseElementAsSingle_FieldsExpando(element);

                case XElementSingleTypeEnum.DynamicXml:
                    return new DynamicXml(element);

                default:
                    throw new NotImplementedException();
            }
        }

        protected internal object ParseElementAsSingle_PrimitiveByAttribute(XElement element)
        {
            var dataTypeAttribute = element.Attribute(DataTypeAttributeName);
            if (dataTypeAttribute == null)
                throw new Exception(
                    string.Format("ParseElementAsSingle_PrimitiveByAttribute: can't find attribute by name = {0}",
                        DataTypeAttributeName));

            if (string.IsNullOrEmpty(dataTypeAttribute.Value))
                throw new Exception("ParseElementAsSingle_PrimitiveByAttribute: can't find attribute value");

            var type = Type.GetType(dataTypeAttribute.Value);
            if (type == null)
                throw new Exception(
                    string.Format("ParseElementAsSingle_PrimitiveByAttribute: can't load type by type name = {0}",
                        dataTypeAttribute.Value));

            var elementValue = element.Value;
            if (elementValue == null)
                throw new Exception("ParseElementAsSingle_PrimitiveByAttribute: element value is null");

            return Convert.ChangeType(elementValue, type);
        }

        protected internal object ParseElementAsSingle_FieldsExpando(XElement element)
        {
            var fieldAttributes = element.Attributes().Where(x => x.Name != DataTypeAttributeName).ToList();
            if (fieldAttributes.Count == 0)
            {
                throw new Exception("ParseElementAsSingle_FieldsExpando: no data attributes");
            }

            var fieldsDictionary = new ExpandoObject() as IDictionary<string, Object>;
            foreach (var iFieldAttribute in fieldAttributes)
            {
                fieldsDictionary.Add(string.Concat("_", iFieldAttribute.Name.ToString()), iFieldAttribute.Value);
            }

            return fieldsDictionary;
        }

        #endregion

        #endregion

        #region DynamicObject

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name;

            if (_dictionary.TryGetValue(name, out result)) return true;

            var xTargetElements = _xElement.Elements(name).ToList();
            if (xTargetElements.Count == 0) return false;

            var listType = GetXElementListType(xTargetElements);
            if (listType.HasValue)
            {
                result = ParseElementsAsList(xTargetElements, listType.Value);
                _dictionary[name] = result;
                return true;
            }

            result = ParseElementAsSingle(xTargetElements.Single());
            _dictionary[name] = result;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name;

            var xElementsList = _xElement.Elements(name).ToList();

            if (xElementsList.Any())
            {
                var listType = GetXElementListType(xElementsList);
                if (listType.HasValue)
                {
                    if (!(value is Array) && !(value is IList))
                    {
                        throw new InvalidCastException(
                            string.Format("Incorrect type ({0}) for Property = {1}",
                            value.GetType(),
                            name));
                    }

                    throw new NotImplementedException();
                }

                var singleType = GetXElementSingleType(xElementsList.Single());
                
            }
            
            
            _dictionary[binder.Name] = value;

            return true;
        }

        #endregion
    }
}
