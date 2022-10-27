using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Storage;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using SmartPower.Connections.Rv;
using SmartPower.UserInterface.Settings;
using OneControl.Direct.MyRvLink.Devices;
using Xamarin.Essentials;
using MainThread = IDS.Portable.Common.MainThread;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace SmartPower.Services
{
    public class DiagnosticReportManager : Singleton<DiagnosticReportManager>
    {
        public enum State
        {
            Ready,
            GeneratingLog,
            SendingLog,
        }

        private readonly string LogTag = nameof(DiagnosticReportManager);

        private int _generatingLog = 0;

        private readonly string LogFileName = "unknown";

        private readonly string  AppSystemReportFilename = "OneControlSystemReport.html";


        // Require for singleton pattern
        private DiagnosticReportManager()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress">Is called when the state changes and the current percent complete 0-100</param>
        public void SendEmail(Action<State, Exception> progress)
        {
            _ = Task.Run(async () =>
              {
                  try
                  {

                      progress?.Invoke(State.GeneratingLog, null);


                      var zipFullFilename = await MakeLogZipAsync();

                      progress?.Invoke(State.SendingLog, null);

                      var message = new EmailMessage(subject: $"{AppInfo.Name} Diagnostic Report", body: "[Please Add Text To Describe The Problem]", "");
                      var attachment = new EmailAttachment(zipFullFilename, "application/zip");
                      message.Attachments.Add(new EmailAttachment(zipFullFilename, "application/zip"));
                      var tcs = new TaskCompletionSource<bool>();
                      MainThread.RequestMainThreadAction(async () =>
                      {
                          try
                          {
                              await Email.ComposeAsync(message);
                          }
                          finally
                          {
                              tcs.TrySetResult(true);
                          }
                      });
                      await tcs.Task;

                    // Successfully send the e-mail
                    progress?.Invoke(State.Ready, null);
                  }
                  catch (FeatureNotSupportedException fbsEx)
                  {
                      TaggedLog.Warning(LogTag, $"Email not supported on this device {fbsEx.Message}");
                      progress?.Invoke(State.Ready, fbsEx);
                  }
                  catch (Exception ex)
                  {
                      TaggedLog.Warning(LogTag, $"Email send failure {ex.Message}");
                      progress?.Invoke(State.Ready, ex);  // exception is passed to the progress handler and we are ready to try again
                }
              });
        }

        private const string FirebasePathPartValidCharacters = "ABCDEFGHIJKLMNOPQRSUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !@#$%^&()_-+=?,.";
        private const char FirebasePathPartReplacementCharacter = '*';

        private static string SanitizeFirebasePathPart(string pathPart)
        {
            if (string.IsNullOrEmpty(pathPart))
                return String.Empty;

            StringBuilder sanitizeString = new StringBuilder();
            foreach (var character in pathPart)
            {
                if (FirebasePathPartValidCharacters.Contains(character))
                {
                    sanitizeString.Append(character);
                    continue;
                }

                sanitizeString.Append(FirebasePathPartReplacementCharacter);
            }

            return sanitizeString.ToString();
        }

        public async Task UploadLogAsync(Action<State, int, Exception> progress)
        {
            try
            {
                progress?.Invoke(State.GeneratingLog, 0, null);

                var zipFullFilename = await MakeLogZipAsync();

                progress?.Invoke(State.SendingLog, 0, null);

                using (var stream = File.Open(zipFullFilename, FileMode.Open))
                {
                    var now = DateTime.Now;
                    var fileLength = stream.Length;
                    var connectionName = "";
                    if (AppSettings.Instance.SelectedRvGatewayConnection is RvGatewayCanConnectionTcpIpWifiGateway wifiConnection)
                        connectionName = $"{wifiConnection.ConnectionNameFriendly}-";
                    else if (AppSettings.Instance.SelectedRvGatewayConnection is RvGatewayCanConnectionBle bleConnection)
                        connectionName = $"{bleConnection.ConnectionNameFriendly}-";

                    var username = LogFileName;
                    var userNamePathPart = SanitizeFirebasePathPart(username);

                    // Construct Firebase Storage, path to where you want to upload the file and Put it there
                    //
                    // Allowed paths:
                    //      /app/logs/{username}/{date}/{logFilename}
                    //
                    FirebaseStorageTask task = new FirebaseStorage("onecontrol-70c06.appspot.com")
                            .Child("app")
#if DEBUG
                            .Child("oem-tool-logs-debug")
#else
                            .Child("oem-tool-logs")
#endif
                            .Child($"{(string.IsNullOrWhiteSpace(userNamePathPart) ? "Anonymous" : userNamePathPart)}")
                            .Child($"{now.Year:D4}-{now.Month:D2}-{now.Day:D2}")
                            .Child($"{now.Hour:D2}{now.Minute:D2}-{connectionName}-{Guid.NewGuid()}.zip")
                            .PutAsync(stream, CancellationToken.None, "application/zip");

                    void ProgressOnProgressChanged(object sender, FirebaseStorageProgress e)
                    {
                        Console.WriteLine($@"Progress: {e.Percentage} %");
                        progress?.Invoke(State.SendingLog, e.Percentage, null);
                    }

                    // Track progress of the upload
                    task.Progress.ProgressChanged += ProgressOnProgressChanged;

                    // await the task to wait until upload completes and get the download url
                    var downloadUrl = await task;
                    task.Progress.ProgressChanged -= ProgressOnProgressChanged;

                    // Successfully uploaded the log
                    progress?.Invoke(State.Ready, 100, null);
                }
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"UploadLogAsync failed because {ex.Message}");
                progress?.Invoke(State.Ready, 0, ex);   // exception is passed to the progress handler and we are ready to try again
            }
        }

        private async Task<string> MakeLogZipAsync()
        {
            if (Interlocked.Exchange(ref _generatingLog, 1) != 0)
                throw new Exception("Already Generating Log");

            try
            {
                var tcs = new TaskCompletionSource<string>();

                await Task.Run(async () =>
                {
                    var tempPath = System.IO.Path.GetTempPath();
                    var zipFolderName = Path.Combine(tempPath, $"{AppInfo.Name}DiagnosticReport");
                    var zipFullFilename = $"{zipFolderName}.zip";

                    try
                    {
                        TaggedLog.Information(LogTag, $"Generating Diagnostics {zipFullFilename}");

                        DeleteExistingZipFile(zipFullFilename);
                        DeleteExistingZipFolder(zipFolderName);
                        CreateZipFolder(zipFolderName);

                        await TryCopyLogFilesToZipFolderAsync(zipFolderName);

                        await TryCopyTemperatureSensorFilesToZipFolderAsync(zipFolderName);

                        await TryCopyBatteryMonitorFilesToZipFolderAsync(zipFolderName);                        

                        await TryCopyManifestToZipFolderAsync(zipFolderName);

                        await TryCopyMyRvLinkFilesToZipFolderAsync(zipFolderName);

                        TryCopyDiagnosticsReport(zipFolderName);

                        CreateZip(zipFolderName, zipFullFilename);

                        tcs.TrySetResult(zipFullFilename);
                    }
                    catch (Exception ex)
                    {
                        TaggedLog.Information(LogTag, $"Generating Diagnostics Failed {ex.Message}");

                        tcs.TrySetException(ex);
                    }
                }).ConfigureAwait(false);

                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _generatingLog = 0;
            }
        }

        private void CreateZip(string zipFolderName, string zipFullFilename)
        {
            try
            {
                ZipFile.CreateFromDirectory(zipFolderName, zipFullFilename);
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to create zip file {ex.Message}");
                throw;
            }
        }

        private void TryCopyDiagnosticsReport(string zipFolderName)
        {
            var filename = AppSystemReportFilename;
            var fullFilename = Path.Combine(zipFolderName, filename);

            try
            {
                TaggedLog.Debug(LogTag, $"Generate System Report HTML {filename}");
                var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
                var deviceList = logicalDeviceService?.DeviceManager?.FindLogicalDevices<ILogicalDevice>((logicalDevice) => AppCollectionSyncContainer.FilterForSelectedRv(logicalDevice, SelectedRvDeviceOptions.AllDevices | SelectedRvDeviceOptions.WithoutDemoDevices));
#pragma warning disable CS8604 // Possible null reference argument.
                var html = SystemDiagnosticsDeviceReportSyncContainer.MakeHtml(deviceList);
#pragma warning restore CS8604 // Possible null reference argument.

                TaggedLog.Debug(LogTag, $"Copy {filename} to {zipFolderName}\n{html}");
                File.WriteAllText(fullFilename, html);
                if (!File.Exists(fullFilename))
                    throw new Exception($"File couldn't be created {fullFilename}");

            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to make System Report HTML {filename} {ex.Message}");
            }
        }
                
        private async Task TryCopyManifestToZipFolderAsync(string zipFolderName)
        {
            var filename = LogicalDeviceExManifest.DeviceManifestFilename;
            try
            {
                var fullFileName = Path.Combine(AppLogConstants.LogFolder, filename);
                if (!File.Exists(fullFileName))
                    throw new FileNotFoundException($"Skipping Manifest as it's not found {filename}");

                TaggedLog.Debug(LogTag, $"Copy {filename} to {zipFolderName}");
                await CopyFileAsync(fullFileName, Path.Combine(zipFolderName, filename));
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to copy {filename} {ex.Message}");
            }
        }

        private async Task TryCopyTemperatureSensorFilesToZipFolderAsync(string zipFolderName)
        {
            try
            {
                TaggedLog.Debug(LogTag, $"Log folder location {AppLogConstants.LogFolder}");
                DirectoryInfo src = new DirectoryInfo(AppLogConstants.LogFolder);
                FileInfo[] foundFiles = src.GetFiles($"{AppLogConstants.TemperatureSensorFilePrefix}*.json");
                foreach (var file in foundFiles)
                {
                    TaggedLog.Debug(LogTag, $"Copy {file.Name} to {zipFolderName}");

                    // Can't use file.CopyTo because it needs an exclusive lock on the file.  We use CopyFileAsync which opens 
                    // the file with r/w permissions so we can access it while it is being written to by others.
                    //
                    await CopyFileAsync(file.FullName, Path.Combine(zipFolderName, file.Name));
                }
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to temperature sensor files {ex.Message}");
            }
        }

        private async Task TryCopyBatteryMonitorFilesToZipFolderAsync(string zipFolderName)
        {
            try
            {
                TaggedLog.Debug(LogTag, $"Log folder location {AppLogConstants.LogFolder}");
                DirectoryInfo src = new DirectoryInfo(AppLogConstants.LogFolder);
                FileInfo[] foundFiles = src.GetFiles($"{AppLogConstants.BatteryMonitorFilePrefix}*.json");
                foreach (var file in foundFiles)
                {
                    TaggedLog.Debug(LogTag, $"Copy {file.Name} to {zipFolderName}");

                    // Can't use file.CopyTo because it needs an exclusive lock on the file.  We use CopyFileAsync which opens 
                    // the file with r/w permissions so we can access it while it is being written to by others.
                    //
                    await CopyFileAsync(file.FullName, Path.Combine(zipFolderName, file.Name));
                }
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to battery monitor files {ex.Message}");
            }
        }

        private async Task TryCopyLogFilesToZipFolderAsync(string zipFolderName)
        {
            try
            {
                TaggedLog.Debug(LogTag, $"Log folder location {AppLogConstants.LogFolder}");

                DirectoryInfo src = new DirectoryInfo(AppLogConstants.LogFolder);
                FileInfo[] foundFiles = src.GetFiles($"{AppLogConstants.LogFileNameBase}*.{AppLogConstants.LogFileNameExtension}");
                foreach (var file in foundFiles)
                {
                    TaggedLog.Debug(LogTag, $"Copy {file.Name} to {zipFolderName}");

                    // Can't use file.CopyTo because it needs an exclusive lock on the file.  We use CopyFileAsync which opens 
                    // the file with r/w permissions so we can access it while it is being written to by others.
                    //
                    //file.CopyTo(Path.Combine(zipFolderName, file.Name), overwrite: true);
                    await CopyFileAsync(file.FullName, Path.Combine(zipFolderName, file.Name));
                }


            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to copy log files {ex.Message}");
            }
        }

        private async Task TryCopyMyRvLinkFilesToZipFolderAsync(string zipFolderName)
        {
            try
            {
                TaggedLog.Debug(LogTag, $"Log folder location {AppLogConstants.LogFolder}");

                DirectoryInfo src = new DirectoryInfo(AppLogConstants.LogFolder);
                // FileInfo[] foundFiles = src.GetFiles($"{MyRvLinkDeviceTableSerializable.BaseFilename}*.{MyRvLinkDeviceTableSerializable.BaseFilenameExtension}");
                // foreach (var file in foundFiles)
                // {
                //     TaggedLog.Debug(LogTag, $"Copy {file.Name} to {zipFolderName}");
                //     await CopyFileAsync(file.FullName, Path.Combine(zipFolderName, file.Name));
                // }
            }
            catch (Exception ex)
            {
                TaggedLog.Information(LogTag, $"Unable to copy all MyRvLink files {ex.Message}");
            }
        }

        private void CreateZipFolder(string zipFolderName)
        {
            try
            {
                System.IO.Directory.CreateDirectory(zipFolderName);
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Unable to create folder {ex.Message}");
                throw;
            }

        }

        private void DeleteExistingZipFolder(string zipFolderName)
        {
            try
            {
                System.IO.Directory.Delete(zipFolderName, recursive: true);

            }
            catch (DirectoryNotFoundException)
            {
                /* Ignored as it's ok if the file isn't there */
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Unable to delete folder {ex.Message}");
                throw;
            }

        }

        private void DeleteExistingZipFile(string zipFullFilename)
        {
            try
            {
                System.IO.File.Delete(zipFullFilename);
            }
            catch (FileNotFoundException)
            {
                /* Ignored as it's ok if the file isn't there */
            }
            catch (DirectoryNotFoundException)
            {
                /* Ignored as it's ok if the directory isn't there */
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, $"Unable to delete file {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// See https://github.com/serilog/serilog-sinks-file/issues/109
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="destinationFilePath"></param>
        /// <returns></returns>
        public static async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            using (FileStream sourceStream = File.Open(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream destinationStream = File.Create(destinationFilePath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }

}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.