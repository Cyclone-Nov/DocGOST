using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Interfaces;
using GostDOC.Models;

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
            Name.Value = aName;
            Text.Value = aText;

            if (aName.Contains("Литера"))
            {
                GraphType = ItemType.ComboBox;
                Items.Add(string.Empty);
                Items.Add("П");
                Items.Add("Э");
                Items.Add("Т");
                Items.Add("О");
                Items.Add("О1");
                Items.Add("О2");
                Items.Add("О3");
                Items.Add("А");
                Items.Add("Б");
                Items.Add("И");
                Items.Add("РО");
                Items.Add("РО1");
                Items.Add("РО2");
                Items.Add("РА");
                Items.Add("РБ");
                Items.Add("РИ");
            }
            else
            {
                GraphType = ItemType.Text;
            }
        }
    }
}
