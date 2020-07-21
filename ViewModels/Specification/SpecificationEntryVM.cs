using System.Security.Policy;

namespace GostDOC.ViewModels.Specification
{
    class SpecificationEntryVM
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        public ObservableProperty<string> Format { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Зона
        /// </summary>
        public ObservableProperty<string> Zone { get; } = new ObservableProperty<string>();

        /// <summary>
        /// Не знаю что именно должно быть в позиции...
        /// </summary>
        public enum PositionEnum
        {
            Auto,
            NonAuto
        }

        /// <summary>
        /// Количество
        /// </summary>
        public ObservableProperty<PositionEnum> Position { get; } = new ObservableProperty<PositionEnum>();

        /// <summary>
        /// Обозначение
        /// </summary>
        public ObservableProperty<string> Designation { get; } = new ObservableProperty<string>();

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