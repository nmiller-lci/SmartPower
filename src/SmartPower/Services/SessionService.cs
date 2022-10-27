using System;
using System.Linq;
using IDS.Core.IDS_CAN;
using SmartPower.Model;

namespace SmartPower.Services
{
    public interface ISessionService
    {
        void RecordNewSession(string vin);
        void RecordSessionEnded(string vin);
        void AddLogEntry(DEVICE_TYPE deviceType, SessionState state, string? details = null);
        IQueryable<Session>? GetSessionsForVin(string vin);
        Session? GetLastSessionForVin(string vin);
        void RecordDevicePaired(DEVICE_TYPE deviceType, string? macAddress, string? deviceName);
        bool WasDevicePairedPreviously(string? macAddress, string? deviceName);
    }
    
    public class SessionService: ISessionService
    {
        private readonly IRealmService _realmService;
        private string? _currentSessionVin;
        
        public SessionService(IRealmService realmService)
        {
            _realmService = realmService;
        }

        public void RecordNewSession(string vin)
        {
            var sessionId = Guid.NewGuid();
            _currentSessionVin = vin;
            
            var realm = _realmService.GetSessionDataRealm();
            realm?.Write(() =>
            {
                realm.Add(new Session()
                {
                    Id = sessionId,
                    VIN = vin,
                    CreatedDateTime = DateTime.UtcNow,
                    State = SessionState.Created
                });
            });
        }
        
        public void RecordSessionEnded(string vin)
        {
            var lastSessionForThisVin = GetLastSessionForVin(vin);
            if (lastSessionForThisVin == null) return;
            
            var session = GetSession(lastSessionForThisVin.Id);
            if (session == null)
                throw new Exception($"Session for VIN {vin} not found");

            _currentSessionVin = null;
            
            var realm = _realmService.GetSessionDataRealm();
            realm?.Write(() =>
            {
                session.State = SessionState.Ended;
                session.LastModified = DateTime.UtcNow;
                realm.Add(session);
            });
        }

        public void AddLogEntry(DEVICE_TYPE deviceType, SessionState state, string? details = null)
        {
            string deviceTypeFriendlyString;
            switch (deviceType)
            {
                case DEVICE_TYPE.BATTERY_MONITOR:
                    deviceTypeFriendlyString = "Battery Monitor";
                    break;
                case DEVICE_TYPE.AWNING_SENSOR:
                    deviceTypeFriendlyString = "Awning Sensor";
                    break;
                case DEVICE_TYPE.BLUETOOTH_GATEWAY:
                    deviceTypeFriendlyString = "RV";
                    break;
                default:
                    deviceTypeFriendlyString = $"{deviceType}";
                    break;
            }
            
            var lastSessionForThisVin = GetLastSessionForVin(_currentSessionVin);
            if (lastSessionForThisVin == null) return;
            AddLogEntry(lastSessionForThisVin.Id, deviceTypeFriendlyString, state, details);
        }

        private void AddLogEntry(Guid sessionId, string deviceType, SessionState state, string? details)
        {
            var realm = _realmService.GetSessionDataRealm();
            var session = GetSession(sessionId);
            if (session == null)
                throw new Exception($"Session with ID {sessionId} not found");
            
            realm?.Write(() =>
            {
                if (session.State != state)
                {
                    session.State = state;
                    session.LastModified = DateTime.UtcNow;
                }
                
                var logEntry = new SessionLogEntry
                {
                    Id = Guid.NewGuid(), 
                    DeviceType = deviceType,
                    Session = session, 
                    EntryDateTime = DateTime.UtcNow, 
                    State = state, 
                    Details = details
                };
                realm.Add(logEntry);
            });
        }

        public IQueryable<Session>? GetSessionsForVin(string vin)
        {
            var realm = _realmService.GetSessionDataRealm();
            return realm?.All<Session>().Where(session => session.VIN == vin);
        }
        
        public Session? GetLastSessionForVin(string vin)
        {
            var realm = _realmService.GetSessionDataRealm();
            return realm?.All<Session>().Where(session => session.VIN == vin).OrderBy(session => session.LastModified).LastOrDefault();
        }

        private Session? GetSession(Guid sessionId)
        {
            var realm = _realmService.GetSessionDataRealm();
            var session = realm?.Find<Session>(sessionId);
            return session;
        }
        
        public void RecordDevicePaired(DEVICE_TYPE deviceType, string? macAddress, string? deviceName)
        {
            var session = GetLastSessionForVin(_currentSessionVin);
            if (session == null) return;
            
            var realm = _realmService.GetSessionDataRealm();
            realm?.Write(() =>
            {
                var logEntry = new SessionPairedDevice
                {
                    Id = Guid.NewGuid(), 
                    DeviceType = deviceType.ToString(),
                    Session = session, 
                    EntryDateTime = DateTime.UtcNow, 
                    MAC = macAddress,
                    DeviceName = deviceName
                };
                realm.Add(logEntry);
            });
        }
        
        public bool WasDevicePairedPreviously(string? macAddress, string? deviceName)
        {
            var realm = _realmService.GetSessionDataRealm();
            if (!string.IsNullOrWhiteSpace(macAddress))
            {
                return realm != null && realm.All<SessionPairedDevice>().Any(record => record.MAC == macAddress);
            }

            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                return realm != null && realm.All<SessionPairedDevice>().Any(record => record.DeviceName == deviceName);
            }

            return false;
        }
    }
}