using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class ComponentVM
    {
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Code { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Format { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Entry { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Manufacturer { get; } = new ObservableProperty<string>();
        public ObservableProperty<uint> Position { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountProduct { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountKits { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> CountReg { get; } = new ObservableProperty<uint>();
        public ObservableProperty<uint> Count { get; } = new ObservableProperty<uint>();
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();

        public ComponentVM()
        {
            Init();
        }

        public ComponentVM(Component aComponent)
        {
            Name.Value = GetValue("Наименование", aComponent);
            Code.Value = GetValue("Код продукции", aComponent);
            Format.Value = GetValue("Формат", aComponent);
            Entry.Value = GetValue("Документ на поставку", aComponent);
            Manufacturer.Value = GetValue("Поставщик", aComponent);

            CountProduct.Value = Convert(GetValue("Количество на изд.", aComponent));
            CountKits.Value = Convert(GetValue("Количество в комп.", aComponent));
            CountReg.Value = Convert(GetValue("Количество на рег.", aComponent));

            Note.Value = GetValue("Примечание", aComponent);

            Init();
        }

        private uint Convert(string aValue)
        {
            uint result = 0;
            uint.TryParse(aValue, out result);
            return result;
        }

        private void Init()
        {
            CountProduct.PropertyChanged += CountProductChanged;
            CountKits.PropertyChanged += CountProductChanged;
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
            Count.Value = CountProduct.Value + CountKits.Value + CountReg.Value;
        }
    }
}
