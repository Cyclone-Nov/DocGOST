﻿using System;
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

        /// <summary>
        /// Наименование компонента
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Наименование компонента
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public ObservableProperty<string> Zone { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Код продукции
        /// </summary>
        /// <value>
        /// The code.
        /// </value>
        public ObservableProperty<string> Code { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Формат документа (если компонента является документом)
        /// </summary>
        /// <value>
        /// The format.
        /// </value>
        public ObservableProperty<DocumentFormats> Format { get; } = new ObservableProperty<DocumentFormats>();
        /// <summary>
        /// Наличиен данного компонента в сборке
        /// </summary>
        /// <value>
        /// The entry.
        /// </value>
        public ObservableProperty<string> Entry { get; } = new ObservableProperty<string>();
        /// <summary>
        /// производитель компонента
        /// </summary>
        /// <value>
        /// The manufacturer.
        /// </value>
        public ObservableProperty<string> Manufacturer { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Поставщик компонента
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public ObservableProperty<string> Supplier { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Позиция компонента на чертеже и в документе спецификация
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public ObservableProperty<int> Position { get; } = new ObservableProperty<int>(0);
        /// <summary>
        /// Количество на изделие
        /// </summary>
        /// <value>
        /// The count dev.
        /// </value>
        public ObservableProperty<float> CountDev { get; } = new ObservableProperty<float>(1);
        /// <summary>
        /// Количество на комплект
        /// </summary>
        /// <value>
        /// The count set.
        /// </value>
        public ObservableProperty<float> CountSet { get; } = new ObservableProperty<float>(0);
        /// <summary>
        /// Количество на регулир.
        /// </summary>
        /// <value>
        /// The count reg.
        /// </value>
        public ObservableProperty<float> CountReg { get; } = new ObservableProperty<float>(0);
        /// <summary>
        /// Количество
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public ObservableProperty<float> Count { get; } = new ObservableProperty<float>(0);
        /// <summary>
        /// позиционное обозначение (для радиокомпонентов, узлови и деталей)
        /// </summary>
        /// <value>
        /// The designator identifier.
        /// </value>
        public ObservableProperty<string> DesignatorID { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Примечание
        /// </summary>
        /// <value>
        /// The note.
        /// </value>
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Обозначение
        /// </summary>
        /// <value>
        /// The sign.
        /// </value>
        public ObservableProperty<string> Sign { get; } = new ObservableProperty<string>();
        /// <summary>
        /// Децимальный номер узла или сборки, куда входит компонент
        /// </summary>
        /// <value>
        /// The where included.
        /// </value>
        public ObservableProperty<string> WhereIncluded { get; } = new ObservableProperty<string>();
        public bool IsReadOnly { get; set; } = false;
        public string MaterialGroup { get; set; }

        private class ComponentMemento
        {
            public Guid Guid { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public DocumentFormats Format { get; set; }
            public string Entry { get; set; }
            public string Manufacturer { get; set; }
            public int Position { get; set; }
            public float CountDev { get; set; }
            public float CountSet { get; set; }
            public float CountReg { get; set; }
            public string DesignatorID { get; set; }
            public string Note { get; set; }
            public string Sign { get; set; }
            public string WhereIncluded { get; set; }
            public string Zone { get; set; }            
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
                    Sign = Sign.Value,
                    WhereIncluded = WhereIncluded.Value,
                    Zone = Zone.Value
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
                Sign.Value = memento.Sign;
                WhereIncluded.Value = memento.WhereIncluded;
                Zone.Value = memento.Zone;
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
            string strForamt = GetValue(Constants.ComponentFormat, aComponent);            
            Format.Value = string.IsNullOrEmpty(strForamt) ? 
                            DocumentFormats.Empty : 
                            Common.Converters.DescriptionToEnum<DocumentFormats>(strForamt, out var isValid);
            
            Entry.Value = GetValue(Constants.ComponentDoc, aComponent);
            Manufacturer.Value = GetValue(Constants.ComponentSupplier, aComponent);

            CountDev.Value = Convert(GetValue(Constants.ComponentCountDev, aComponent));
            CountSet.Value = Convert(GetValue(Constants.ComponentCountSet, aComponent));
            CountReg.Value = Convert(GetValue(Constants.ComponentCountReg, aComponent));

            Count.Value = aComponent.Count;

            Note.Value = GetValue(Constants.ComponentNote, aComponent);
            Sign.Value = GetValue(Constants.ComponentSign, aComponent);

            DesignatorID.Value = GetValue(Constants.ComponentDesignatorID, aComponent);
            WhereIncluded.Value = GetValue(Constants.ComponentWhereIncluded, aComponent);

            Position.Value = (int)Convert(GetValue(Constants.ComponentPosition, aComponent));
            Zone.Value = GetValue(Constants.ComponentZone, aComponent);

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
