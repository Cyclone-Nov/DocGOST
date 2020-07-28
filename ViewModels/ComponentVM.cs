using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class ComponentVM
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Code { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Format { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Zone { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Entry { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Manufacturer { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> WhereIncluded { get; } = new ObservableProperty<string>();
        public ObservableProperty<uint> Position { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountProduct { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountKits { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountReg { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> Count { get; } = new ObservableProperty<uint>();
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Description { get; } = new ObservableProperty<string>();

        public ComponentVM()
        {
            CountProduct.PropertyChanged += CountProductChanged;
            CountKits.PropertyChanged += CountProductChanged;
            CountReg.PropertyChanged += CountProductChanged;
        }

        private void CountProductChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Count.Value = CountProduct.Value + CountKits.Value + CountReg.Value;
        }
    }
}
