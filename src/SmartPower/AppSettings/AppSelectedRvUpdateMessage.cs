using SmartPower.Extensions;
using Xamarin.Forms;

namespace SmartPower
{
    public class AppSelectedRvUpdateMessage : MessagingCenterMessage<AppSelectedRvUpdateMessage>
    {
        public static AppSelectedRvUpdateMessage DefaultMessage { get; } = new AppSelectedRvUpdateMessage();
        public static void SendMessage() => MessagingCenter.Instance.Send(DefaultMessage);
    }
}



