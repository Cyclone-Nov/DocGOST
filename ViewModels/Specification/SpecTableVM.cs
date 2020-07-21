using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels.ElementsList
{
    class SpecTableVM
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        private ObservableProperty<string> Format = new ObservableProperty<string>();

        /// <summary>
        /// Зона
        /// </summary>
        private ObservableProperty<string> Zone = new ObservableProperty<string>();

        /// <summary>
        /// Не знаю что именно должно быть в позиции...
        /// </summary>
        enum PositionEnum
        {
            Auto, 
            NonAuto
        }

        /// <summary>
        /// Количество
        /// </summary>
        private ObservableProperty<PositionEnum> Position = new ObservableProperty<PositionEnum>();
        
        /// <summary>
        /// Обозначение
        /// </summary>
        private ObservableProperty<string> Designation = new ObservableProperty<string>();

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
