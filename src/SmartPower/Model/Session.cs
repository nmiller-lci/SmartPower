using System;
using System.Linq;
using Realms;

namespace SmartPower.Model
{
    public class Session: RealmObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        
        [Required]
        public string VIN { get; set; }
        
        public int Year { get; set; }
        
        public string Make { get; set; }
        
        public string Model { get; set; }
        
        public DateTimeOffset CreatedDateTime { get; set; }
        
        public DateTimeOffset LastModified { get; set; }
        
        [Backlink(nameof(SessionLogEntry.Session))]
        public IQueryable<SessionLogEntry> LogEntries { get; }
        
        public int StateId { get; set; }
        
        public SessionState State
        {
            get => (SessionState)StateId;
            set => StateId = (int)value;
        }
    }
}