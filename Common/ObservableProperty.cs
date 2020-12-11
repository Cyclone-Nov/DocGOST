using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GostDOC
{
    public class ObservableProperty<T> : INotifyPropertyChanged
    {
        private T _value;
        virtual public T Value
        {
            get { return _value; }
            set
            {
                if (_value == null || !_value.Equals(value))                
                {
                    _value = value;
                    NotifyPropertyChanged("Value");
                }                
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        internal void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableProperty()
        {
            _value = default(T);
        }

        public ObservableProperty(T aValue)
        {
            _value = aValue;
        }
    }

    public class ObservablePropertyLimits<T> : ObservableProperty<T> where T : IComparable<T>
    {
        private T _min;
        private T _max;
        override public T Value
        {
            get { return base.Value; }
            set
            {
                if (Comparer<T>.Default.Compare(value, _min) < 0)
                {
                    base.Value = _min;
                }
                else if (Comparer<T>.Default.Compare(value, _max) > 0)
                {
                    base.Value = _max;
                }
                else
                {
                    base.Value = value;
                }
            }
        }

        public ObservablePropertyLimits(T aMin, T aMax)
        {
            _min = aMin;
            _max = aMax;
        }

        public ObservablePropertyLimits(T aMin, T aMax, T aValue)
        {
            _min = aMin;
            _max = aMax;
            Value = aValue;
        }
    }
}
