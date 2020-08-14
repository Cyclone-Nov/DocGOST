using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class ComponentVM
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Code { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Format { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Entry { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Manufacturer { get; } = new ObservableProperty<string>();
        public ObservableProperty<int> Position { get; } = new ObservableProperty<int>();
        public ObservableProperty<uint> CountDev { get; } = new ObservableProperty<uint>(1);
        public ObservableProperty<uint> CountSet { get; } = new ObservableProperty<uint>(0);
        public ObservableProperty<uint> CountReg { get; } = new ObservableProperty<uint>(0);
        public ObservableProperty<uint> Count { get; } = new ObservableProperty<uint>(0);
        public ObservableProperty<string> DesignatorID { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> NoteSP { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Sign { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> WhereIncluded { get; } = new ObservableProperty<string>();

        public ComponentVM()
        {
            Init();
        }

        public ComponentVM(Component aComponent)
        {
            Init(); 

            Guid = aComponent.Guid;

            Name.Value = GetValue(Constants.ComponentName, aComponent);
            Code.Value = GetValue(Constants.ComponentProductCode, aComponent);
            Format.Value = GetValue(Constants.ComponentFormat, aComponent);
            Entry.Value = GetValue(Constants.ComponentDoc, aComponent);
            Manufacturer.Value = GetValue(Constants.ComponentSupplier, aComponent);

            CountDev.Value = Convert(GetValue(Constants.ComponentCountDev, aComponent));
            CountSet.Value = Convert(GetValue(Constants.ComponentCountSet, aComponent));
            CountReg.Value = Convert(GetValue(Constants.ComponentCountReg, aComponent));

            Note.Value = GetValue(Constants.ComponentNote, aComponent);
            Sign.Value = GetValue(Constants.ComponentSign, aComponent);

            DesignatorID.Value = GetValue(Constants.ComponentDesignatiorID, aComponent);
            NoteSP.Value = string.IsNullOrEmpty(DesignatorID.Value) ? Note.Value : DesignatorID.Value;
            WhereIncluded.Value = GetValue(Constants.ComponentWhereIncluded, aComponent);

            CountDev.Value = aComponent.Count;
        }

        private uint Convert(string aValue)
        {
            uint result = 0;
            uint.TryParse(aValue, out result);
            return result;
        }

        private void Init()
        {
            CountDev.PropertyChanged += CountProductChanged;
            CountSet.PropertyChanged += CountProductChanged;
            CountReg.PropertyChanged += CountProductChanged;
        }

        private string GetValue(string aName, Component aComponent)
        {
            string result;
            if (aComponent.Properties.TryGetValue(aName, out result))
            {
                return result;
            }
            return string.Empty;
        }

        private void CountProductChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Count.Value = CountDev.Value + CountSet.Value + CountReg.Value;
        }
    }
}
