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

        public ObservableCollection<MaterialNode> MaterialNodes { get; } = new ObservableCollection<MaterialNode>();
        public ObservableProperty<bool> IsAddMaterialEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsRemoveEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<bool> IsEditEnabled { get; } = new ObservableProperty<bool>(false);
        public ObservableProperty<MaterialNode> SelectedItem { get; } = new ObservableProperty<MaterialNode>();

        public ICommand TreeViewSelectionChangedCmd => new Command<MaterialNode>(TreeViewSelectionChanged);
        public ICommand AddGroupCmd => new Command(AddGroup);
        public ICommand AddMaterialCmd => new Command(AddMaterial, IsAddMaterialEnabled);
        public ICommand EditCmd => new Command(Edit, IsEditEnabled);
        public ICommand RemoveCmd => new Command(Remove, IsRemoveEnabled);
        public ICommand ClosingCmd => new Command(Closing);
        public ICommand LoadedCmd => new Command(Loaded);

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
            if (SelectedItem == null)
            {
                return;
            }

            var group = CommonDialogs.GetGroupName();
            if (!string.IsNullOrEmpty(group) && _materials.AddSubGroup(SelectedItem.Value.Name.Value, group))
            {
                // Create new group node
                var node = new MaterialNode(group) { Parent = SelectedItem.Value, Nodes = new ObservableCollection<MaterialNode>() };
                // Add new group to collection
                SelectedItem.Value.Nodes.InsertSorted(node);
            }
        }

        private Tuple<string, string> GetMaterialGroups()
        {
            if (SelectedItem.Value.Parent?.Parent != null)
            {
                return new Tuple<string, string>(SelectedItem.Value.Parent.Parent.Name.Value, SelectedItem.Value.Parent.Name.Value);
            }
            else 
            {
                return new Tuple<string, string>(SelectedItem.Value.Parent.Name.Value, null);
            }
        }
        private Tuple<string, string> GetGroups()
        {
            if (SelectedItem.Value.Parent != null)
            {
                return new Tuple<string, string>(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value);
            }
            else
            {
                return new Tuple<string, string>(SelectedItem.Value.Name.Value, null);
            }
        }

        private void AddMaterial(object obj)
        {
            if (SelectedItem == null)
            {
                return;
            }

            var material = CommonDialogs.AddMaterial();
            if (material != null)
            {
                var groups = GetGroups();                
                if (_materials.AddMaterial(groups.Item1, groups.Item2, material))
                {
                    SelectedItem.Value.Nodes.InsertSorted(new MaterialNode(material.Name, MaterialNodeType.Component) { Parent = SelectedItem.Value });
                }
            }
        }

        private void Edit(object obj)
        {
            if (SelectedItem == null)
            {
                return;
            }

            if (SelectedItem.Value.Type == MaterialNodeType.Group)
            {
                if (SelectedItem.Value.Parent != null)
                {
                    var group = CommonDialogs.EditGroupName(SelectedItem.Value.Name.Value);
                    if (!string.IsNullOrEmpty(group) && group != SelectedItem.Value.Name.Value)
                    {
                        if (_materials.EditSubGroup(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value, group))
                        {
                            SelectedItem.Value.Name.Value = group;
                        }
                    }
                }
            }
            else
            {
                var groups = GetMaterialGroups();
                var src = _materials.GetMaterial(groups.Item1, groups.Item2, SelectedItem.Value.Name.Value);
                if (src != null)
                {
                    var material = CommonDialogs.UpdateMaterial(src);
                    if (material != null)
                    {
                        if (_materials.RemoveMaterial(groups.Item1, groups.Item2, src.Name))
                        {
                            if (_materials.AddMaterial(groups.Item1, groups.Item2, material))
                            {
                                SelectedItem.Value.Name.Value = material.Name;
                            }
                        }
                    }
                }
            }      
        }

        private void Remove(object obj)
        {
            if (SelectedItem == null)
            {
                return;
            }

            if (SelectedItem.Value.Type == MaterialNodeType.Group)
            {
                if (SelectedItem.Value.Parent != null)
                {
                    _materials.RemoveSubGroup(SelectedItem.Value.Parent.Name.Value, SelectedItem.Value.Name.Value);
                    SelectedItem.Value.Parent.Nodes.Remove(SelectedItem.Value);
                }
            }
            else
            {
                var groups = GetMaterialGroups();
                var src = _materials.GetMaterial(groups.Item1, groups.Item2, SelectedItem.Value.Name.Value);
                if (src != null)
                {
                    _materials.RemoveMaterial(groups.Item1, groups.Item2, SelectedItem.Value.Name.Value);
                    SelectedItem.Value.Parent.Nodes.Remove(SelectedItem.Value);
                }
            }
        }

        private void TreeViewSelectionChanged(MaterialNode aItem)
        {
            SelectedItem.Value = aItem;

            IsAddMaterialEnabled.Value = SelectedItem.Value?.Type == MaterialNodeType.Group;
            IsRemoveEnabled.Value = SelectedItem.Value?.Parent != null;
            IsEditEnabled.Value = SelectedItem.Value?.Parent != null;
        }

        private void Closing(object obj)
        {
            _materials.Save();
        }

        private void Loaded(object obj)
        {
            SelectedItem.Value = MaterialNodes.FirstOrDefault();
        }
    }
}
