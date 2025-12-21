using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MacroApp.Models
{
    public enum MacroMode
    {
        RepeatWhileHolding,
        NoRepeat,
        Toggle,
        Sequence
    }

    public class KeySequenceTrigger : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _activationKey;
        private string _preHoldKey;
        private string _holdKey;
        private string _releaseKey;
        private int _delay;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string ActivationKey
        {
            get => _activationKey;
            set
            {
                _activationKey = value;
                OnPropertyChanged(nameof(ActivationKey));
            }
        }

        public string PreHoldKey
        {
            get => _preHoldKey;
            set
            {
                _preHoldKey = value;
                OnPropertyChanged(nameof(PreHoldKey));
            }
        }

        public string HoldKey
        {
            get => _holdKey;
            set
            {
                _holdKey = value;
                OnPropertyChanged(nameof(HoldKey));
            }
        }

        public string ReleaseKey
        {
            get => _releaseKey;
            set
            {
                _releaseKey = value;
                OnPropertyChanged(nameof(ReleaseKey));
            }
        }

        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                OnPropertyChanged(nameof(Delay));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class KeyboardToMouseMacro : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _triggerKey;
        private bool _primaryClick;
        private bool _secondaryClick;
        private MacroMode _mode;
        private ObservableCollection<MacroAction> _actions;

        public KeyboardToMouseMacro()
        {
            _actions = new ObservableCollection<MacroAction>();
            _mode = MacroMode.RepeatWhileHolding;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string TriggerKey
        {
            get => _triggerKey;
            set
            {
                _triggerKey = value;
                OnPropertyChanged(nameof(TriggerKey));
            }
        }

        public bool PrimaryClick
        {
            get => _primaryClick;
            set
            {
                _primaryClick = value;
                OnPropertyChanged(nameof(PrimaryClick));
            }
        }

        public bool SecondaryClick
        {
            get => _secondaryClick;
            set
            {
                _secondaryClick = value;
                OnPropertyChanged(nameof(SecondaryClick));
            }
        }

        public MacroMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }

        public ObservableCollection<MacroAction> Actions
        {
            get => _actions;
            set
            {
                _actions = value;
                OnPropertyChanged(nameof(Actions));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MouseToKeyboardMacro : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _mouseButton;
        private string _targetKey;
        private MacroMode _mode;
        private ObservableCollection<MacroAction> _actions;

        public MouseToKeyboardMacro()
        {
            _actions = new ObservableCollection<MacroAction>();
            _mode = MacroMode.RepeatWhileHolding;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string MouseButton
        {
            get => _mouseButton;
            set
            {
                _mouseButton = value;
                OnPropertyChanged(nameof(MouseButton));
            }
        }

        public string TargetKey
        {
            get => _targetKey;
            set
            {
                _targetKey = value;
                OnPropertyChanged(nameof(TargetKey));
            }
        }

        public MacroMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }

        public ObservableCollection<MacroAction> Actions
        {
            get => _actions;
            set
            {
                _actions = value;
                OnPropertyChanged(nameof(Actions));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AutoBuildConfiguration : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string _placeBuilding;
        private string _wall;
        private string _stairs;
        private string _floor;
        private string _roof;
        private double _delay;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string PlaceBuilding
        {
            get => _placeBuilding;
            set
            {
                _placeBuilding = value;
                OnPropertyChanged(nameof(PlaceBuilding));
            }
        }

        public string Wall
        {
            get => _wall;
            set
            {
                _wall = value;
                OnPropertyChanged(nameof(Wall));
            }
        }

        public string Stairs
        {
            get => _stairs;
            set
            {
                _stairs = value;
                OnPropertyChanged(nameof(Stairs));
            }
        }

        public string Floor
        {
            get => _floor;
            set
            {
                _floor = value;
                OnPropertyChanged(nameof(Floor));
            }
        }

        public string Roof
        {
            get => _roof;
            set
            {
                _roof = value;
                OnPropertyChanged(nameof(Roof));
            }
        }

        public double Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                OnPropertyChanged(nameof(Delay));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MacroConfiguration : INotifyPropertyChanged
    {
        public KeySequenceTrigger KeySequence { get; set; }
        public KeyboardToMouseMacro KeyboardToMouse { get; set; }
        public MouseToKeyboardMacro MouseToKeyboard { get; set; }
        public AutoBuildConfiguration AutoBuild { get; set; }

        public MacroConfiguration()
        {
            KeySequence = new KeySequenceTrigger();
            KeyboardToMouse = new KeyboardToMouseMacro();
            MouseToKeyboard = new MouseToKeyboardMacro();
            AutoBuild = new AutoBuildConfiguration();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}