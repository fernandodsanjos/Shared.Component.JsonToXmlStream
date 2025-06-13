﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Shared.Component
{
  [Serializable]
    public class JsonToXmlStreamSettings
    {

        /// <summary>
        /// Allows renaming of one specific object
        /// </summary>
        [Description("Allows renaming of one specific object")]
        [Category("Rename")]
        public bool Rename
        {
            get; set;
        } = false;

       
        [Category("Rename")]
        public string OldName
        {
            get; set;
        }

        [Category("Rename")]
        public string NewName
        {
            get; set;
        }
        /// <summary>
        /// Ignore empty elements
        /// </summary>
        [Description("Ignore empty")]
        public bool IgnoreEmpty
        {
            get; set;
        } = false;

        /// <summary>
        /// Namespace prefix
        /// </summary>
        [Description("Namespace prefix")]
        public string Prefix
        {
            get; set;
        } = String.Empty;

        public string Namespace
        {
            get; set;
        } = String.Empty;


        /// <summary>
        /// Add prefix to all objects
        /// </summary>
        [DisplayName("Prefix objects")]
        public bool PrefixObjects
        {
            get; set;
        } = false;

        public bool OmitXmlDeclaration
        {
            get; set;
        } = false;

        public bool Indent
        {
            get; set;
        } = false;

        /// <summary>
        /// Root name
        /// </summary>
        public string RootName
        {
            get; set;
        } = "root";

        /// <summary>
        /// Array name
        /// </summary>
        public string ArrayName
        {
            get; set;
        } = "record";

        /// <summary>
        /// Exclude record/objects
        /// </summary>
        public string Exclude
        {
            get; set;
        }

        /// <summary>
        /// Ignores root node, namespaces and prefixes
        /// </summary>
        public bool RawMode
        {
            get; set;
        }

        [TypeConverter(typeof(EncodingTypeConverter))]
        /// <summary>
        /// Output encoding
        /// </summary>
        public Encoding Encoding
        {
            get; set;
        } = UTF8Encoding.UTF8;

        /// <summary>
        /// Use attributes
        /// </summary>
        [Description("Output attributes instead of elements for objects")]
        [DisplayName("Use attributes")]
        public bool UseAttributes
        {
            get; set;
        } = false;

       
    }
}
