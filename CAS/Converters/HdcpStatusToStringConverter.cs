using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;

namespace CAS.Converters
{
    class HdcpStatusToStringConverterr : IValueConverter
    {
        readonly Dictionary<HdcpStatusTypes, string> _hdcpStatusToString = new Dictionary<HdcpStatusTypes, string>
        {
            { HdcpStatusTypes.Actived,      "Active"   },
            { HdcpStatusTypes.Inactived,    "Inactive" },
            { HdcpStatusTypes.None,         "-----"    },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "-";
            }
            if (!(value is HdcpStatusTypes?))
            {
                return Binding.DoNothing;
            }
            return _hdcpStatusToString[(HdcpStatusTypes)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
