using System;
using System.Diagnostics;
using IDS.Portable.Common;
using Realms;

namespace SmartPower.Services
{
    public interface IRealmService
    {
        Realm? GetBundledDataRealm();
        Realm? GetSessionDataRealm();
    }
    
    public class RealmService: IRealmService
    {
        private readonly string LogTag = nameof(RealmService);
        
        public Realm? GetBundledDataRealm()
        {
            try
            {
                var realmFilePath = DatabaseManager.BundledDatabasePath;
                var realmConfiguration = new RealmConfiguration(realmFilePath)
                {
                    SchemaVersion = 11
                };
                return Realm.GetInstance(realmConfiguration);
            }
            catch (Exception e)
            {
                TaggedLog.Error(LogTag, $"Unable to get bundled data Realm instance: {e.Message}" , e);
            }

            return null;
        }
        
        public Realm? GetSessionDataRealm()
        {
            try
            {
                var realmFilePath = DatabaseManager.SessionDatabasePath;
                var realmConfiguration = new RealmConfiguration(realmFilePath)
                {
                    SchemaVersion = 11
                };
                return Realm.GetInstance(realmConfiguration);
            }
            catch (Exception e)
            {
                TaggedLog.Error(LogTag, $"Unable to get session data Realm instance: {e.Message}" , e);
                Debugger.Break();
            }

            return null;
        }
    }
}