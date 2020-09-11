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
        VirtualStream m_stm = null;
        JsonTextReader reader = null;
        TextReader jsonInput = null;
        

        public bool OmitXmlDeclaration
        {
            get; set;
        } = false;

        public bool Indent
        {
            get; set;
        } = false;

        public string Namespace
        {
            get; set;
        } = String.Empty;

        public string RootName
        {
            get; set;
        } = String.Empty;

        public string ArrayName
        {
            get; set;
        } = "record";

        public Encoding Encoding
        {
            get; set;
        }
        [Description("Set this to output attributes instead of elements when possible")]
        public bool UseAttributes
        {
            get; set;
        }

        #region Constructors
        public JsonToXmlStream(TextReader json, string rootname, string ns,string arrayname) : this(json, rootname, ns, arrayname, UTF8Encoding.UTF8)
        {

        }
        public JsonToXmlStream(TextReader json, string rootname, string ns) : this(json, rootname, ns, "record", UTF8Encoding.UTF8)
        {

        }
        public JsonToXmlStream(TextReader json,string rootname) : this(json, rootname, String.Empty, "record", UTF8Encoding.UTF8)
        {

        }
        public JsonToXmlStream(TextReader json):this(json, String.Empty, String.Empty, "record", UTF8Encoding.UTF8)
        {
         
        }
        public JsonToXmlStream(TextReader json,string rootname, string ns, string arrayname, Encoding encoding)
        {
            jsonInput = json;
            RootName = rootname;
            Namespace = ns;
            ArrayName = arrayname;
            Encoding = encoding;
        }


        #endregion

        private void Init()
        {
            if (wtr == null)
            {
                m_stm = new VirtualStream();
                wtr = XmlWriter.Create(m_stm, new XmlWriterSettings
                {
                    Encoding = this.Encoding,
                    Indent = this.Indent,
                    OmitXmlDeclaration = this.OmitXmlDeclaration
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
                Read(this.RootName);

            return m_stm.Read(buffer, offset, count);


        }

        private void Read(string root = "")
        {


            if (root != "")
            { 
                if (wtr.WriteState == System.Xml.WriteState.Start && string.IsNullOrEmpty(this.Namespace) == false)
                {

                    wtr.WriteStartElement(root, this.Namespace);
                }
                else
                {
                    wtr.WriteStartElement(root);
                }
            }

            

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
            if (wtr.WriteState == System.Xml.WriteState.Start && string.IsNullOrEmpty(this.Namespace) == false)
            {
               
                wtr.WriteStartElement(name,this.Namespace);
            }
            else
            {
                wtr.WriteStartElement(name);
            }
           

            string elementName = name;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = this.RootName;

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

            string elementName = name;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = this.ArrayName;

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
            if (UseAttributes && wtr.WriteState == System.Xml.WriteState.Element)
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
