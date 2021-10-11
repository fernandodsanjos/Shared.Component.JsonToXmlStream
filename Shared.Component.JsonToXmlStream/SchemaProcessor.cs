using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Component.XmlSchemaObject;
using System.IO;
using System.Xml;
using Microsoft.BizTalk.Streaming;
using Newtonsoft.Json;

namespace Shared.Component
{
    public class SchemaProcessor : JsonProcessor
    {

        const int UTF8_BOM = 3;
        public SchemaProcessor(TextReader jsonInput, JsonToXmlStreamSettings settings):base(jsonInput, settings)
        {
            
        }
        public override Stream Parse()
        {
            VirtualStream elmementStream = new VirtualStream();
            XmlWriter elementWriter = XmlWriter.Create(elmementStream, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });

            XmlSchemaObject.XmlNode schemaNode = new XmlSchemaObject.XmlNode();
            XmlProcessor processor = new XmlProcessor();

            schemaNode = processor.Process(Settings.Schema, Settings.RootName);

            if (schemaNode == null)
                throw new NullReferenceException($"SchemaNode with name {Settings.RootName} could not be found");

            wtr.WriteStartElement(Settings.Prefix, Settings.RootName, schemaNode.Namespace);

            string elementName = String.Empty;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        XmlSchemaObject.XmlNode objectNode = schemaNode.FirstOrDefaultChild(elementName);

                        var objectStream = WriteObject(objectNode);

                        WriteElementFragment(elementWriter, objectStream);


                        break;
                    case JsonToken.EndObject:
                        if (reader.Depth > 0)
                        {

                        }

                        break;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                        //All attributes must be written before child elements
                        WriteElementOrAttribute(schemaNode, elementName, wtr, elementWriter);

                        break;
                    case JsonToken.Null:
                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:
                        elementName = (string)reader.Value;

                        break;
                    case JsonToken.StartArray:
                        XmlSchemaObject.XmlNode arrayNode = schemaNode.FirstOrDefaultChild(elementName);

                        var arrayStream = WriteArray(arrayNode);

                        WriteElementFragment(elementWriter, arrayStream);


                        break;
                  
                }

            }

            WriteElementFragment(wtr, elmementStream, elementWriter);
           
            wtr.WriteEndDocument();
            wtr.Flush();
            wtr.Close();
            m_stm.Position = 0;

            return m_stm;
        }

        private Stream WriteArray(XmlSchemaObject.XmlNode schemaNode)
        {
            VirtualStream arrayStream = new VirtualStream();
            XmlWriter arrayWriter = XmlWriter.Create(arrayStream, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseOutput = false });

            string elementName = String.Empty;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    if (schemaNode == null)
                        return null;

                    break;
                }

                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        XmlSchemaObject.XmlNode objectNode = schemaNode == null ?null: schemaNode.CurrentOrDefaultChild(elementName);

                        var objectStream = WriteObject(objectNode);

                        WriteElementFragment(arrayWriter, objectStream);


                        break;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:

                        if (schemaNode != null)
                        {
                            string prefix = Settings.Prefix;
                            string ns = schemaNode.Namespace;

                            if (String.IsNullOrEmpty(schemaNode.Namespace))
                            {
                                prefix = null;
                                ns = null;
                            }

                            arrayWriter.WriteStartElement(prefix, elementName == String.Empty ? schemaNode.Name : elementName, ns);
                            arrayWriter.WriteValue(reader.Value);
                            arrayWriter.WriteEndElement();
                        }

                        break;


                    case JsonToken.Null:

                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:


                        elementName = (string)reader.Value;
                       

                        break;
                    case JsonToken.StartArray:
                        XmlSchemaObject.XmlNode arrayNode = schemaNode == null ? null : schemaNode.ChildNode(elementName);

                        Stream childArray = WriteArray(arrayNode);

                        WriteElementFragment(arrayWriter, childArray);

                        break;
                        //  default:

                }

            }

            arrayWriter.Flush();
            arrayWriter.Close();
            arrayStream.Position = 0;

            return arrayStream;

        }

        private Stream WriteObject(XmlSchemaObject.XmlNode schemaNode)
        {
            VirtualStream elementStream = new VirtualStream();

            
            XmlWriter elementWriter = XmlWriter.Create(elementStream,new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment });

            VirtualStream objectStream = new VirtualStream();
            XmlWriter objectWriter = XmlWriter.Create(objectStream, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment,CloseOutput = false });

            string elementName = String.Empty;

            if(schemaNode != null)
                objectWriter.WriteStartElement(Settings.Prefix, schemaNode.Name, schemaNode.Namespace);

            
            while (reader.Read())
            {
                if(reader.TokenType == JsonToken.EndObject)
                {
                    if (schemaNode == null)
                        return null;

                    break;
                }

                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName != String.Empty)
                        {
                            XmlSchemaObject.XmlNode objectNode = schemaNode == null ? null : schemaNode.ChildNode(elementName);

                            Stream retObjectStream = WriteObject(objectNode);

                            WriteElementFragment(elementWriter, retObjectStream);

                        }

                        break;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:

                        WriteElementOrAttribute(schemaNode, elementName, objectWriter, elementWriter);

                        break;


                    case JsonToken.Null:

                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:

                        elementName = (string)reader.Value;
                       

                        break;
                    // case JsonToken.EndArray:
                    //   return;
                    case JsonToken.StartArray:
                        XmlSchemaObject.XmlNode arrayNode = schemaNode == null ? null : schemaNode.ChildNode(elementName);

                        var innerArray = WriteArray(arrayNode);

                        WriteElementFragment(elementWriter, innerArray);


                        break;
                        //  default:

                }

            }

            WriteElementFragment(objectWriter, elementStream, elementWriter);

            objectWriter.WriteEndElement();
            objectWriter.Flush();
            objectWriter.Close();

            objectStream.Position = 0;

            return objectStream;


        }

        private void WriteElementOrAttribute(XmlSchemaObject.XmlNode schemaNode,string elementName,XmlWriter attributeWriter, XmlWriter elementWriter)
        {
            if (schemaNode == null)
                return;

            XmlSchemaObject.XmlNode childNode = schemaNode.ChildNode(elementName);

            if (childNode == null)
                return;

            if (childNode.IsAttribute)
            {
                WriteValue(childNode, attributeWriter);
            }
            else
            {
                WriteValue(childNode, elementWriter);
            }
        }
        private void WriteElementFragment(XmlWriter objectWriter, Stream elementStream, XmlWriter elementWriter)
        {
            if (elementStream == null)
                return;
           
            elementWriter.Flush();
            elementWriter.Close();

            WriteElementFragment(objectWriter, elementStream);


            
        }

        private void WriteElementFragment(XmlWriter objectWriter , Stream elementStream)
        {
            

            if (elementStream == null)
                return;

            elementStream.Position = 0;

            if (elementStream.Length > UTF8_BOM)
            {
                    XmlReader elementReader = XmlReader.Create(elementStream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
                    WriteXmlReader(objectWriter, elementReader);
            }
           
               

            

        }

        private string StreamToString(Stream input,bool  reset = true)
        {
            string output = String.Empty;

            input.Position = 0;
            StreamReader reader = new StreamReader(input);
            output = reader.ReadToEnd(); 

            if(reset)
                input.Position = 0;

            return output;
        }
        private void WriteValue(XmlSchemaObject.XmlNode schemaNode)
        {
            WriteValue(schemaNode, wtr);
        }
        private void WriteValue(XmlSchemaObject.XmlNode schemaNode, XmlWriter nodeWriter)
        {
            if (schemaNode == null)
                return;

            if (schemaNode.IsAttribute)
            {

                nodeWriter.WriteStartAttribute(schemaNode.Name);
                nodeWriter.WriteValue(reader.Value);
                nodeWriter.WriteEndAttribute();
            }
            else
            {
                string prefix = Settings.Prefix;
                string ns = schemaNode.Namespace;

                if (String.IsNullOrEmpty(schemaNode.Namespace))
                {
                    prefix = null;
                    ns = null;
                }

                nodeWriter.WriteStartElement(prefix,schemaNode.Name, ns);
                nodeWriter.WriteValue(reader.Value);
                nodeWriter.WriteEndElement();
            }
        }
    }
}
