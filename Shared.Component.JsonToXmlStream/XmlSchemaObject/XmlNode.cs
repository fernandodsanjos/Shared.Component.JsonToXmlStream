using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Component.XmlSchemaObject
{
    public class XmlNode
    {
        private Dictionary<string, XmlNode> childNodes { get; set; }

        public XmlNode FirstChild { get; set; }

        public XmlNode FirstOrDefaultChild(string elementName)
        {
            return String.IsNullOrEmpty(elementName) ? FirstChild : ChildNode(elementName);
        }

        public XmlNode CurrentOrDefaultChild(string elementName)
        {
            return String.IsNullOrEmpty(elementName) ? this : ChildNode(elementName);
        }

        private bool HasAttributes { get; set; }

        private bool HasElements { get; set; }

        public bool IsMixed
        {
            get
            {
                return HasAttributes && HasElements;
            }
        }

        public void AddChildNode(XmlNode node)
        {
            bool firstChildNode = childNodes == null ? true : false;

            if (childNodes == null)
            {
                childNodes = new Dictionary<string, XmlNode>();
            }

            if (firstChildNode)
                FirstChild = node;

            if (node.IsAttribute)
            {
                HasAttributes = true;
            }
            else
            {
                HasElements = true;
            }
               

            
            childNodes.Add(node.Name, node);

        }

        public XmlNode ChildNode(string name)
        {
            if (childNodes == null)
                return null;

                XmlNode node = null;
            childNodes.TryGetValue(name, out node);

           return node; 
        }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public bool IsRoot { get; set; }

        public bool IsAttribute { get; set; }

        public bool IsRepetative { get; set; }
    }
}
