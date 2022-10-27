using System.IO;

namespace SmartPower.Services
{
    public static class DatabaseManager
    {
        private const string RealmDatabaseAssetName = "SmartPower.Assets.data.realm";
        private const string RealmDatabaseBundledDataFile = "BundledData.realm";
        private const string RealmDatabaseSessionDataFile = "SessionData.realm";
        
        public static string BundledDatabasePath
        {
            get
            {
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                var databaseFilePath = Path.Combine(documentsPath, RealmDatabaseBundledDataFile);
                return databaseFilePath;
            }
        }
        
        public static string SessionDatabasePath
        {
            get
            {
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                var databaseFilePath = Path.Combine(documentsPath, RealmDatabaseSessionDataFile);
                return databaseFilePath;
            }
        }
        
        public static void RehydrateBundledDatabases()
        {
            var assembly = typeof(DatabaseManager).Assembly;
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var destinationFile = Path.Combine(documentsPath, RealmDatabaseBundledDataFile);
            
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            
            using var resFilestream = assembly.GetManifestResourceStream(RealmDatabaseAssetName);
            if (resFilestream == null) return;
            var ba = new byte[resFilestream.Length];
            resFilestream.Read(ba, 0, ba.Length);
            File.WriteAllBytes(destinationFile, ba);
        }
    }
}