using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Interfaces;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class ComponentVM : IMemento<object>
    {
        public Guid Guid { get; private set; } = Guid.NewGuid();
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

        private class ComponentMemento
        {
            public Guid Guid { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Format { get; set; }
            public string Entry { get; set; }
            public string Manufacturer { get; set; }
            public int Position { get; set; }
            public uint CountDev { get; set; }
            public uint CountSet { get; set; }
            public uint CountReg { get; set; }
            public string DesignatorID { get; set; }
            public string Note { get; set; }
            public string NoteSP { get; set; }
            public string Sign { get; set; }
            public string WhereIncluded { get; set; }
        }

        public object Memento
        {
            get
            {
                return new ComponentMemento()
                {
                    Guid = Guid,
                    Name = Name.Value,
                    Code = Code.Value,
                    Format = Format.Value,
                    Entry = Entry.Value,
                    Manufacturer = Manufacturer.Value,
                    Position = Position.Value,
                    CountDev = CountDev.Value,
                    CountSet = CountSet.Value,
                    CountReg = CountReg.Value,
                    DesignatorID = DesignatorID.Value,
                    Note = Note.Value,
                    NoteSP = NoteSP.Value,
                    Sign = Sign.Value,
                    WhereIncluded = WhereIncluded.Value
                };
            }

            set
            {
                ComponentMemento memento = value as ComponentMemento;
                Guid = memento.Guid;
                Name.Value = memento.Name;
                Code.Value = memento.Code;
                Format.Value = memento.Format;
                Entry.Value = memento.Entry;
                Manufacturer.Value = memento.Manufacturer;
                Position.Value = memento.Position;
                CountDev.Value = memento.CountDev;
                CountSet.Value = memento.CountSet;
                CountReg.Value = memento.CountReg;
                DesignatorID.Value = memento.DesignatorID;
                Note.Value = memento.Note;
                NoteSP.Value = memento.NoteSP;
                Sign.Value = memento.Sign;
                WhereIncluded.Value = memento.WhereIncluded;
            }
        }

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

            DesignatorID.Value = GetValue(Constants.DesignatiorID, aComponent);
            NoteSP.Value = string.IsNullOrEmpty(Note.Value) ? DesignatorID.Value : Note.Value;
            WhereIncluded.Value = GetValue(Constants.ComponentWhereIncluded, aComponent);

            if (CountDev.Value == 0)
            {
                CountDev.Value = aComponent.Count;
            }
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
