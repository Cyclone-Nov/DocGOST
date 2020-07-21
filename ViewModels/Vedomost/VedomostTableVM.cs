using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels.Vedomost
{
    class VedomostTableVM
    {
        /// <summary>
        /// Наименование
        /// </summary>
        private ObservableProperty<string> Name = new ObservableProperty<string>();
        
        /// <summary>
        /// Код
        /// </summary>
        private ObservableProperty<string> Code = new ObservableProperty<string>();
        
        /// <summary>
        /// Код
        /// </summary>
        private ObservableProperty<string> Document = new ObservableProperty<string>();
        
        /// <summary>
        /// Код
        /// </summary>
        private ObservableProperty<string> Supplier = new ObservableProperty<string>();
        
        /// <summary>
        /// 
        /// </summary>
        private ObservableProperty<string> WhereIncluded = new ObservableProperty<string>();

        /// <summary>
        /// Количество на изделие
        /// </summary>
        private ObservableProperty<uint> QuantityOnProduct = new ObservableProperty<uint>();
        
        /// <summary>
        /// Количество на комплекты
        /// </summary>
        private ObservableProperty<uint> QuantityOnSets = new ObservableProperty<uint>();

        /// <summary>
        /// Количество на регулир.
        /// </summary>
        private ObservableProperty<uint> QuantityOnReg = new ObservableProperty<uint>();

        /// <summary>
        /// Всего
        /// </summary>
        private ObservableProperty<uint> Sum = new ObservableProperty<uint>();

        /// <summary>
        /// Примечание
        /// </summary>
        private ObservableProperty<string> Note = new ObservableProperty<string>();
    }
}
