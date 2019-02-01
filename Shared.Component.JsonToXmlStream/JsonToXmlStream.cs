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
        string rootName = string.Empty;
        string ns = string.Empty;
        string arrayName = string.Empty;



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
            //new StringReader(json)
            //new StreamReader(stream
            m_stm = new VirtualStream();
            wtr = XmlWriter.Create(m_stm, new XmlWriterSettings { Encoding = encoding, Indent = false });
            reader = new JsonTextReader(json);
            
            //This does not work on a readonly stream like a network stream
            //Match match = Regex.Match(json, @"^{[ \x00-\x1F\x7F]*'[a-z]*'[ \x00-\x1F\x7F]*:[ \x00-\x1F\x7F]*{", RegexOptions.IgnoreCase);

            //if(match.Success == false && rootname == String.Empty)
              //  rootname = "root";

            rootName = rootname;
            arrayName = string.IsNullOrEmpty(arrayname) ? "record" : arrayname;

            this.ns = ns;
        }


        #endregion

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (m_stm.Length == 0)
                Read(rootName);

            return m_stm.Read(buffer, offset, count);


        }

        private void Read(string root = "")
        {


            if (root != "")
            { 
                if (wtr.WriteState == System.Xml.WriteState.Start && string.IsNullOrEmpty(this.ns) == false)
                {

                    wtr.WriteStartElement(root, this.ns);
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
            if (wtr.WriteState == System.Xml.WriteState.Start && string.IsNullOrEmpty(this.ns) == false)
            {
               
                wtr.WriteStartElement(name,this.ns);
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
                            elementName = rootName;

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
                            elementName = arrayName;

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
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
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
