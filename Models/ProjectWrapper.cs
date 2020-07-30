using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    class ProjectWrapper
    {
        private DocManager _docManager = DocManager.Instance;

        #region Public
        public bool UpdateComponents(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType, IList<Component> aComponents)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return UpdateComponents(cfg.Specification, aGroupName, aSubGroupName, aComponents);
                }
                else if (aParentType == NodeType.Bill)
                {
                    return UpdateComponents(cfg.Bill, aGroupName, aSubGroupName, aComponents);
                }
            }
            return false;
        }

        public bool AddGroup(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return AddGroup(cfg.Specification, aGroupName, aSubGroupName);
                }
                else if (aParentType == NodeType.Bill)
                {
                    return AddGroup(cfg.Bill, aGroupName, aSubGroupName);
                }
            }
            return false;
        }

        public bool RemoveGroup(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType, bool aRemoveComponents)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return RemoveGroup(cfg.Specification, aGroupName, aSubGroupName, aRemoveComponents);
                }
                else if (aParentType == NodeType.Bill)
                {
                    return RemoveGroup(cfg.Bill, aGroupName, aSubGroupName, aRemoveComponents);
                }
            }
            return false;
        }

        public IList<Component> GetComponents(string aCfgName, NodeType aParentType, string aGroupName, string aSubGroupName)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                Group grp = null;

                // Find group
                if (aParentType == NodeType.Specification)
                {
                    cfg.Specification.TryGetValue(aGroupName, out grp);
                }
                else if (aParentType == NodeType.Bill)
                {
                    cfg.Bill.TryGetValue(aGroupName, out grp);
                }

                // Find subgroup
                if (!string.IsNullOrEmpty(aSubGroupName) && grp != null)
                {
                    grp.SubGroups.TryGetValue(aSubGroupName, out grp);
                }

                return grp?.Components;
            }
            return null;
        }

        #endregion Public

        #region Private
        private void AddComponent(Group aGroup, Component aComponent)
        {
            bool exists = false;
            foreach (var component in aGroup.Components)
            {
                if (component.Guid == aComponent.Guid)
                {                    
                    // Update existing
                    foreach (var prop in aComponent.Properties)
                    {
                        component.Properties[prop.Key] = prop.Value;
                    }
                    exists = true;
                }
            }

            if (!exists)
            {
                // Add new component to group
                aGroup.Components.Add(aComponent);
            }
        }

        private void UpdateComponents(Group aGroup, IList<Component> aComponents)
        {
            var copy = aGroup.Components.ToList();
            aGroup.Components.Clear();

            foreach (var component in aComponents)
            {
                Component existing = copy.Find(x => x.Guid == component.Guid);
                if (existing != null)
                {
                    // Update existing
                    foreach (var prop in component.Properties)
                    {
                        existing.Properties[prop.Key] = prop.Value;
                    }
                    aGroup.Components.Add(existing);
                }
                else
                {
                    aGroup.Components.Add(component);
                }
            }
        }

        private bool UpdateComponents(IDictionary<string, Group> aGroups, string aGroupName, string aSubGroupName, IList<Component> aComponents)
        {
            Group group = null;
            if (!aGroups.TryGetValue(aGroupName, out group))
            {
                return false;
            }

            if (string.IsNullOrEmpty(aSubGroupName))
            {
                UpdateComponents(group, aComponents);
                return true;
            }
            else
            {
                Group subGroup = null;
                if (!group.SubGroups.TryGetValue(aSubGroupName, out subGroup))
                {
                    UpdateComponents(subGroup, aComponents);
                    return true;
                }
            }

            return false;
        }

        private bool AddGroup(IDictionary<string, Group> aGroups, string aGroupName, string aSubGroupName)
        {
            if (string.IsNullOrEmpty(aSubGroupName))
            {
                if (!aGroups.ContainsKey(aGroupName))
                {
                    aGroups.Add(aGroupName, new Group() { Name = aGroupName, SubGroups = new Dictionary<string, Group>() });
                    return true;
                }
            }
            else
            {
                Group grp = null;
                if (aGroups.TryGetValue(aGroupName, out grp))
                {
                    if (!grp.SubGroups.ContainsKey(aSubGroupName))
                    {
                        grp.SubGroups.Add(aSubGroupName, new Group() { Name = aGroupName });
                        return true;
                    }
                }
            }
            return false;
        }

        private Group GetDefaultGroup(IDictionary<string, Group> aGroups)
        {
            Group defaultGroup = null;
            if (!aGroups.TryGetValue("", out defaultGroup))
            {
                defaultGroup = new Group() { Name = "" };
                aGroups.Add(defaultGroup.Name, defaultGroup);
            }
            return defaultGroup;
        }

        private bool RemoveGroup(IDictionary<string, Group> aGroups, string aGroupName, string aSubGroupName, bool aRemoveComponents)
        {
            // Get default group
            Group defaultGroup = aRemoveComponents ? null : GetDefaultGroup(aGroups);

            // Get group
            Group group = null;
            if (!aGroups.TryGetValue(aGroupName, out group))
            {
                return false;
            }

            // Remove group or subgroup
            if (string.IsNullOrEmpty(aSubGroupName))
            {
                if (!aRemoveComponents)
                {
                    foreach (var subGroup in group.SubGroups)
                    {
                        defaultGroup.Components.AddRange(subGroup.Value.Components);
                    }
                    defaultGroup.Components.AddRange(group.Components);
                }
                aGroups.Remove(aGroupName);
                return true;
            }
            else
            {
                Group subGroup = null;
                if (!group.SubGroups.TryGetValue(aSubGroupName, out subGroup))
                {
                    if (!aRemoveComponents)
                    {
                        defaultGroup.Components.AddRange(subGroup.Components);
                    }
                    group.SubGroups.Remove(aSubGroupName);
                    return true;
                }
            }
            return false;
        }
        #endregion Private
    }
}
