using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;
using System.Threading;

namespace SmartPower.UserInterface.Common.ActionSheet
{
    public class ActionSheetConfig
    {
        public Func<object, string> Converter { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public IList<object> Options { get; }
        public object? SelectedOption { get; }

        public ActionSheetConfig(string title, IList<object> options, object selectedOption, string subtitle = "", Func<object, string>? converter = null)
        {
            Title = title;
            Subtitle = subtitle;
            Options = options;
            SelectedOption = selectedOption;
            Converter = converter ?? (obj => obj.ToString());
        }
    }

    public class ActionSheetResult
    {
        public object? SelectedOption { get; }
        public bool Canceled { get; }

        public ActionSheetResult(object? selectedOption, bool canceled)
        {

            SelectedOption = selectedOption;
            Canceled = canceled;
        }
    }

    public class ActionSheetPageViewModel : BaseViewModel, IViewModelStartStop
    {
        public const string ActionSheetResultKey = "ASRK";
        public const string ActionSheetConfigKey = "ASCK";
        private IList<object>? _options;

        public ActionSheetPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
        }

        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string? _subtitle;
        public string? Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        private ObservableCollection<Option<object>> _items = new ObservableCollection<Option<object>>();

        public ObservableCollection<Option<object>> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private Option<object>? _itemSelected;

        public Option<object>? ItemSelected
        {
            get => _itemSelected;
            set
            {
                if (!SetProperty(ref _itemSelected, value)) return;
                OnBackWithResult(new ActionSheetResult(_itemSelected?.Value, false));
            }
        }

        public Task OnStartAsync(INavigationParameters? parameters, CancellationToken startStopCancellationToken)
        {
            try
            {
                var config = (ActionSheetConfig)parameters![ActionSheetConfigKey];

                _title = config.Title;
                _subtitle = config.Subtitle;

                _options = config.Options;
                for (var o = 0; o < _options.Count; o++)
                {
                    var option = _options[o];
                    var item = new Option<object>(config.Converter(option), o, option);
                    item.CellCommand = new Command(o1 => { ItemSelected = item; });

                    if (config.SelectedOption != null && string.Equals(config.Converter(option),
                            config.Converter(config.SelectedOption)))
                    {
                        item.IsSelected = true;
                        _itemSelected = item;
                    }

                    _items.Add(item);
                }

                RaiseAllPropertiesChanged();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return Task.CompletedTask;

        }

        public void OnStop()
        {

        }

        private AsyncCommand? _onBackPressed = null;
        public AsyncCommand OnBackPressed => _onBackPressed ??= new AsyncCommand(
            () => OnBackWithResult(new ActionSheetResult(_itemSelected, true)), allowsMultipleExecutions: false);

        private async Task OnBackWithResult(object parameter)
        {
            var navParameters = new NavigationParameters
            {
                { ActionSheetResultKey, parameter }
            };

            await NavigationService.GoBackAsync(navParameters);
        }
    }
}
