using System;
using System.Globalization;
using IDS.Core.IDS_CAN;
using Xamarin.Forms;

namespace SmartPower.UserInterface.Converters
{
    public class DeviceTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DEVICE_TYPE deviceType))
                return false;

            switch (deviceType)
            {
                case DEVICE_TYPE.BATTERY_MONITOR:
                    return Resources.Strings.battery_monitor;
                case DEVICE_TYPE.AWNING_SENSOR:
                    return Resources.Strings.wind_sensor;
                case DEVICE_TYPE.BLUETOOTH_GATEWAY:
                    return Resources.Strings.rv;
                default:
                    return deviceType.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}