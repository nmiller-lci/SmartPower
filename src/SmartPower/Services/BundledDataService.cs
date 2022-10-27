using System.Collections.Generic;
using System.Linq;
using IDS.Core.IDS_CAN;
using SmartPower.Model;

namespace SmartPower.Services
{
    public interface IBundledDataService
    {
        Floorplan? GetFloorPlan(string id);
        List<string>? GetMakes();
        List<string>? GetModels(string? make = null);
        List<string>? GetFloorPlans(string? model = null);
        List<PairableDevice> GetDevicesForFloorPlan(string floorPlan);
    }
    
    public class BundledDataService: IBundledDataService
    {
        private readonly IRealmService _realmService;
        
        public BundledDataService(IRealmService realmService)
        {
            _realmService = realmService;
        }
        
        public Floorplan? GetFloorPlan(string id)
        {
            var realm = _realmService.GetBundledDataRealm();
            return realm?.Find<Floorplan>(id);
        }
        
        public List<string>? GetMakes()
        {
            var realm = _realmService.GetBundledDataRealm();
            return realm?.All<Floorplan>().ToList().Select(x => x.Make).Distinct().OrderBy(x => x).ToList();
        }
        
        public List<string>? GetModels(string? make = null)
        {
            var realm = _realmService.GetBundledDataRealm();
            return !string.IsNullOrWhiteSpace(make) ? 
                realm?.All<Floorplan>().Where(x => x.Make == make).ToList().Select(x => x.Model).Distinct().OrderBy(x => x).ToList() : 
                realm?.All<Floorplan>().ToList().Select(x => x.Model).Distinct().OrderBy(x => x).ToList();
            // Realm doesn't support the Select method so the cast ToList is needed just after Where()
        }
        
        public List<string>? GetFloorPlans(string? model = null)
        {
            var realm = _realmService.GetBundledDataRealm();
            return !string.IsNullOrWhiteSpace(model) ? 
                realm?.All<Floorplan>().Where(x => x.Model == model).ToList().Select(x => x.Id).Distinct().OrderBy(x => x).ToList() : 
                realm?.All<Floorplan>().ToList().Select(x => x.Id).OrderBy(x => x).Distinct().ToList();
            // Realm doesn't support the Select method so the cast ToList is needed just after Where()
        }
        
        public List<PairableDevice> GetDevicesForFloorPlan(string floorPlan)
        {
            var list = new List<PairableDevice> { new(DEVICE_TYPE.BLUETOOTH_GATEWAY) };
            var realm = _realmService.GetBundledDataRealm();
            var selectedFloorPlan = realm?.All<Floorplan>().Single(x => x.Id == floorPlan);
            if (selectedFloorPlan == null) return list;

            if (selectedFloorPlan.HasBatteryMonitor)
            {
                list.Add(new(DEVICE_TYPE.BATTERY_MONITOR));
            }

            if (selectedFloorPlan.HasWindSensor)
            {
                foreach (var functionNameId in selectedFloorPlan.WindSensors)
                {
                    switch ((FUNCTION_NAME)functionNameId)
                    {
                        case FUNCTION_NAME.AWNING:
                            list.Add(new(DEVICE_TYPE.AWNING_SENSOR, (FUNCTION_NAME)functionNameId));
                            break;
                        case FUNCTION_NAME.REAR_AWNING:
                            list.Add(new(DEVICE_TYPE.AWNING_SENSOR, (FUNCTION_NAME)functionNameId));
                            break;
                        case FUNCTION_NAME.SIDE_AWNING:
                            list.Add(new(DEVICE_TYPE.AWNING_SENSOR, (FUNCTION_NAME)functionNameId));
                            break;
                    }
                }
            }
            
            return list;
        }
    }
}