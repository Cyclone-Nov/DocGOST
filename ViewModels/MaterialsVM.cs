using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GostDOC.Common;
using GostDOC.Models;
using GostDOC.UI;

namespace GostDOC.ViewModels
{
    class MaterialsVM
    {
        private MaterialTypes _materials = DocManager.Instance.MaterialTypes;
        private MaterialNode _selectedItem = null;

        public ObservableCollection<MaterialNode> MaterialNodes { get; } = new ObservableCollection<MaterialNode>();
        public ObservableProperty<bool> IsAddMaterialEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsEditEnabled { get; } = new ObservableProperty<bool>(false);

        public ICommand TreeViewSelectionChangedCmd => new Command<MaterialNode>(TreeViewSelectionChanged);
        public ICommand AddGroupCmd => new Command(AddGroup);
        public ICommand AddMaterialCmd => new Command(AddMaterial, IsAddMaterialEnabled);
        public ICommand EditCmd => new Command(Edit, IsEditEnabled);
        public ICommand RemoveCmd => new Command(Remove, IsRemoveEnabled);
        public ICommand ClosingCmd => new Command(Closing);

        public MaterialsVM()
        {
            foreach (var kvp in _materials.Materials)
            {
                MaterialNode node = new MaterialNode(kvp.Key) { Nodes = new ObservableCollection<MaterialNode>() };
                AddNode(node, kvp.Value);
                MaterialNodes.InsertSorted(node);
            }
        }

        private void AddNode(MaterialNode aNode, MaterialGroup aGroup)
        {
            if (aGroup.SubGroups != null)
            {
                foreach (var sub in aGroup.SubGroups)
                {
                    MaterialNode gp = new MaterialNode(sub.Key) { Parent = aNode };
                    AddNode(gp, sub.Value);
                    aNode.Nodes.InsertSorted(gp);
                }
            }

            foreach (var m in aGroup.Materials)
            {
                MaterialNode material = new MaterialNode(m.Key, MaterialNodeType.Component) { Parent = aNode };
                aNode.Nodes.InsertSorted(material);
            }
        }

        private void AddGroup(object obj)
        {
            if (_selectedItem == null)
            {
                return;
            }

            var group = CommonDialogs.GetGroupName();
            if (!string.IsNullOrEmpty(group) && _materials.AddSubGroup(_selectedItem.Name.Value, group))
            {
                // Create new group node
                var node = new MaterialNode(group) { Parent = _selectedItem, Nodes = new ObservableCollection<MaterialNode>() };
                // Add new group to collection
                _selectedItem.Nodes.InsertSorted(node);
            }
        }

        private Tuple<string, string> GetMaterialGroups()
        {
            if (_selectedItem.Parent?.Parent != null)
            {
                return new Tuple<string, string>(_selectedItem.Parent.Parent.Name.Value, _selectedItem.Parent.Name.Value);
            }
            else 
            {
                return new Tuple<string, string>(_selectedItem.Parent.Name.Value, null);
            }
        }
        private Tuple<string, string> GetGroups()
        {
            if (_selectedItem.Parent != null)
            {
                return new Tuple<string, string>(_selectedItem.Parent.Name.Value, _selectedItem.Name.Value);
            }
            else
            {
                return new Tuple<string, string>(_selectedItem.Name.Value, null);
            }
        }

        private void AddMaterial(object obj)
        {
            if (_selectedItem == null)
            {
                return;
            }

            var material = CommonDialogs.AddMaterial();
            if (material != null)
            {
                var groups = GetGroups();                
                if (_materials.AddMaterial(groups.Item1, groups.Item2, material))
                {
                    _selectedItem.Nodes.InsertSorted(new MaterialNode(material.Name, MaterialNodeType.Component) { Parent = _selectedItem });
                }
            }
        }

        private void Edit(object obj)
        {
            if (_selectedItem == null)
            {
                return;
            }

            if (_selectedItem.Type == MaterialNodeType.Group)
            {
                if (_selectedItem.Parent != null)
                {
                    var group = CommonDialogs.EditGroupName(_selectedItem.Name.Value);
                    if (!string.IsNullOrEmpty(group) && group != _selectedItem.Name.Value)
                    {
                        if (_materials.EditSubGroup(_selectedItem.Parent.Name.Value, _selectedItem.Name.Value, group))
                        {
                            _selectedItem.Name.Value = group;
                        }
                    }
                }
            }
            else
            {
                var groups = GetMaterialGroups();
                var src = _materials.GetMaterial(groups.Item1, groups.Item2, _selectedItem.Name.Value);
                if (src != null)
                {
                    var material = CommonDialogs.UpdateMaterial(src);
                    if (material != null)
                    {
                        if (_materials.RemoveMaterial(groups.Item1, groups.Item2, src.Name))
                        {
                            if (_materials.AddMaterial(groups.Item1, groups.Item2, material))
                            {
                                _selectedItem.Name.Value = material.Name;
                            }
                        }
                    }
                }
            }      
        }

        private void Remove(object obj)
        {
            if (_selectedItem == null)
            {
                return;
            }

            if (_selectedItem.Type == MaterialNodeType.Group)
            {
                if (_selectedItem.Parent != null)
                {
                    _materials.RemoveSubGroup(_selectedItem.Parent.Name.Value, _selectedItem.Name.Value);
                    _selectedItem.Parent.Nodes.Remove(_selectedItem);
                }
            }
            else
            {
                var groups = GetMaterialGroups();
                var src = _materials.GetMaterial(groups.Item1, groups.Item2, _selectedItem.Name.Value);
                if (src != null)
                {
                    _materials.RemoveMaterial(groups.Item1, groups.Item2, _selectedItem.Name.Value);
                    _selectedItem.Parent.Nodes.Remove(_selectedItem);
                }
            }
        }

        private void TreeViewSelectionChanged(MaterialNode aItem)
        {
            _selectedItem = aItem;

            IsAddMaterialEnabled.Value = _selectedItem?.Type == MaterialNodeType.Group;
            IsRemoveEnabled.Value = _selectedItem?.Parent != null;
            IsEditEnabled.Value = _selectedItem?.Parent != null;
        }

        private void Closing(object obj)
        {
            _materials.Save();
        }
    }
}
