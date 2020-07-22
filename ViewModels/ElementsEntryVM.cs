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
        public ObservableProperty<string> PositionDesignation { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Наименование
        /// </summary>
        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Количество
        /// </summary>
        public ObservableProperty<uint> Quantity { get; } = new ObservableProperty<uint>();

        /// <summary>
        /// Примечание
        /// </summary>
        public ObservableProperty<string> Note { get; } = new ObservableProperty<string>();

    }
}
