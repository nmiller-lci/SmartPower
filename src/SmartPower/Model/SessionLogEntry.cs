using System;
using Realms;

namespace SmartPower.Model
{
    public class SessionLogEntry: RealmObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        
        public DateTimeOffset EntryDateTime { get; set; }
        
        public Session Session { get; set; }
        
        public string Details { get; set; }
        
        public string DeviceType { get; set; }
        
        public int StateId { get; set; }
        
        public SessionState State
        {
            get => (SessionState)StateId;
            set => StateId = (int)value;
        }
    }
}