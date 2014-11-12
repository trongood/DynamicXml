using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            ArrayDefinedByAttribute = 2,
            List = 3
        }

        protected internal enum XElementSingleTypeEnum
        {
            Text = 1,
            ValueFromAttribute = 2,
            Fields = 3,
            DynamicXml = 4
        }

        #endregion

        #region private fields

        private readonly Dictionary<string, WeakReference> _cache = new Dictionary<string, WeakReference>();

        private WeakReference<XElement> _xElement;

        #endregion

        #region const

        public const string DataTypeAttributeName = "_dataType";

        #endregion

        #region construct

        public XDynamic(XElement xElement)
        {
            XElement = xElement;
            if (XElement == null)
                throw new ArgumentNullException("xElement");
        }

        public XDynamic(string xName)
        {
            if (string.IsNullOrEmpty(xName))
                throw new ArgumentNullException("xName");

            XElement = new XElement(xName);
        }

        public XDynamic()
        {
            XElement = new XElement("root");
        }

        #endregion

        #region properties

        public XElement XElement
        {
            get
            {
                XElement xRet = null;
                if (!_xElement.TryGetTarget(out xRet))
                {
                    throw new NullReferenceException("XDynamic.XElement: can't find object by reference - create wrapper again");
                }
                return xRet;
            }

            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _xElement = new WeakReference<XElement>(value);
            }
        }

        #endregion

        #region internal logic

        private void RemoveMember(string name, XElement xParent)
        {
            _cache.Remove(name);
            if (xParent == null)
                xParent = XElement;

            var elementsToRemove = xParent.Elements(name).ToList();
            if (elementsToRemove.Any())
            {
                foreach (var xRec in elementsToRemove)
                {
                    xRec.Remove();
                }
            }
        }

        #region cache

        private bool TryGetFromCache(string name, out object value)
        {
            WeakReference ret = null;
            if (_cache.TryGetValue(name, out ret) && ret.IsAlive)
            {
                value = ret.Target;
                return true;
            }

            value = null;
            return false;
        }

        private void SetToCache(string name, object value)
        {
            _cache[name] = new WeakReference(value, false);
        }

        #endregion

        #region get value

        protected internal virtual object GetValue(string name, XElement xParent)
        {
            object ret;

            var xTargetElements = xParent.Elements(name).ToList();
            if (xTargetElements.Count == 0)
            {
                var attributes = xParent.Attributes(name).ToList();
                if (attributes.Any())
                {
                    return attributes.First().Value;
                }

                return null;
            }

            var listType = GetXElementListType(xTargetElements);
            if (listType.HasValue)
            {
                var list = ParseElementsAsList(xTargetElements, listType.Value);
                ret = list;
                return ret;
            }

            ret = ParseElementAsSingle(xTargetElements.Single());
            
            return ret;
        }
        
        private XElementListTypeEnum? GetXElementListType(IList<XElement> elements)
        {
            if (elements.Count > 1) return XElementListTypeEnum.ListWithoutParent;

            var xElement = elements.Single();

            var dataTypeAttribute = xElement.Attributes(DataTypeAttributeName).FirstOrDefault();
            if (dataTypeAttribute != null)
            {
                if (dataTypeAttribute.Value.Equals("array", StringComparison.InvariantCultureIgnoreCase))
                {
                    return XElementListTypeEnum.ArrayDefinedByAttribute;
                }

                return null;
            }

            var childElements = xElement.Elements().ToList();
            if (childElements.Select(x => x.Name).Distinct().Count() == 1 && childElements.Count > 1)
                return XElementListTypeEnum.List;

            return null;
        }

        private XElementSingleTypeEnum GetXElementSingleType(XElement element)
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
                ? XElementSingleTypeEnum.Fields
                : XElementSingleTypeEnum.Text;
        }
        
        #region parse elements as list

        protected internal virtual ObservableCollection<object> ParseElementsAsList(IList<XElement> elements, XElementListTypeEnum listType)
        {
            switch (listType)
            {
                case XElementListTypeEnum.ListWithoutParent:
                    return ParseElementsAsList_ListWithoutXParent(elements);

                case XElementListTypeEnum.List:
                case XElementListTypeEnum.ArrayDefinedByAttribute:
                    return ParseElementsAsList_ListWithParent(elements);

                default:
                    throw new NotImplementedException();
            }
        }

        private ObservableCollection<object> ParseElementsAsList_ListWithoutXParent(IEnumerable<XElement> elements)
        {
            return new ObservableCollection<object>(elements.Select(ParseElementAsSingle));
        }

        private ObservableCollection<object> ParseElementsAsList_ListWithParent(IList<XElement> elements)
        {
            if (elements.Count != 1)
                throw new InvalidOperationException("ParseElementsAsList_ListWithParent: elements.Count != 1");

            var childElements = elements.Single().Elements();
            var ret = new ObservableCollection<object>(childElements.Select(ParseElementAsSingle));
            return ret;
        }

        #endregion

        #region parse element as single

        protected internal virtual object ParseElementAsSingle(XElement element)
        {
            var singleType = GetXElementSingleType(element);

            switch (singleType)
            {
                case XElementSingleTypeEnum.Text:
                    return element.Value;

                case XElementSingleTypeEnum.ValueFromAttribute:
                    return ParseElementAsSingle_PrimitiveByAttribute(element);

                case XElementSingleTypeEnum.Fields:
                case XElementSingleTypeEnum.DynamicXml:
                    return new XDynamic(element);

                default:
                    throw new NotImplementedException("XmlDynamicWrapper.XDynamic.ParseElementAsSingle: ");
            }
        }

        private object ParseElementAsSingle_PrimitiveByAttribute(XElement element)
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
        
        #endregion

        #endregion

        #region set value

        protected internal void SetValue(object value, string name, XElement xParent)
        {
            if (value == null)
            {
                RemoveMember(name, xParent);
                return;
            }

            if (value.GetType().IsValueType)
            {
                SetValue_ValueType(value, name, xParent);
            }

            var xDynamicValue = value as XDynamic;
            if (xDynamicValue != null)
            {
                SetValue_XDynamicType(xDynamicValue);
            }

            var values = (value as IEnumerable<object>);
            if (values != null)
            {
                SetValue_EnumerableType(values, name, xParent);
            }
        }

        protected internal void SetValue_ValueType(object value, string name, XElement xParent, int index = 0)
        {
            XElement targetXElement = null;

            var elements = xParent.Elements(name).ToList();
            if (!elements.Any())
            {
                targetXElement = new XElement(name);
                xParent.Add(targetXElement);
            }

            if (elements.Count > 0)
            {
                targetXElement = elements.Count - 1 >= index ? elements[index] : new XElement(name);
            }

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
        }

        protected internal void SetValue_XDynamicType(XDynamic value)
        {
            var existedIndex = XElement.Elements(value.XElement.Name).ToList().IndexOf(value.XElement);
            if (existedIndex < 0)
            {
                XElement.Add(value.XElement);
            }
        }

        protected internal void SetValue_EnumerableType(IEnumerable<object> values, string name, XElement xParent)
        {
            XElement xArray = null;

            var targetXElements = xParent.Elements(name).ToList();
            if (targetXElements.Count == 0)
            {
                xArray = new XElement(name);
                xArray.Add(new XAttribute(DataTypeAttributeName, "array"));
                xParent.Add(xArray);
            }

            if (xArray == null)
            {
                var listType = GetXElementListType(targetXElements);
                if (!listType.HasValue)
                {
                    RemoveMember(name, xParent);

                    xArray = new XElement(name);
                    xArray.Add(new XAttribute(DataTypeAttributeName, "array"));
                    xParent.Add(xArray);
                }

                switch (listType)
                {
                    case XElementListTypeEnum.List:
                    case XElementListTypeEnum.ArrayDefinedByAttribute:
                        xArray = targetXElements.Single();
                        break;

                    case XElementListTypeEnum.ListWithoutParent:
                        xArray = xParent;
                        break;
                }
            }

            foreach (var iValue in values)
            {
                SetValue(iValue, name, xArray);
            }
        }

        #endregion

        #endregion

        #region DynamicObject

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name;

            if (TryGetFromCache(name, out result)) return true;

            result = GetValue(name, XElement);

            SetToCache(name, result);

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name;

            SetValue(value, name, XElement);

            return true;
        }

        #endregion
    }
}
