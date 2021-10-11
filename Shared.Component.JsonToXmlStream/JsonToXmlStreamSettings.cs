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
    public class JsonToXmlStreamSettings
    {


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
