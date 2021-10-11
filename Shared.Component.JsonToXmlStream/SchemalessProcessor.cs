using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Microsoft.BizTalk.Streaming;
using Newtonsoft.Json;

namespace Shared.Component
{
    public class SchemalessProcessor:JsonProcessor
    {
        public SchemalessProcessor(TextReader jsonInput, JsonToXmlStreamSettings settings):base(jsonInput, settings)
        {

        }
        public override  Stream Parse()
        {
            wtr.WriteStartElement(Settings.Prefix, Settings.RootName, Settings.Namespace);

            string elementName = String.Empty;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName != String.Empty)
                            WriteObject(elementName);
                        break;
                    case JsonToken.EndObject:
                        if (reader.Depth > 0)
                        {
                            wtr.WriteEndElement();
                        }

                        break;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:


                        WriteValue(elementName);


                        break;


                    case JsonToken.Null:

                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:


                        elementName = (string)reader.Value;

                        break;
                    case JsonToken.StartArray:
                        WriteArray(elementName);


                        break;
                        //  default:

                }


            }

            //Make sure all elements are closed
            wtr.WriteEndDocument();
            wtr.Flush();
            m_stm.Position = 0;

            return m_stm;
        }

        private  void WriteArray(string name)
        {
            string elementName = (String.IsNullOrEmpty(name) ? Settings.ArrayName : name);

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = Settings.ArrayName;

                        WriteObject(elementName);
                        break;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:


                        wtr.WriteStartElement(elementName);
                        wtr.WriteValue(reader.Value);
                        wtr.WriteEndElement();


                        break;


                    case JsonToken.Null:

                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:


                        elementName = (string)reader.Value;

                        break;
                    case JsonToken.EndArray:
                        return;
                    case JsonToken.StartArray:
                        WriteArray(elementName);


                        break;
                        //  default:

                }

            }
        }

        private void WriteObject(string name)
        {
            string prefix = null;
            string ns = null;
            if (Settings.PrefixObjects)
            {
                prefix = Settings.Prefix;
                ns = Settings.Namespace;
            }

            wtr.WriteStartElement(prefix, name, ns);

            string elementName = name;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = Settings.RootName;

                        WriteObject(elementName);
                        break;
                    case JsonToken.EndObject:
                        wtr.WriteEndElement();
                        return;
                    case JsonToken.Date:
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:

                        WriteValue(elementName);


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
                        WriteArray(elementName);


                        break;
                        //  default:

                }

            }

        }

        private void WriteValue(string elementName)
        {
            if (Settings.UseAttributes && wtr.WriteState == System.Xml.WriteState.Element)
            {

                wtr.WriteStartAttribute(elementName);
                wtr.WriteValue(reader.Value);
                wtr.WriteEndAttribute();
            }
            else
            {
                wtr.WriteStartElement(elementName);
                wtr.WriteValue(reader.Value);
                wtr.WriteEndElement();
            }
        }
    }
}
