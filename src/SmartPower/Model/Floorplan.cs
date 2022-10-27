using System.Collections.Generic;
using Realms;

namespace SmartPower.Model
{
    public class Floorplan : RealmObject
    {
        [PrimaryKey]
        [Required]
        public string Id { get; set; }

        [Required]
        public string Make { get; set; }

        [Required]
        public string Model { get; set; }

        public long Year { get; set; }

        public bool HasBatteryMonitor { get; set; }

        public bool HasWindSensor { get; set; }
        
        public IList<long> WindSensors { get; }
    }
}