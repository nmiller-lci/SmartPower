
using HandlebarsDotNet;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;
using IDS.Portable.LogicalDevice;
using SmartPower.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xamarin.Forms;

namespace SmartPower.UserInterface.Settings
{
    public class SystemDiagnosticsDeviceReportSyncContainer : AppCollectionSyncContainer<OrderedObservableCollection<ILogicalDevice>, ILogicalDevice, ILogicalDevice>
    {
        private const string LogTag = nameof(SystemDiagnosticsDeviceReportSyncContainer);

        private const string SystemDiagnosticsDataTemplateFilename = "SystemDiagnosticsDeviceReportTemplate.html";
        public const string HtmlEmptyTemplate = "<html><body style=\"background-color:{{primary_color}};color:white\">System Diagnostics Not Available At This Time</body></html>";

        private readonly HtmlWebViewSource _htmlSource;
        private readonly Func<object, string> _diagnosticsHtmlTemplateExecute;

        // We don't want the base constructors to auto sync as our constructor will do that after we complete our setup (_navigationService)
        protected override bool AutoDataSourceSyncOnConstruction => false;

        protected override SelectedRvDeviceOptions FilterForSelectedRvDeviceOptions => SelectedRvDeviceOptions.WithNames | SelectedRvDeviceOptions.WithValidConfiguration;

        static SystemDiagnosticsDeviceReportSyncContainer()
        {
            Handlebars.RegisterHelper("formatted-ActiveConnection", (writer, context, parameters) => {
                try
                {
                    if (context is not ILogicalDevice logicalDevice)
                    {
                        writer.Write("");
                        return;
                    }

                    switch (logicalDevice.ActiveConnection)
                    {
                        case LogicalDeviceActiveConnection.Direct:
                            writer.WriteSafeString("<div style=\"color:green\">Online</div>");
                            break;

                        case LogicalDeviceActiveConnection.Remote:
                            writer.WriteSafeString("<div style=\"color:green\">Remote</div>");
                            break;

                        default:
                        case LogicalDeviceActiveConnection.Offline:
                            writer.WriteSafeString("Offline");
                            break;
                    }
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });

            Handlebars.RegisterHelper("formatted-AssemblyPartNumber", async (writer, context, parameters) => {
                try
                {
                    if (context is not ILogicalDevice logicalDevice)
                    {
                        writer.Write("");
                        return;
                    }

                    string assemblyPartNumber;
                    try
                    {
                        assemblyPartNumber = await logicalDevice.GetSoftwarePartNumberAsync(CancellationToken.None);
                    }
                    catch
                    {
                        assemblyPartNumber = "unavailable";
                    }

                    writer.WriteSafeString($"{assemblyPartNumber}");
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });

            Handlebars.RegisterHelper("application_name", async (writer, context, parameters) => {
                try
                {
                    writer.WriteSafeString($"{Xamarin.Essentials.AppInfo.Name}");
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });

            Handlebars.RegisterHelper("primary_color", (writer, context, parameters) =>
            {
                try
                {
                    writer.WriteSafeString(ToHexAsRgba(Color.White));
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });

            Handlebars.RegisterHelper("text_color", (writer, context, parameters) =>
            {
                try
                {
                    writer.WriteSafeString(ToHexAsRgba(Color.Black));
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });
            Handlebars.RegisterHelper("accent_color", (writer, context, parameters) =>
            {
                try
                {
                    writer.WriteSafeString(ToHexAsRgba((Color)Application.Current.Resources[IDS.UI.Resources.Style.Colors.Tertiary]));
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });
            Handlebars.RegisterHelper("alt_text_color", (writer, context, parameters) =>
            {
                try
                {
                    writer.WriteSafeString(ToHexAsRgba((Color)Application.Current.Resources[IDS.UI.Resources.Style.Colors.OnPrimary]));
                }
                catch (Exception e)
                {
                    TaggedLog.Error(LogTag, "Error writing system diagnostics: " + e);
                }
            });
        }

        private static string ToHexAsRgba(Color color)
        {
            // Xamarin.Forms Color.ToHex() returns ARGB, but HTML requires RGBA.
            var red = (uint)(color.R * 255);
            var green = (uint)(color.G * 255);
            var blue = (uint)(color.B * 255);
            var alpha = (uint)(color.A * 255);
            return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
        }

        public SystemDiagnosticsDeviceReportSyncContainer(HtmlWebViewSource htmlSource, OrderedObservableCollection<ILogicalDevice> collection, IContainerDataSource dataSource, Func<ILogicalDevice, bool>? deviceFilter = null)
            : base(collection, dataSource)
        {
            _htmlSource = htmlSource;
            DeviceFilter = deviceFilter ?? DeviceFilterAllDevices;
            
            var diagnosticsHtmlTemplate = LoadDiagnosticsHtmlTemplate();
            _htmlSource.Html = diagnosticsHtmlTemplate;

            try
            {
                _diagnosticsHtmlTemplateExecute = Handlebars.Compile(diagnosticsHtmlTemplate);
            }
            catch (Exception ex)
            {
                TaggedLog.Warning(LogTag, $"Unable to build template {ex.Message}");
            }

            DataSourceSync();
        }

        private readonly struct LogicalDeviceCollectionWrapper
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public IEnumerable<ILogicalDevice> Collection { get; }

            public LogicalDeviceCollectionWrapper(IEnumerable<ILogicalDevice> collection)
            {
                Collection = collection;
            }
        }

        public static string MakeHtml(IEnumerable<ILogicalDevice> logicalDevices)
        {
            try
            {
                if( logicalDevices == null )
                    throw new ArgumentNullException(nameof(logicalDevices));

                var diagnosticsHtmlTemplate = LoadDiagnosticsHtmlTemplate();
                var diagnosticsHtmlTemplateExecute = Handlebars.Compile(diagnosticsHtmlTemplate);
                var html = diagnosticsHtmlTemplateExecute(new LogicalDeviceCollectionWrapper(logicalDevices));
                return html;

            }
            catch( Exception ex )
            {
                TaggedLog.Warning(LogTag, $"Unable to make HTML {ex.Message}");
                return HtmlEmptyTemplate;
            }
        }


        protected override Func<ILogicalDevice, bool> CurrentDataSourceFilter => DataSourceFilter;

        #region Device Filtering
        public static readonly Func<ILogicalDevice, bool> DeviceFilterAllDevices = device => true;

        private Func<ILogicalDevice, bool> _deviceFilter = DeviceFilterAllDevices;
        public Func<ILogicalDevice, bool> DeviceFilter
        {
            get => _deviceFilter;
            set {
                var newFilter = value ?? DeviceFilterAllDevices;
                if (_deviceFilter == newFilter)
                    return;

                _deviceFilter = newFilter;
                DataSourceSync();
            }
        }

        /* DataSourceFilter can end up being called from the base constructor, before we have had a change to setup _navigationService */
        private bool DataSourceFilter(ILogicalDevice device) => FilterForSelectedRv(device) && DeviceFilter(device);
        #endregion

        protected override Func<ILogicalDevice, ILogicalDevice> CurrentViewModelFactory => (logicalDevice) => logicalDevice;

        public override void OnSyncEnd(OrderedObservableCollection<ILogicalDevice> collection)
        {
            if( _diagnosticsHtmlTemplateExecute == null )
                return;

            try
            {
                TaggedLog.Debug(LogTag, $"Build Diagnostics HTML");
                _htmlSource.Html = _diagnosticsHtmlTemplateExecute(this);
            }
            catch (Exception ex)
            {
                TaggedLog.Warning(LogTag, $"Unable to apply template {ex.Message}");
            }
        }

        // Returns HtmlEmptyTemplate if the template wasn't able to be loaded.
        private static string LoadDiagnosticsHtmlTemplate()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(SystemDiagnosticsDataTemplateFilename));
                if (string.IsNullOrEmpty(resourceName))
                    throw new ArgumentException($"Missing HTML template file {SystemDiagnosticsDataTemplateFilename}");

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                TaggedLog.Warning(LogTag, $"Unable to load diagnostics template, using default.  {ex.Message}");
                return HtmlEmptyTemplate;
            }
        }
    }
}