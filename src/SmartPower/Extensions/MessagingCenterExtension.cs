using System;
using IDS.Portable.Common;
using Serilog;
using Xamarin.Forms;

namespace SmartPower.Extensions
{
    public class MessagingCenterMessage<TMessage>
    {
        // ReSharper disable once StaticMemberInGenericType (YES WE WANT SEPARATE INSTANCE PER TEMPLATE!)
        public static readonly string MessageId = typeof(TMessage).FullName;
    }

    public static class MessagingCenterExtension
    {
        public static void Send<TSender>(this IMessagingCenter messagingCenter, TSender sender)
            where TSender : MessagingCenterMessage<TSender>
        {
            var message = MessagingCenterMessage<TSender>.MessageId;
            messagingCenter.Send(sender, message);
        }

        public static void SubscribeOnMainThread<TSender>(this IMessagingCenter messagingCenter, object subscriber, string message, Action<TSender> callback, TSender source = null)
            where TSender : class
        {
            messagingCenter.Subscribe(subscriber, message, (TSender sender) => {
                MainThread.RequestMainThreadAction(() => {
                    callback(sender);
                });
            }, source);
        }

        public static void SubscribeOnMainThread<TSender>(this IMessagingCenter messagingCenter, object subscriber, Action<TSender> callback, TSender source = null)
            where TSender : MessagingCenterMessage<TSender>
        {
            var message = MessagingCenterMessage<TSender>.MessageId;
            messagingCenter.Subscribe(subscriber, message, (TSender sender) => {
                MainThread.RequestMainThreadAction(() => {
                    callback(sender);
                });
            }, source);
        }

        public static void Subscribe<TSender>(this IMessagingCenter messagingCenter, object subscriber, Action<TSender> callback, TSender source = null)
            where TSender : MessagingCenterMessage<TSender>
        {
            var message = MessagingCenterMessage<TSender>.MessageId;
            messagingCenter.Subscribe(subscriber, message, callback, source);
        }

        public static void TryUnsubscribe<TSender>(this IMessagingCenter messagingCenter, object subscriber, string message)
            where TSender : class
        {
            try
            {
                messagingCenter.Unsubscribe<TSender>(subscriber, message);
            }
            catch
            {
                /* ignored */
            }
        }

        public static void TryUnsubscribe<TSender>(this IMessagingCenter messagingCenter, object subscriber)
            where TSender : MessagingCenterMessage<TSender>
        {
            try
            {
                var message = MessagingCenterMessage<TSender>.MessageId;
                messagingCenter.Unsubscribe<TSender>(subscriber, message);
            }
            catch
            {
                Log.Error($"MessagingCenterExtension TryUnsubscribe failed!");
                /* ignored */
            }
        }
    }
}
