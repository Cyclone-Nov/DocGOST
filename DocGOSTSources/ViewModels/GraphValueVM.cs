using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Interfaces;
using GostDOC.Models;
using GostDOC.Common;

namespace GostDOC.ViewModels
{
    class GraphValueVM : IMemento<object>
    {
        public enum ItemType
        {
            ComboBox,
            Text
        }

        private class GraphValueMemento
        {
            public string Name { get; set; }
            public string Text { get; set; }
            public List<string> Items { get; } = new List<string>();
            public ItemType GraphType { get; set; }
        }

        public object Memento
        {
            get
            {
                var result = new GraphValueMemento()
                {
                    Name = Name.Value,
                    Text = Text.Value,
                    GraphType = GraphType
                };
                result.Items.AddRange(Items);
                return result;
            }

            set
            {
                GraphValueMemento memento = value as GraphValueMemento;
                Name.Value = memento.Name;
                Text.Value = memento.Text;
                GraphType = memento.GraphType;
                Items.AddRange(memento.Items);
            }
        }

        public ObservableProperty<string> Name { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> Text { get; } = new ObservableProperty<string>();
        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();
        public ItemType GraphType { get; set; }

        public GraphValueVM()
        {
        }

        public GraphValueVM(string aName, string aText)
        {
            if (IsLiteraField(aName))
            {
                aName = ConvertToLiteraName(aName);
                GraphType = ItemType.ComboBox;
                foreach (var it in Constants.LiterasList)
                    Items.Add(it);
            }
            else
            {
                GraphType = ItemType.Text;
            }

            Name.Value = aName;
            Text.Value = aText;
        }

        /// <summary>
        /// определение что данное поле относится к литерам
        /// </summary>
        /// <param name="aFieldName">Name of a field.</param>
        /// <returns>
        ///   <c>true</c> if [is litera field] [the specified a field name]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsLiteraField(string aFieldName)
        {
            return aFieldName.Contains(Constants.LiteraName);
        }

        /// <summary>
        /// конвертирование названия поля с литерой в допустимое название
        /// </summary>
        /// <param name="aName">a name.</param>
        /// <returns></returns>
        private string ConvertToLiteraName(string aName)
        {
            string digit = aName.Substring(Constants.LiteraName.Length, aName.Length - Constants.LiteraName.Length).Trim();                
            return string.IsNullOrEmpty(digit) ? $"{Constants.LiteraName} 1" : $"{Constants.LiteraName} {digit}";
        }
    }
}
