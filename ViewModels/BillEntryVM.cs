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
        public ObservableProperty<string> Name = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Code = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Document = new ObservableProperty<string>();

        /// <summary>
        /// Код
        /// </summary>
        public ObservableProperty<string> Supplier = new ObservableProperty<string>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableProperty<string> WhereIncluded = new ObservableProperty<string>();

        /// <summary>
        /// Количество на изделие
        /// </summary>
        public ObservableProperty<uint> QuantityOnProduct = new ObservableProperty<uint>();

        /// <summary>
        /// Количество на комплекты
        /// </summary>
        public ObservableProperty<uint> QuantityOnSets = new ObservableProperty<uint>();

        /// <summary>
        /// Количество на регулир.
        /// </summary>
        public ObservableProperty<uint> QuantityOnReg = new ObservableProperty<uint>();

        /// <summary>
        /// Всего
        /// </summary>
        public ObservableProperty<uint> Sum = new ObservableProperty<uint>();

        /// <summary>
        /// Примечание
        /// </summary>
        public ObservableProperty<string> Note = new ObservableProperty<string>();
    }
}
