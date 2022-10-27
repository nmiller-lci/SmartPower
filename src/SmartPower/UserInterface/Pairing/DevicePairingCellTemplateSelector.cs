using Xamarin.Forms;

namespace SmartPower.UserInterface.Pairing
{
    public class DevicePairingCellTemplateSelector: DataTemplateSelector
    {
        public DataTemplate WindSensorPairingCell { get; set; }
        public DataTemplate DevicePairingCell { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            switch (item)
            {
                case PairDeviceCellModel _:
                    return DevicePairingCell;
                case PairWindSensorCellModel _:
                    return WindSensorPairingCell;
                default:
                    return null;
            }
        }
    }
}
