using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.DataPreparation
{
    /// <summary>
    /// выдать имя компонента для словаря позиций (используется для спецификации)
    /// </summary>
    public static class DataPreparationUtils
    {
        /// <summary>
        /// Gets the name for position dictionary.
        /// </summary>
        /// <param name="aComponent">a component.</param>
        /// <returns></returns>
        public static string GetNameForPositionDictionary(Component aComponent)
        {
            string designation = aComponent.GetProperty(Constants.ComponentSign);
            string name = aComponent.GetProperty(Constants.ComponentName);
            return ($"{name} {designation}").Trim();
        }

    }
}
