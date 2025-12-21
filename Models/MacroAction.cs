using System;
using System.ComponentModel;

namespace MacroApp.Models
{
    public enum ActionType
    {
        MouseLeftDown,
        MouseLeftUp,
        MouseRightDown,
        MouseRightUp,
        MouseMiddleDown,
        MouseMiddleUp,
        KeyDown,
        KeyUp,
        Delay
    }

    public class MacroAction : INotifyPropertyChanged
    {
        private ActionType _actionType;
        private int _duration;
        private int _delay;
        private string _keyCode;

        public ActionType ActionType
        {
            get => _actionType;
            set
            {
                _actionType = value;
                OnPropertyChanged(nameof(ActionType));
            }
        }

        public int Duration
        {
            get => _duration;
            set
            {
                _duration = Math.Max(0, value);
                OnPropertyChanged(nameof(Duration));
            }
        }

        public int Delay
        {
            get => _delay;
            set
            {
                _delay = Math.Max(0, value);
                OnPropertyChanged(nameof(Delay));
            }
        }

        public string KeyCode
        {
            get => _keyCode;
            set
            {
                _keyCode = value;
                OnPropertyChanged(nameof(KeyCode));
            }
        }

        public string DisplayName
        {
            get
            {
                return ActionType switch
                {
                    ActionType.MouseLeftDown => "Mouse Left Down",
                    ActionType.MouseLeftUp => "Mouse Left Up",
                    ActionType.MouseRightDown => "Mouse Right Down",
                    ActionType.MouseRightUp => "Mouse Right Up",
                    ActionType.MouseMiddleDown => "Mouse Middle Down",
                    ActionType.MouseMiddleUp => "Mouse Middle Up",
                    ActionType.KeyDown => $"Key Down ({KeyCode})",
                    ActionType.KeyUp => $"Key Up ({KeyCode})",
                    ActionType.Delay => $"Delay ({Delay}ms)",
                    _ => ActionType.ToString()
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MacroAction Clone()
        {
            return new MacroAction
            {
                ActionType = this.ActionType,
                Duration = this.Duration,
                Delay = this.Delay,
                KeyCode = this.KeyCode
            };
        }
    }
}