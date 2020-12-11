using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.ViewModels
{
    class BaseChanged
    {

        /// <summary>
        /// уникальный идентификатор поставщика
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// признак что произошло изменение какого либо значения структуры
        /// </summary>
        /// <value>
        /// The supplier.
        /// </value>
        public bool IsChanged { get; set; } = false;

        protected void PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }
    }
}
