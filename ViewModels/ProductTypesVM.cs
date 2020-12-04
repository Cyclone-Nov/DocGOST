using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using GostDOC.Common;
using GostDOC.Dictionaries;
using GostDOC.Models;
using GostDOC.UI;

namespace GostDOC.ViewModels
{
    class ProductTypesVM
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private ProductTypes _products;

        public ProductTypesDoc DocType { get; private set; } = ProductTypesDoc.Materials;

        public ObservableCollection<DictionaryNode> DictionaryNodes { get; } = new ObservableCollection<DictionaryNode>();
        public ObservableProperty<bool> IsAddGroupEnabled { get; } = new ObservableProperty<bool>(true);
        public ObservableProperty<bool> IsAddEnabled { get; } = new ObservableProperty<bool>(true);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsEditEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<DictionaryNode> SelectedItem { get; } = new ObservableProperty<DictionaryNode>();

        public ObservableProperty<string> Title { get; } = new ObservableProperty<string>();
        public ObservableProperty<string> AddProductButton { get; } = new ObservableProperty<string>();

        public ICommand TreeViewSelectionChangedCmd => new Command<DictionaryNode>(TreeViewSelectionChanged);
        public ICommand TreeViewMouseButtonDownCmd => new Command<DictionaryNode>(TreeViewMouseButtonDown);
        public ICommand AddGroupCmd => new Command(AddGroup, IsAddGroupEnabled);
        public ICommand AddProductCmd => new Command(AddProduct, IsAddEnabled);
        public ICommand EditCmd => new Command(Edit, IsEditEnabled);
        public ICommand RemoveCmd => new Command(Remove, IsRemoveEnabled);
        public ICommand ClosingCmd => new Command(Closing);
        public ICommand LoadedCmd => new Command(Loaded);

        public ProductTypesVM()
        {        
        }

        public void SetDocType(ProductTypesDoc aDocType)
        {
            DocType = aDocType;

            switch (DocType)
            {
                case ProductTypesDoc.Materials:
                    Title.Value = "Материалы";
                    AddProductButton.Value = "Добавить материал";
                    _products = DocManager.Instance.Materials;
                    break;
                case ProductTypesDoc.Others:
                    Title.Value = "Прочие изделия";
                    AddProductButton.Value = "Добавить изделие";
                    _products = DocManager.Instance.Others;
                    break;
                case ProductTypesDoc.Standard:
                    Title.Value = "Стандартные изделия";
                    AddProductButton.Value = "Добавить изделие";
                    _products = DocManager.Instance.Standard;
                    break;
                default:
                    _log.Error($"Неизвестный тип документа {aDocType}!");
                    return;
            }

            Init();
        }

        private void Init()
        {
            foreach (var kvp in _products.Products.Groups)
            {
                DictionaryNode node = new DictionaryNode(kvp.Key) { Nodes = new ObservableCollection<DictionaryNode>() };
                AddNode(node, kvp.Value);
                DictionaryNodes.InsertSorted(node);
            }

            foreach (var kvp in _products.Products.ProductsList)
            {
                DictionaryNodes.InsertSorted(new DictionaryNode(kvp.Key, DictionaryNodeType.Component));
            }
        }

        private void AddGroup(object obj)
        {
            var group = CommonDialogs.GetGroupName();
            if (!string.IsNullOrEmpty(group))
            {
                if (SelectedItem.Value == null)
                {
                    if (_products.AddGroup(group))
                    {
                        var node = new DictionaryNode(group) { Nodes = new ObservableCollection<DictionaryNode>() };
                        DictionaryNodes.InsertSorted(node);
                    }
                }
                else 
                { 
                    if (_products.AddSubGroup(SelectedItem.Value.Name.Value, group))
                    {
                        // Create new group node
                        var node = new DictionaryNode(group, DictionaryNodeType.SubGroup) { Parent = SelectedItem.Value, Nodes = new ObservableCollection<DictionaryNode>() };
                        // Add new group to collection
                        SelectedItem.Value.Nodes.InsertSorted(node);
                    }
                }
            }
        }

        private void AddProduct(object obj)
        {
            var product = CommonDialogs.AddProduct(DocType);
            if (product != null)
            {
                if (SelectedItem.Value == null)
                {
                    if (_products.AddProduct(null, null, product))
                    {
                        DictionaryNodes.InsertSorted(new DictionaryNode(product.Name, DictionaryNodeType.Component));
                    }
                }
                else if (SelectedItem.Value.Parent == null)
                {
                    if (_products.AddProduct(SelectedItem.Value.Name.Value, null, product))
                    {
                        SelectedItem.Value.Nodes.InsertSorted(new DictionaryNode(product.Name, DictionaryNodeType.Component) { Parent = SelectedItem.Value });
                    }
                }
                else
                {
                    if (_products.AddProduct(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value, product))
                    {
                        SelectedItem.Value.Nodes.InsertSorted(new DictionaryNode(product.Name, DictionaryNodeType.Component) { Parent = SelectedItem.Value });
                    }
                }
            }
        }

        private void Edit(object obj)
        {
            if (SelectedItem.Value.Type == DictionaryNodeType.Group)
            {
                var group = CommonDialogs.EditGroupName(SelectedItem.Value.Name.Value);
                if (!string.IsNullOrEmpty(group))
                {
                    if (_products.EditGroup(SelectedItem.Value.Name.Value, group))
                    {
                        SelectedItem.Value.Name.Value = group;
                    }
                }
            }
            else if (SelectedItem.Value.Type == DictionaryNodeType.SubGroup)
            {
                var group = CommonDialogs.EditGroupName(SelectedItem.Value.Name.Value);
                if (!string.IsNullOrEmpty(group))
                {
                    if (_products.EditSubGroup(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value, group))
                    {
                        SelectedItem.Value.Name.Value = group;
                    }
                }
            }
            else if (SelectedItem.Value.Type == DictionaryNodeType.Component)
            {
                string group = null;
                string subgroup = null;

                if (SelectedItem.Value.Parent?.Type == DictionaryNodeType.SubGroup)
                {
                    group = SelectedItem.Value.Parent.Parent.Name.Value;
                    subgroup = SelectedItem.Value.Parent.Name.Value;
                }
                else if (SelectedItem.Value.Parent?.Type == DictionaryNodeType.Group)
                {
                    group = SelectedItem.Value.Parent.Name.Value;
                }

                var src = _products.GetProduct(group, subgroup, SelectedItem.Value.Name.Value);
                if (src != null)
                {
                    var product = CommonDialogs.UpdateProduct(src);
                    if (product != null)
                    { 
                        // Add new product
                        if (_products.AddProduct(group, subgroup, product))
                        {
                            // Remove current product
                            if (_products.RemoveProduct(group, subgroup, src.Name))
                            {
                                SelectedItem.Value.Name.Value = product.Name;
                            }
                        }
                    }
                }
            }      
        }

        private void Remove(object obj)
        {
            if (SelectedItem.Value.Type == DictionaryNodeType.Group)
            {
                _products.RemoveGroup(SelectedItem.Value.Name.Value);
                DictionaryNodes.Remove(SelectedItem.Value);
            }
            else if (SelectedItem.Value.Type == DictionaryNodeType.SubGroup)
            {
                _products.RemoveSubGroup(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value);
                SelectedItem.Value.Parent.Nodes.Remove(SelectedItem.Value);
            }
            else
            {
                if (SelectedItem.Value.Parent == null)
                {
                    _products.RemoveProduct(null, null, SelectedItem.Value.Name.Value);
                    DictionaryNodes.Remove(SelectedItem.Value);
                }
                else if (SelectedItem.Value.Parent.Type == DictionaryNodeType.SubGroup)
                {
                    _products.RemoveProduct(SelectedItem.Value.Parent.Parent.Name.Value, SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value);
                    SelectedItem.Value.Parent.Nodes.Remove(SelectedItem.Value);
                }
                else if (SelectedItem.Value.Parent.Type == DictionaryNodeType.Group)
                {
                    _products.RemoveProduct(SelectedItem.Value.Parent.Name.Value, null, SelectedItem.Value.Name.Value);
                    SelectedItem.Value.Parent.Nodes.Remove(SelectedItem.Value);
                }
            }
        }

        private void TreeViewSelectionChanged(DictionaryNode aItem)
        {
            var type = SelectedItem.Value?.Type;

            switch (DocType)
            {
                case ProductTypesDoc.Materials:
                    IsAddGroupEnabled.Value = type == DictionaryNodeType.Group;
                    IsAddEnabled.Value = type == DictionaryNodeType.Group || type == DictionaryNodeType.SubGroup;
                    IsRemoveEnabled.Value = type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    IsEditEnabled.Value = type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    break;
                case ProductTypesDoc.Others:
                    IsRemoveEnabled.Value = type == DictionaryNodeType.Group || type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    IsEditEnabled.Value = type == DictionaryNodeType.Group || type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    break;
                case ProductTypesDoc.Standard:
                    IsRemoveEnabled.Value = type == DictionaryNodeType.Group || type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    IsEditEnabled.Value = type == DictionaryNodeType.Group || type == DictionaryNodeType.SubGroup || type == DictionaryNodeType.Component;
                    break;
            }         
        }

        private void TreeViewMouseButtonDown(DictionaryNode aItem)
        {
            if (aItem != null)
            {
                aItem.IsSelected.Value = false;
            }
        }

        private void Closing(object obj)
        {
            _products.Save();
        }

        private void Loaded(object obj)
        {
            SelectedItem.Value = DictionaryNodes.FirstOrDefault();
        }

        private void AddNode(DictionaryNode aNode, ProductGroup aGroup)
        {
            if (aGroup.SubGroups != null)
            {
                foreach (var sub in aGroup.SubGroups)
                {
                    DictionaryNode gp = new DictionaryNode(sub.Key, DictionaryNodeType.SubGroup) { Parent = aNode };
                    AddNode(gp, sub.Value);
                    aNode.Nodes.InsertSorted(gp);
                }
            }

            foreach (var m in aGroup.ProductsList)
            {
                DictionaryNode material = new DictionaryNode(m.Key, DictionaryNodeType.Component) { Parent = aNode };
                aNode.Nodes.InsertSorted(material);
            }
        }
    }
}
