using System;
using Realms;

namespace SmartPower.Model
{
    public class SessionPairedDevice: RealmObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        
        public DateTimeOffset EntryDateTime { get; set; }
        
        public Session Session { get; set; }
        
        public string MAC { get; set; }
        
        public string DeviceName { get; set; }
        
        public string DeviceType { get; set; }
    }
}