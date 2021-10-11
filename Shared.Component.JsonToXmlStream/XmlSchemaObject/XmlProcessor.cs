using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Shared.Component.XmlSchemaObject
{
    public class XmlProcessor
    {
        private XmlSchemaSet SchemaSet { get; set; }
        public XmlNode Process(XmlSchema schema,string rootNode)
        {
          XmlNode root = new XmlNode
          {
              Name = "<root>",
              IsRoot = true
          };

            SchemaSet = new XmlSchemaSet();
            SchemaSet.Add(schema);

            SchemaSet.Compile();

            Traverse(root, schema.Items);

            return root.ChildNode(rootNode);
        }

        private void Traverse(XmlNode node, XmlSchemaObjectCollection items)
        {
            foreach (var item in items)
            {

                if (item is XmlSchemaElement)
                {

                    XmlSchemaElement elm = item as XmlSchemaElement;

                    if (elm.Name == null)
                    {
                        elm = SchemaSet.GlobalElements[elm.QualifiedName] as XmlSchemaElement;
                    }

                    XmlNode elmNode = new XmlNode
                    {
                        Name = elm.Name
                        ,Namespace = elm.QualifiedName.Namespace
                    };

                    node.AddChildNode(elmNode);

                    if (elm.ElementSchemaType is XmlSchemaComplexType)
                    {
                        XmlSchemaComplexType ct = elm.ElementSchemaType as XmlSchemaComplexType;

                        foreach (DictionaryEntry attrib in ct.AttributeUses)
                        {
                            XmlSchemaAttribute attribute = attrib.Value as XmlSchemaAttribute;

                            XmlNode attributeNode = new XmlNode
                            {
                                IsAttribute = true,
                                Name = attribute.Name
                            };

                            elmNode.AddChildNode(attributeNode);

                        }

                        if (ct.Particle is XmlSchemaSequence)
                        {
                            XmlSchemaSequence seq = ct.Particle as XmlSchemaSequence;

                            Traverse(elmNode, seq.Items);
                        }
                        else if (ct.ContentTypeParticle is XmlSchemaSequence)
                        {
                            XmlSchemaSequence seq = ct.ContentTypeParticle as XmlSchemaSequence;

                            Traverse(elmNode, seq.Items);
                        }
                    }
                }

            }
        }
    }
}
