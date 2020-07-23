
namespace GostDOC.Models
{
    class SpecificationEntry
    {
        /// <summary>
        /// Позиционное обозначение
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Зона
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Количество
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Обозначение
        /// </summary>
        public string Designation { get; set; }

        /// <summary>
        /// Наименование
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Количество
        /// </summary>
        public uint Quantity { get; set; }

        /// <summary>
        /// Примечание
        /// </summary>
        public string Note { get; set; }
    }
}