using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class ElementsEntryVM
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        public ObservableProperty<string> PositionDesignation = new ObservableProperty<string>();

        /// <summary>
        /// Наименование
        /// </summary>
        public ObservableProperty<string> Name = new ObservableProperty<string>();

        /// <summary>
        /// Количество
        /// </summary>
        public ObservableProperty<uint> Quantity = new ObservableProperty<uint>();

        /// <summary>
        /// Примечание
        /// </summary>
        public ObservableProperty<string> Note = new ObservableProperty<string>();

    }
}
