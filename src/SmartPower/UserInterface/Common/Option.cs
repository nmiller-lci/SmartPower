using System;
using System.Collections.Generic;
using System.Text;
using IDS.UI.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IDS.Portable.Common;

namespace SmartPower.UserInterface.Common
{
    public class Option : CommonNotifyPropertyChanged, ISingleSelectionCellViewModel
    {
        public Option(string text, int index, bool hidden = false)
        {
            Text = text;
            this._index = index;
            _hidden = hidden;
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => SetBackingField(ref _text, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetBackingField(ref _isEnabled, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetBackingField(ref _isSelected, value);
        }

        private int _index;
        public int Index
        {
            get => _index;
            set => SetBackingField(ref _index, value);
        }

        private bool _hidden;
        public bool Hidden
        {
            get => _hidden;
            set => SetBackingField(ref _hidden, value);
        }

        public void SetText(string newText)
        {
            _text = newText;
            OnPropertyChanged(nameof(Text));
        }

        //Not used
        public ICommand CellCommand { get; set; }

        public bool IsFavorite { get; set; }
        public string Category { get; } = string.Empty;

        public string Description { get; set; }

        public bool ShowInformationIcon => throw new NotImplementedException();

        public ICommand? InfoCommand => throw new NotImplementedException();

        public int CompareTo(object obj)
        {
            return obj is Option option ? string.Compare(option.Text, Text, System.StringComparison.Ordinal) : -1;
        }

        public int CompareTo(ICellViewModel other)
        {
            return CompareTo((object)other);
        }

        public int CompareTo(ISingleSelectionCellViewModel other)
        {
            return CompareTo((object)other);
        }
    }

    public class Option<TValue> : Option
    {
        public Option(string text, int index, bool hidden = false) : base(text, index, hidden) { }

        public Option(string text, int index, TValue value, bool hidden = false) : base(text, index, hidden)
        {
            Value = value;
        }

        public TValue Value { get; set; }
    }

    public class OrderedOption : Option, IComparer<Option>, IComparer<OrderedOption>, IComparable<OrderedOption>
    {
        public OrderedOption(string text, int index) : base(text, index) { }

        public virtual int Compare(Option x, Option y) => string.Compare(x?.Text ?? "", y?.Text ?? "", System.StringComparison.Ordinal);
        public virtual int Compare(OrderedOption x, OrderedOption y) => string.Compare(x?.Text ?? "", y?.Text ?? "", System.StringComparison.Ordinal);
        public virtual int CompareTo(OrderedOption other) => string.Compare(Text, other.Text, System.StringComparison.Ordinal);
    }

    public class OrderedOption<TValue> : Option<TValue>, IComparable<Option<TValue>>
        where TValue : IComparable<TValue>
    {
        public OrderedOption(string text, int index) : base(text, index) { }

        public OrderedOption(string text, int index, TValue value) : base(text, index, value) { }

        public virtual int CompareTo(Option<TValue> other) => Value.CompareTo(other.Value);
    }
}
