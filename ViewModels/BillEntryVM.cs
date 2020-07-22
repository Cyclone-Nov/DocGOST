using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class BillEntryVM
    {
        /// <summary>
        /// Наименование
        /// </summary>
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Code { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Document { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Supplier { get; } = new ObservableProperty<string>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableProperty<string> WhereIncluded { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Количество на изделие
        /// </summary>
        public ObservableProperty<uint> QuantityOnProduct { get; } = new ObservableProperty<uint>();

        /// <summary>
        /// Количество на комплекты
        /// </summary>
        public ObservableProperty<uint> QuantityOnSets { get; } = new ObservableProperty<uint>();

        /// <summary>
        /// Количество на регулир.
        /// </summary>
        public ObservableProperty<uint> QuantityOnReg { get; } = new ObservableProperty<uint>();

        /// <summary>
        /// Всего
        /// </summary>
        public ObservableProperty<uint> Sum { get; } = new ObservableProperty<uint>();

        /// <summary>
        /// Примечание
        /// </summary>
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();
    }
}
