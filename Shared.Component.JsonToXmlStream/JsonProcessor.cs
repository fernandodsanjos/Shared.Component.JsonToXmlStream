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
    public abstract class JsonProcessor
    {
        protected VirtualStream m_stm = new VirtualStream();
        protected JsonToXmlStreamSettings Settings { set; get; }
        protected TextReader JsonInput { set; get; }

        protected XmlWriter wtr = null;
        
        protected JsonTextReader reader = null;

        public JsonProcessor(TextReader jsonInput, JsonToXmlStreamSettings settings)
        {
            Settings = settings;
            JsonInput = jsonInput;

            if (String.IsNullOrEmpty(Settings.Prefix) == false && String.IsNullOrEmpty(Settings.Namespace))
                Settings.Namespace = "http://jsontoxml/";


            wtr = XmlWriter.Create(m_stm, new XmlWriterSettings
            {
                Encoding = Settings.Encoding,
                Indent = Settings.Indent,
                OmitXmlDeclaration = Settings.OmitXmlDeclaration
            });

            reader = new JsonTextReader(JsonInput);

            //This does not work on a readonly stream like a network stream
            //Match match = Regex.Match(json, @"^{[ \x00-\x1F\x7F]*'[a-z]*'[ \x00-\x1F\x7F]*:[ \x00-\x1F\x7F]*{", RegexOptions.IgnoreCase);

        }

        public virtual Stream Parse()
        {
            return null;
        }

        protected void WriteXmlReader(XmlWriter writer, XmlReader reader)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {

                    case XmlNodeType.Element:
                        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);

                        bool selfClosing = reader.IsEmptyElement;

                        string prefix = reader.Prefix;
                        string ns = reader.NamespaceURI;

                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);

                            if (!(reader.LocalName == prefix && reader.Value == ns))
                                writer.WriteAttributeString(reader.LocalName, reader.Value);
                        }

                        if(selfClosing)
                            writer.WriteEndElement();

                        break;
                    case XmlNodeType.Text:
                        writer.WriteString(reader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        writer.WriteEndElement();
                        break;

                    default:
                        break;
                }
            }

            writer.Flush();

        }

    }
}
