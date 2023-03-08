using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Component
{
    public class EncodingConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (typeof(string) == sourceType)
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (typeof(InstanceDescriptor) == destinationType)
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                string encoding = (string)value;

                Encoding retval;
                
                if (String.Compare(encoding, UTF8Encoding.UTF8.EncodingName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // special case for utf-8 to match with what we do in the default text encoding
                    retval = UTF8Encoding.UTF8;
                }
                else
                {
                    retval = Encoding.GetEncoding(encoding);
                }
              
                return retval;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (typeof(string) == destinationType && value is Encoding)
            {
                Encoding encoding = (Encoding)value;
                return encoding.HeaderName;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

  
}
