using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels.ElementsList
{
    class ElementsListTableVM
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        private ObservableProperty<string> PositionDesignation = new ObservableProperty<string>();

        /// <summary>
        /// Наименование
        /// </summary>
        private ObservableProperty<string> Name = new ObservableProperty<string>();
        
        /// <summary>
        /// Количество
        /// </summary>
        private ObservableProperty<uint> Quantity = new ObservableProperty<uint>();
        
        /// <summary>
        /// Примечание
        /// </summary>
        private ObservableProperty<string> Note = new ObservableProperty<string>();

    }
}
