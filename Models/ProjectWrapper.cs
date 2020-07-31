using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;

namespace GostDOC.Models
{
    static class Extensions
    {
        public static void UpdateComponentProperties(this Component current, Component update)
        {
            foreach (var prop in update.Properties)
            {
                current.Properties[prop.Key] = prop.Value;
            }
        }
    }

    class ProjectWrapper
    {
        private DocManager _docManager = DocManager.Instance;

        #region Public
        public bool UpdateComponents(string aCfgName, SubGroupInfo aGroupInfo, NodeType aParentType, IList<Component> aComponents)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                return UpdateComponents(groups, aGroupInfo, aComponents);
            }            
            return false;
        }

        public bool AddGroup(string aCfgName, SubGroupInfo aGroupInfo, NodeType aParentType)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                return AddGroup(groups, aGroupInfo);
            }
            return false;
        }

        public bool RemoveGroup(string aCfgName, SubGroupInfo aGroupInfo, NodeType aParentType, bool aRemoveComponents)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                return RemoveGroup(groups, aGroupInfo, aRemoveComponents);
            }
            return false;
        }

        public void MoveComponents(string aCfgName, NodeType aParentType, MoveInfo aMoveInfo)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                MoveComponents(groups, aMoveInfo);
            }
        }

        public IList<Component> GetComponents(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                Group grp = null;
                if (groups.TryGetValue(aGroupInfo.GroupName, out grp))
                {
                    // Find subgroup
                    if (!string.IsNullOrEmpty(aGroupInfo.SubGroupName))
                    {
                        grp.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out grp);
                    }

                    return grp?.Components;
                }
            }
            return null;
        }

        public IDictionary<string, IEnumerable<string>> GetGroupNames(string aCfgName, NodeType aParentType)
        {
            Dictionary<string, IEnumerable<string>> result = new Dictionary<string, IEnumerable<string>>();
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                GetGroupInfo(groups, result);
            }
            return result;
        }

        public void SaveGraphValues(IDictionary<string, string> aGraphValues)
        {
            foreach (var cfg in _docManager.Project.Configurations)
            {
                if (cfg.Value.Graphs.Count > 0)
                {
                    foreach (var kvp in aGraphValues)
                    {
                        cfg.Value.Graphs[kvp.Key] = kvp.Value; 
                    }
                    break;
                }
            }
        }

        #endregion Public

        #region Private

        private IDictionary<string, Group> GetConfigGroups(string aCfgName, NodeType aParentType)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return cfg.Specification;
                }
                else if (aParentType == NodeType.Bill)
                {
                    return cfg.Bill;
                }
            }
            return null;
        }

        private void UpdateComponents(Group aGroup, IList<Component> aComponents)
        {
            // Copy all components from group and clear them
            var copy = aGroup.Components.ToList();
            aGroup.Components.Clear();

            // Fill only remained components in new order and update their properties
            foreach (var component in aComponents)
            {
                Component existing = copy.Find(x => x.Guid == component.Guid);
                if (existing != null)
                {
                    // Update existing properties
                    existing.UpdateComponentProperties(component);
                    // Add to group
                    aGroup.Components.Add(existing);
                }
                else
                {
                    aGroup.Components.Add(component);
                }
            }
        }

        private bool UpdateComponents(IDictionary<string, Group> aGroups, SubGroupInfo aGroupInfo, IList<Component> aComponents)
        {
            Group group = null;
            if (!aGroups.TryGetValue(aGroupInfo.GroupName, out group))
            {
                return false;
            }

            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                UpdateComponents(group, aComponents);
                return true;
            }
            else
            {
                Group subGroup = null;
                if (!group.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out subGroup))
                {
                    UpdateComponents(subGroup, aComponents);
                    return true;
                }
            }

            return false;
        }

        private bool AddGroup(IDictionary<string, Group> aGroups, SubGroupInfo aGroupInfo)
        {
            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                if (!aGroups.ContainsKey(aGroupInfo.GroupName))
                {
                    aGroups.Add(aGroupInfo.GroupName, new Group() { Name = aGroupInfo.GroupName, SubGroups = new Dictionary<string, Group>() });
                    return true;
                }
            }
            else
            {
                Group grp = null;
                if (aGroups.TryGetValue(aGroupInfo.GroupName, out grp))
                {
                    if (!grp.SubGroups.ContainsKey(aGroupInfo.SubGroupName))
                    {
                        grp.SubGroups.Add(aGroupInfo.SubGroupName, new Group() { Name = aGroupInfo.GroupName });
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

        private bool RemoveGroup(IDictionary<string, Group> aGroups, SubGroupInfo aGroupInfo, bool aRemoveComponents)
        {
            // Get default group
            Group defaultGroup = aRemoveComponents ? null : GetDefaultGroup(aGroups);

            // Get group
            Group group = null;
            if (!aGroups.TryGetValue(aGroupInfo.GroupName, out group))
            {
                return false;
            }

            // Remove group or subgroup
            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                if (!aRemoveComponents)
                {
                    foreach (var subGroup in group.SubGroups)
                    {
                        defaultGroup.Components.AddRange(subGroup.Value.Components);
                    }
                    defaultGroup.Components.AddRange(group.Components);
                }
                aGroups.Remove(aGroupInfo.GroupName);
                return true;
            }
            else
            {
                Group subGroup = null;
                if (group.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out subGroup))
                {
                    if (!aRemoveComponents)
                    {
                        defaultGroup.Components.AddRange(subGroup.Components);
                    }
                    group.SubGroups.Remove(aGroupInfo.SubGroupName);
                    return true;
                }
            }
            return false;
        }

        private void GetGroupInfo(IDictionary<string, Group> aGroups, IDictionary<string, IEnumerable<string>> aResult)
        {
            foreach (var group in aGroups.Values)
            {
                var subGroups = new List<string>();
                foreach (var subGroup in group.SubGroups.AsNotNull())
                {
                    subGroups.Add(subGroup.Key);
                }
                aResult.Add(group.Name, subGroups);
            }
        }

        private Group GetGroup(IDictionary<string, Group> aGroups, SubGroupInfo aSubGroupInfo)
        {
            Group group = null;
            if (aGroups.TryGetValue(aSubGroupInfo.GroupName, out group))
            {
                if (!string.IsNullOrEmpty(aSubGroupInfo.SubGroupName))
                {
                    group.SubGroups?.TryGetValue(aSubGroupInfo.SubGroupName, out group);
                }
            }
            return group;
        }

        private void MoveComponents(IDictionary<string, Group> aGroups, MoveInfo aMoveInfo)
        {
            var src = GetGroup(aGroups, aMoveInfo.Source);
            var dst = GetGroup(aGroups, aMoveInfo.Destination);

            foreach (var cmp in aMoveInfo.Components)
            {
                // Find component in source group
                var component = src.Components.Find(x => x.Guid == cmp.Guid);
                // Save changes in properties
                component.UpdateComponentProperties(cmp);
                // Add component to destination group
                dst.Components.Add(component);
                // Remove component from source group
                src.Components.Remove(component);
            }
        }

        #endregion Private
    }
}
