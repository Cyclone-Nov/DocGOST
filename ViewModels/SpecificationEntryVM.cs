using System.Security.Policy;

namespace GostDOC.ViewModels
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
        /// Количество
        /// </summary>
        public ObservableProperty<string> Position { get; } = new ObservableProperty<string>();

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