using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml;
using Microsoft.BizTalk.Streaming;
using System.IO;
using System.Text.RegularExpressions;

namespace Shared.Component
{
    public class JsonToXmlStream:Stream
    {
        private XmlWriter wtr = null;
        VirtualStream m_stm = new VirtualStream();
        JsonTextReader reader = null;
        TextReader jsonInput = null;
        JsonToXmlStreamSettings settings = null;

        HashSet<string> excludes = null;

        private void LoadExcludes()
        {
            excludes = new HashSet<string>();

            if (String.IsNullOrEmpty(settings.Exclude) == false)
            {
                String[] excl = settings.Exclude.Split(';');

                

                foreach (var item in excl)
                {
                    excludes.Add(item);
                }
            }
        }

        private bool Exclude(string name)
        {
            return excludes.Contains(name);
        }
        public JsonToXmlStreamSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
            }
        }
        
        public JsonToXmlStream(TextReader reader)
        {
           
            this.jsonInput = reader;
            this.settings = new JsonToXmlStreamSettings();
            LoadExcludes();
        }
       
        public JsonToXmlStream(TextReader reader,JsonToXmlStreamSettings settings)
        {
           
            this.jsonInput = reader;
            this.settings = settings;
            LoadExcludes();
        }

        

        private void Init()
        {
            if (wtr == null)
            {

                if (String.IsNullOrEmpty(this.settings.Prefix) == false && String.IsNullOrEmpty(this.settings.Namespace))
                    this.settings.Namespace = "http://jsontoxml/";

               
                wtr = XmlWriter.Create(m_stm, new XmlWriterSettings
                {
                    Encoding = this.settings.Encoding,
                    Indent = this.settings.Indent,
                    OmitXmlDeclaration = this.settings.OmitXmlDeclaration  
                });
                reader = new JsonTextReader(jsonInput);

                //This does not work on a readonly stream like a network stream
                //Match match = Regex.Match(json, @"^{[ \x00-\x1F\x7F]*'[a-z]*'[ \x00-\x1F\x7F]*:[ \x00-\x1F\x7F]*{", RegexOptions.IgnoreCase);

            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            Init();

            if (m_stm.Length == 0)
                Read(this.settings.RootName);

            return m_stm.Read(buffer, offset, count);


        }

        private void Read(string root = "")
        {

            wtr.WriteStartElement(this.settings.Prefix, root, this.settings.Namespace);

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

        }


        private void WriteObject(string name)
        {
            if (Exclude(name))
            {
                this.reader.Skip();
                return;
            }
                 
            string prefix = null;
            string ns = null;
            if (this.settings.PrefixObjects)
            {
                prefix = this.settings.Prefix;
                ns = this.settings.Namespace;
            }

            wtr.WriteStartElement(prefix, name, ns);

            string elementName = name;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = this.settings.RootName;

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
        private void WriteArray(string name)
        {

            string elementName = (String.IsNullOrEmpty(name) ? this.settings.ArrayName:name);

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = this.settings.ArrayName;

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

        private void WriteValue(string elementName)
        {
            if (this.settings.UseAttributes && wtr.WriteState == System.Xml.WriteState.Element)
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
        #region Stream standard overrides
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
               return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return m_stm.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_stm.Position;
            }

            set
            {
                throw new NotImplementedException();
            }
        }


        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
