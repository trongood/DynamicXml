using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace XmlDynamicWrapper
{
    public class XDynamic : DynamicObject
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

        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        private readonly XElement _xElement;

        #endregion

        #region const

        public const string DataTypeAttributeName = "_dataType";

        #endregion

        #region construct

        public XDynamic(XElement xElement)
        {
            _xElement = xElement;
            if (_xElement == null)
                throw new ArgumentNullException("xElement");
        }

        public XDynamic(string xName)
        {
            if (string.IsNullOrEmpty(xName))
                throw new ArgumentNullException("xName");

            _xElement = new XElement(xName);
        }

        public XDynamic()
        {
            _xElement = new XElement("root");
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
                throw new InvalidOperationException("ParseElementsAsList_ListWithParent: elements.Count != 1");

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
                    return new XDynamic(element);

                default:
                    throw new NotImplementedException("XmlDynamicWrapper.XDynamic.ParseElementAsSingle: ");
            }
        }

        protected internal object ParseElementAsSingle_PrimitiveByAttribute(XElement element)
        {
            var dataTypeAttribute = element.Attribute(DataTypeAttributeName);
            if (dataTypeAttribute == null)
                throw new InvalidOperationException(
                    string.Format("ParseElementAsSingle_PrimitiveByAttribute: can't find attribute by name = {0}",
                        DataTypeAttributeName));

            if (string.IsNullOrEmpty(dataTypeAttribute.Value))
                throw new InvalidOperationException("ParseElementAsSingle_PrimitiveByAttribute: can't find attribute value");

            var type = Type.GetType(dataTypeAttribute.Value);
            if (type == null)
                throw new InvalidOperationException(
                    string.Format("ParseElementAsSingle_PrimitiveByAttribute: can't load type by type name = {0}",
                        dataTypeAttribute.Value));

            var elementValue = element.Value;
            if (elementValue == null)
                throw new InvalidOperationException("ParseElementAsSingle_PrimitiveByAttribute: element value is null");

            try
            {
                return Convert.ChangeType(elementValue, type);
            }
            catch (Exception)
            {
                return elementValue;
            }
        }

        protected internal object ParseElementAsSingle_FieldsExpando(XElement element)
        {
            var fieldAttributes = element.Attributes().Where(x => x.Name != DataTypeAttributeName).ToList();
            if (fieldAttributes.Count == 0)
            {
                throw new InvalidOperationException("ParseElementAsSingle_FieldsExpando: no data attributes");
            }

            var fieldsDictionary = new ExpandoObject() as IDictionary<string, Object>;
            foreach (var iFieldAttribute in fieldAttributes)
            {
                fieldsDictionary.Add(string.Concat("_", iFieldAttribute.Name.ToString()), iFieldAttribute.Value);
            }

            return fieldsDictionary;
        }

        #endregion

        protected internal object GetValue(string name)
        {
            object ret = null;
            if (_cache.TryGetValue(name, out ret)) return ret;

            var xTargetElements = _xElement.Elements(name).ToList();
            if (xTargetElements.Count == 0) return null;

            var listType = GetXElementListType(xTargetElements);
            if (listType.HasValue)
            {
                ret = ParseElementsAsList(xTargetElements, listType.Value);
                _cache[name] = ret;
                return ret;
            }

            ret = ParseElementAsSingle(xTargetElements.Single());
            _cache[name] = ret;

            return ret;
        }

        protected internal void RemoveMember(string name)
        {
            _cache.Remove(name);
            var elementsToRemove = _xElement.Elements(name).ToList();
            if (elementsToRemove.Any())
            {
                foreach (var xElement in elementsToRemove)
                {
                    xElement.Remove();
                }
            }
        }

        protected internal void SetValue_ValueType(object value, string name)
        {
            XElement targetXElement = null;

            var elements = _xElement.Elements(name).ToList();
            if (elements.Count == 0)
            {
                targetXElement = new XElement(name);
                _xElement.Add(targetXElement);
            }

            if (elements.Count > 1)
            {
                foreach (var elementToDelete in elements.Skip(1))
                {
                    elementToDelete.Remove();
                }
            }

            targetXElement = elements.Single();

            if (targetXElement == null)
                throw new InvalidOperationException("XmlDynamicWrapper.XDynamic.SetValue_ValueType: targetXElement is null");

            foreach (var childElementsToDelete in targetXElement.Elements())
            {
                childElementsToDelete.Remove();
            }

            var attributesToDelete = targetXElement.Attributes().Where(x => x.Name != DataTypeAttributeName).ToList();
            foreach (var xAttributeToDelete in attributesToDelete)
            {
                xAttributeToDelete.Remove();
            }

            var dataTypeAttribute = targetXElement.Attribute(DataTypeAttributeName);
            if (dataTypeAttribute == null)
            {
                targetXElement.Add(new XAttribute(DataTypeAttributeName, value.GetType().ToString()));
            }
            else
            {
                dataTypeAttribute.Value = value.GetType().ToString();
            }

            targetXElement.Value = value.ToString();

            _cache[name] = value;
        }
        
        #endregion

        #region DynamicObject

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name;

            result = GetValue(name);

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name;

            if (value == null)
            {
                RemoveMember(name);
                return true;
            }

            if (value is IEnumerable)
            {

            }

            if (value.GetType().IsValueType)
            {
                SetValue_ValueType(value, name);
            }
            
            return true;
        }

        #endregion
    }
}
