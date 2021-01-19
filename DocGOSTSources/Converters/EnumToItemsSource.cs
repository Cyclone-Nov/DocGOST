using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace GostDOC.Converters
{
    /// <summary>
    /// расширение разметки для получения списка значение description для атрибута DescriptionAttribute из полей типа Enum
    /// </summary>
    /// <seealso cref="System.Windows.Markup.MarkupExtension" />
    public class EnumToItemsSource : MarkupExtension
    {
        private readonly Type _type;

        public EnumToItemsSource(Type type)
        {
            _type = type;
        }

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _type.GetMembers().SelectMany(member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).
                                      Cast<DescriptionAttribute>()).
                                      Select(x => x.Description).ToList();
        }
    }
}
