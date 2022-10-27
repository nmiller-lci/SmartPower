using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.CAN;
using IDS.Portable.Common;
using IDS.Portable.Platform;

namespace SmartPower
{
    public class AppCanDeviceInfo : Singleton<AppCanDeviceInfo>, ICanDeviceInfo
    {
        public DEVICE_TYPE DeviceType { get; }

        public FUNCTION_NAME FunctionName { get; }

        public PRODUCT_ID ProductId { get; }

        public string PartNumber { get; }

        public DEVICE_ID DeviceId { get; }

        // Required for singleton pattern
        private AppCanDeviceInfo()
        {
            // Determine OneControl's DeviceType
            //
            #region Determine DeviceType
            var osType = DeviceInfo.Instance.OS;
            switch (osType)
            {
                case DeviceOS.iOS:
                    DeviceType = DEVICE_TYPE.IOS_MOBILE_DEVICE;
                    break;

                case DeviceOS.Android:
                    if (DeviceInfo.Instance.Variant.IsOCTP()) DeviceType = DEVICE_TYPE.ONECONTROL_TOUCH_PAD;
                    else if (DeviceInfo.Instance.Variant == DeviceVariant.LinkPad) DeviceType = DEVICE_TYPE.TABLET;
                    else DeviceType = DEVICE_TYPE.ANDROID_MOBILE_DEVICE;
                    break;

                case DeviceOS.Windows:
                    DeviceType = DEVICE_TYPE.ONECONTROL_APPLICATION;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unknown OS Type {osType}");
            }
            #endregion

            // Determine OneControl's FunctionName
            //
            #region Determine FunctionName
            if (DeviceInfo.Instance.Variant.IsOCTP()) FunctionName = FUNCTION_NAME.MYRV_TOUCHSCREEN;
            else if (DeviceInfo.Instance.Variant == DeviceVariant.LinkPad) FunctionName = FUNCTION_NAME.MYRV_TABLET;
            else FunctionName = FUNCTION_NAME.MYRV_TABLET;
            #endregion

            // Determine OneControl's ProductId
            //
            #region Determine ProductId
            switch (DeviceInfo.Instance.Variant)
            {
                case DeviceVariant.OCTP_5:
                    ProductId = PRODUCT_ID.LCI_MYRV_5IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY;
                    break;

                case DeviceVariant.OCTP_7:
                    ProductId = PRODUCT_ID.LCI_MYRV_7IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY;
                    break;

                case DeviceVariant.OCTP_10:
                    ProductId = PRODUCT_ID.LCI_MYRV_10IN_ONECONTROL_TOUCH_PANEL_ASSEMBLY;
                    break;

                case DeviceVariant.LinkPad:
                    ProductId = PRODUCT_ID.LCI_LINCPAD_TABLET;
                    break;

                default:
                    switch (osType)
                    {
                        case DeviceOS.iOS:
                            ProductId = PRODUCT_ID.LCI_ONECONTROL_IOS_MOBILE_APPLICATION;
                            break;

                        case DeviceOS.Android:
                            ProductId = PRODUCT_ID.LCI_ONECONTROL_ANDROID_MOBILE_APPLICATION;
                            break;

                        case DeviceOS.Windows:
                            ProductId = PRODUCT_ID.LCI_ONECONTROL_ANDROID_MOBILE_APPLICATION;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Unknown OS Type {osType}");
                    }
                    break;
            }
            #endregion

            // Compute other needed OneControl CanDevice information
            //
            PartNumber = DeviceInfo.Instance.Model ?? string.Empty;

            DeviceId = new DEVICE_ID(ProductId, 0, DeviceType, 0, FunctionName, 0, 0); // Core v2.6 requires that a value be passed for device capabilities
        }
    }
}
