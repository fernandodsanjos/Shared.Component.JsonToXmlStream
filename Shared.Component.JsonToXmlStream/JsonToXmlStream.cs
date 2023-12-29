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
    [Serializable]
    public class JsonToXmlStream:Stream
    {
       
        VirtualStream m_stm = new VirtualStream();
   
        TextReader jsonInput = null;
        JsonToXmlStreamSettings settings = null;

        HashSet<string> excludes = null;

        protected XmlWriter Writer { get; set; } = null;

        protected JsonTextReader Reader { get; set; } = null;

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
            if (Writer == null)
            {
                if(this.settings.RawMode)
                {
                    this.settings.PrefixObjects = false;
                    this.settings.Namespace = String.Empty;
                    this.settings.RootName = String.Empty;
                }


                if (String.IsNullOrEmpty(this.settings.Prefix) == false && String.IsNullOrEmpty(this.settings.Namespace))
                    this.settings.Namespace = "http://jsontoxml/";


                Writer = XmlWriter.Create(m_stm, new XmlWriterSettings
                {
                    Encoding = this.settings.Encoding,
                    Indent = this.settings.Indent,
                    OmitXmlDeclaration = this.settings.OmitXmlDeclaration  
                });
                Reader = new JsonTextReader(jsonInput);

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
            if(this.settings.RawMode == false)
                Writer.WriteStartElement(this.settings.Prefix, root, this.settings.Namespace);

            string elementName = String.Empty;

            while (Reader.Read())
            {
                switch (Reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName != String.Empty)
                            WriteObject(elementName);
                        break;
                    case JsonToken.EndObject:
                        if (Reader.Depth > 0)
                        {
                            Writer.WriteEndElement();
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


                        elementName = (string)Reader.Value;

                        break;
                    case JsonToken.StartArray:
                        WriteArray(elementName);


                        break;
                        //  default:

                }

                
            }

            //Make sure all elements are closed
            Writer.WriteEndDocument();
            Writer.Flush();
            m_stm.Position = 0;

        }


        private void WriteObject(string name)
        {
            if (Exclude(name))
            {
                this.Reader.Skip();
                return;
            }

            name = SafeName(name);

            if (this.settings.Rename)
            {
                if ((String.IsNullOrEmpty(this.settings.OldName) || String.IsNullOrEmpty(this.settings.NewName)) == false)
                {
                    if (name == this.settings.OldName)
                        name = this.settings.NewName;
                }
            }

            string prefix = null;
            string ns = null;
            if (this.settings.PrefixObjects)
            {
                prefix = this.settings.Prefix;
                ns = this.settings.Namespace;
            }

            Writer.WriteStartElement(prefix, name, ns);

            string elementName = name;

            while (Reader.Read())
            {
                switch (Reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (elementName == String.Empty)
                            elementName = this.settings.RootName;

                            WriteObject(elementName);
                        break;
                    case JsonToken.EndObject:
                        Writer.WriteEndElement();
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

                        elementName = (string)Reader.Value;

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

            elementName = SafeName(elementName);

            while (Reader.Read())
            {
                switch (Reader.TokenType)
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

                        
                        Writer.WriteStartElement(elementName);
                        Writer.WriteValue(Reader.Value);
                        Writer.WriteEndElement();


                        break;


                    case JsonToken.Null:

                        // empty element. do nothing
                        break;
                    case JsonToken.PropertyName:


                        elementName = (string)Reader.Value;

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
            if ((this.settings.UseAttributes || elementName.StartsWith("@")) && Writer.WriteState == System.Xml.WriteState.Element)
            {

                WriteAttribute(elementName,Reader.Value);
            }
            else
            {
                WriteElement(elementName, Reader.Value);
            }
        }

        public virtual void WriteContent(object value)
        {
            Writer.WriteValue(value);
        }

        public virtual void WriteAttribute(string attributetName, object value)
        {
            if (settings.IgnoreEmpty && (value == null || (value is String && String.IsNullOrEmpty((string)value))))
                return;

            Writer.WriteStartAttribute(SafeName(attributetName.StartsWith("@") ? attributetName.Substring(1, attributetName.Length - 1) : attributetName));
            WriteContent(value);
            Writer.WriteEndAttribute();
        }

        public virtual void WriteElement(string elementName, object value)
        {
            if (settings.IgnoreEmpty && (value == null || (value is String && String.IsNullOrEmpty((string)value))))
                return;

            if (Exclude(elementName) && Reader.Depth == 1 && Reader.Path == elementName)
            {
                this.Reader.Skip();
                return;
            }



            Writer.WriteStartElement(SafeName(elementName));
            WriteContent(value);
            Writer.WriteEndElement();
          
              

            
           
        }

        private string SafeName(string name)
        {
            if (Char.IsDigit(name[0]))
                name = "_" + name;

            return name;
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
