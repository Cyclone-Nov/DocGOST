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
            if (CommonUtils.IsLiteraField(aName))
            {
                aName = CommonUtils.ConvertToLiteraName(aName);
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

    }
}
