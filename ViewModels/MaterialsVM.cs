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

        public ICommand TreeViewSelectionChangedCmd => new Command<MaterialNode>(TreeViewSelectionChanged);
        public ICommand AddGroupCmd => new Command(AddGroup);
        public ICommand AddMaterialCmd => new Command(AddMaterial);
        public ICommand EditCmd => new Command(Edit);
        public ICommand RemoveCmd => new Command(Remove);
        public ICommand ClosingCmd => new Command(Closing);

        public MaterialsVM()
        {
            foreach (var kvp in _materials.Materials)
            {
                MaterialNode group = new MaterialNode(kvp.Key) { Nodes = new ObservableCollection<MaterialNode>() };
                foreach (var m in kvp.Value)
                {
                    MaterialNode material = new MaterialNode(m.Key) { Parent = group };
                    group.Nodes.InsertSorted(material);
                }
                MaterialNodes.InsertSorted(group);
            }
        }

        private void AddGroup(object obj)
        {
            var group = CommonDialogs.GetGroupName();
            if (!string.IsNullOrEmpty(group) && _materials.AddGroup(group))
            {
                // Create new group node
                var node = new MaterialNode(group) { Nodes = new ObservableCollection<MaterialNode>() };
                // Add new group to collection
                MaterialNodes.InsertSorted(node);
            }
        }

        private void AddMaterial(object obj)
        {
            var material = CommonDialogs.AddMaterial();
            if (material != null)
            {
                if (_materials.AddMaterial(_selectedItem.Name.Value, material))
                {
                    _selectedItem.Nodes.InsertSorted(new MaterialNode(material.Name) { Parent = _selectedItem });
                }
            }
        }

        private void Edit(object obj)
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.Parent == null)
                {
                    var group = CommonDialogs.EditGroupName(_selectedItem.Name.Value);
                    if (!string.IsNullOrEmpty(group) && group != _selectedItem.Name.Value)
                    {
                        if (_materials.EditGroup(_selectedItem.Name.Value, group))
                        {
                            _selectedItem.Name.Value = group;
                        }                        
                    }
                }
                else
                {
                    string groupName = _selectedItem.Parent.Name.Value;                    
                    var src = _materials.GetMaterial(groupName, _selectedItem.Name.Value);
                    if (src != null)
                    {
                        var material = CommonDialogs.UpdateMaterial(src);
                        if (material != null)
                        {
                            if (_materials.RemoveMaterial(groupName, src.Name))
                            {
                                if (_materials.AddMaterial(groupName, material))
                                {
                                    _selectedItem.Name.Value = material.Name;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Remove(object obj)
        {
            if (_selectedItem != null)
            {
                if (_selectedItem.Parent == null)
                {
                    _materials.RemoveGroup(_selectedItem.Name.Value);
                    MaterialNodes.Remove(_selectedItem);
                }
                else
                {
                    _materials.RemoveMaterial(_selectedItem.Parent.Name.Value, _selectedItem.Name.Value);
                    _selectedItem.Parent.Nodes.Remove(_selectedItem);
                }
            }
        }

        private void TreeViewSelectionChanged(MaterialNode aItem)
        {
            _selectedItem = aItem;

            IsAddMaterialEnabled.Value = _selectedItem?.Parent == null;
        }

        private void Closing(object obj)
        {
            _materials.Save();
        }
    }
}
