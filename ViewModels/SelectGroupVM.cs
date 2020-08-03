using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GostDOC.Common;
using GostDOC.Models;

namespace GostDOC.ViewModels
{
    class SelectGroupVM
    {
        public ObservableCollection<Node> Groups { get; } = new ObservableCollection<Node>();

        public ICommand SelectionChangedCmd => new Command<Node>(SelectionChanged);
        public ICommand ApplyCmd => new Command<Window>(Apply);

        public SubGroupInfo SubGroupInfo { get; } = new SubGroupInfo();

        public SelectGroupVM()
        {
        }
         
        public void SetGroups(IDictionary<string, IEnumerable<string>> aGroups)
        {
            foreach (var group in aGroups)
            {
                string groupName = group.Key;
                if (string.IsNullOrEmpty(group.Key))
                {
                    groupName = Constants.DefaultGroupName;
                }

                Node groupNode = new Node() { Name = groupName, Nodes = new ObservableCollection<Node>(), NodeType = NodeType.Group };
                foreach (var subGroup in group.Value)
                {
                    Node subGroupNode = new Node() { Name = subGroup, NodeType = NodeType.SubGroup };
                    groupNode.Nodes.Add(subGroupNode);
                }
                Groups.Add(groupNode);
            }
        }

        private void SelectionChanged(Node obj)
        {
            SubGroupInfo.GroupName = string.Empty;
            SubGroupInfo.SubGroupName = string.Empty;

            if (obj.NodeType == NodeType.Group)
            {
                SubGroupInfo.GroupName = obj.Name;
            }
            else if (obj.NodeType == NodeType.SubGroup)
            {
                SubGroupInfo.GroupName = obj.Parent.Name;
                SubGroupInfo.SubGroupName = obj.Name;
            }
        }
        private void Apply(Window wnd)
        {
            wnd.DialogResult = true;
            wnd.Close();
        }
    }
}
