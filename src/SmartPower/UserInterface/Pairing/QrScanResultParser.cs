using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Web;
using IDS.Core.IDS_CAN;

namespace SmartPower.UserInterface.Pairing
{
    public abstract class QrScanResult
    {
        public static QrScanResult TryParseQrCode(string qrCodeString)
        {
            try
            {
                var uri = new Uri(qrCodeString, UriKind.Absolute);
                var valueCollection = HttpUtility.ParseQueryString(uri.Query);

                // QR Code must contain parameters for the MAC and then Device Type or the Device Name.
                // 
                if (valueCollection.AllKeys.Contains(GatewayQrScanResult.DEVICE_NAME_KEY) && 
                    valueCollection.AllKeys.Contains(GatewayQrScanResult.PW_KEY))
                {
                    return ParseGatewayQrCode(valueCollection);
                }
                
                // QR Code must contain parameters for the MAC and then Device Type or the Device Name.
                // 
                if ((valueCollection.AllKeys.Contains(DeviceQrScanResult.MAC_KEY) && 
                    valueCollection.AllKeys.Contains(DeviceQrScanResult.DEVICE_TYPE_KEY)) ||
                    valueCollection.AllKeys.Contains(DeviceQrScanResult.DEVICE_NAME_KEY))
                {
                    return ParseDeviceQrCode(valueCollection);
                }

                return new ErrorQrScanResult(Resources.Strings.unrecognized_qr_code);
            }
            catch (Exception ex)
            {
                return new ErrorQrScanResult(string.Format(Resources.Strings.invalid_device_qr_code, ex.GetType().Name, ex.Message, qrCodeString));
            }
        }

        private static QrScanResult ParseGatewayQrCode(NameValueCollection valueCollection)
        {
            var name = valueCollection.Get(GatewayQrScanResult.DEVICE_NAME_KEY);
            var password = valueCollection.Get(GatewayQrScanResult.PW_KEY);
            
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
                return new ErrorQrScanResult(Resources.Strings.missing_qr_keys);

            return new GatewayQrScanResult(name, password);
        }

        private static QrScanResult ParseDeviceQrCode(NameValueCollection valueCollection)
        {
            var deviceType = valueCollection.Get(DeviceQrScanResult.DEVICE_TYPE_KEY);
            var deviceName = valueCollection.Get(DeviceQrScanResult.DEVICE_NAME_KEY);
            var macString = valueCollection.Get(DeviceQrScanResult.MAC_KEY) ?? string.Empty;

            if (string.IsNullOrEmpty(macString))
                return new ErrorQrScanResult(Resources.Strings.missing_qr_keys);
            macString = macString.Replace(":", "");
            var physicalAddress = PhysicalAddress.Parse(macString.Trim().ToUpper());
            var mac = new MAC(physicalAddress.GetAddressBytes());
            
            // Process data from a current QR code
            // 
            if (!string.IsNullOrWhiteSpace(deviceType))
                return ProcessQrCodeData(deviceType, mac);

            // OP-42: Process data from a legacy QR code. This can be removed at some time in the future.
            // As of 5/25/22, the new QR code format has been set for accessory devices; but there
            // are still form engineering samples floating around that are in the previous format.
            // It's minimal effort to support the legacy standard.
            // 
            if (!string.IsNullOrWhiteSpace(deviceName))
                return ProcessLegacyQrCodeData(deviceName, mac);

            return new ErrorQrScanResult(Resources.Strings.missing_qr_keys);
        }

        private static QrScanResult ProcessQrCodeData(string deviceType, MAC mac)
        { 
            byte dt;
            
            try
            {
                dt = (byte)Convert.ToInt32(deviceType, 16);
            }
            catch
            {
                return new ErrorQrScanResult(Resources.Strings.unrecognized_qr_code);
            }

            switch (dt)
            {
                case DEVICE_TYPE.BATTERY_MONITOR:
                    return new DeviceQrScanResult(DEVICE_TYPE.BATTERY_MONITOR, mac);
                case DEVICE_TYPE.AWNING_SENSOR:
                    return new DeviceQrScanResult(DEVICE_TYPE.AWNING_SENSOR, mac);
                default:
                    return new ErrorQrScanResult(Resources.Strings.unrecognized_qr_code);
            }
        }
        
        private static QrScanResult ProcessLegacyQrCodeData(string deviceName, MAC mac)
        {
            var matches = DeviceQrScanResult.DeviceNameRegEx.Match(deviceName);

            var deviceType = matches.Groups[DeviceQrScanResult.DeviceTypePrefixGroup]?.Value ?? string.Empty;
            var partNumber = matches.Groups[DeviceQrScanResult.PartNumberGroup]?.Value ?? string.Empty;

            switch (deviceType)
            {
                case DeviceQrScanResult.BATTERY_MONITOR_DEVICE_TYPE_PREFIX:
                    return new DeviceQrScanResult(DEVICE_TYPE.BATTERY_MONITOR, mac);
                case DeviceQrScanResult.WIND_SENSOR_DEVICE_TYPE_PREFIX:
                    return new DeviceQrScanResult(DEVICE_TYPE.AWNING_SENSOR, mac);
                default:
                    return new ErrorQrScanResult(Resources.Strings.unrecognized_qr_code);
            }
        }
    }

    public class ErrorQrScanResult : QrScanResult
    {
        public string Error { get; }

        public ErrorQrScanResult(string error)
        {
            Error = error;
        }
    }
    
    public class GatewayQrScanResult : QrScanResult
    {
        public const string DEVICE_NAME_KEY = "devid";
        public const string PW_KEY = "pw";
        
        public string Name { get; }
        public string Pin { get; }

        public GatewayQrScanResult(string name, string pin)
        {
            Name = name;
            Pin = pin;
        }
    }

    public class DeviceQrScanResult : QrScanResult
    {
        public const string DEVICE_TYPE_KEY = "DT";
        public const string DEVICE_NAME_KEY = "DN";
        public const string MAC_KEY = "MAC";

        public const string BATTERY_MONITOR_DEVICE_TYPE_PREFIX = "BMO";
        public const string WIND_SENSOR_DEVICE_TYPE_PREFIX = "AWS";

        public static readonly Regex DeviceNameRegEx =
            new Regex("LCI(?<device_type_prefix>.+?)(?<part_number>[0-9]{5})");

        public const string DeviceTypePrefixGroup = "device_type_prefix";
        public const string PartNumberGroup = "part_number";

        public DEVICE_TYPE Type { get; }
        public MAC Mac { get; }

        public DeviceQrScanResult(DEVICE_TYPE type, MAC mac)
        {
            Type = type;
            Mac = mac;
        }
    }
}
