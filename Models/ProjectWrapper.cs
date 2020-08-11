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
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private DocManager _docManager = DocManager.Instance;

        private class Updated<T>
        {
            public List<T> Added { get; } = new List<T>();
            public List<T> Removed { get; } = new List<T>();
        }

        #region Public
        public bool UpdateGroup(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo, GroupData aGroupData)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups == null)
            {
                return false;
            }

            // Get sorter
            var sortType = Utils.GetSortType(aParentType, aGroupInfo.GroupName);
            var sorter = SortFactory.GetSort(sortType);
            // Update components
            var updated = UpdateComponents(groups, sorter, aGroupInfo, aGroupData);

            if (aParentType == NodeType.Specification)
            {
                // Get Bill groups
                var billGroups = GetConfigGroups(aCfgName, NodeType.Bill);
                if (billGroups == null)
                {
                    return false;
                }
                // Update Bill components after add / remove in specification
                UpdateBillGroups(billGroups, updated);
            }
            return true;
        }

        public bool AddGroup(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                return AddGroup(groups, aGroupInfo);
            }
            return false;
        }

        public bool RemoveGroup(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo, bool aRemoveComponents)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                // Remove components 
                var removed = RemoveGroup(groups, aGroupInfo, aRemoveComponents);

                // Update components group
                if (!aRemoveComponents)
                {
                    foreach (var component in removed)
                    {
                        component.UpdateComponentGroupInfo(aParentType, new SubGroupInfo());
                    }
                }
                return removed.Count > 0;
            }
            return false;
        }

        public void MoveComponents(string aCfgName, NodeType aParentType, MoveInfo aMoveInfo)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                MoveComponents(groups, aParentType, aMoveInfo);
            }
        }

        public GroupData GetGroupData(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo)
        {
            Group group = GetGroup(aCfgName, aParentType, aGroupInfo);
            if (group != null)
            {
                return new GroupData() { AutoSort = group.AutoSort, Components = group.Components };
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

        private Updated<Component> UpdateComponents(IDictionary<string, Group> aGroups, ISort<Component> aSorter, SubGroupInfo aGroupInfo, GroupData aGroupData)
        {
            // Updated components
            Updated<Component> updated = new Updated<Component>();

            Group group = GetGroup(aGroups, aGroupInfo);
            if (group != null)
            {
                List<Component> components = new List<Component>();

                // Fill only remained components in new order and update their properties
                foreach (var component in aGroupData.Components)
                {
                    // Search existing component
                    Component existing = group.Components.Find(x => x.Guid == component.Guid);

                    if (existing != null)
                    {
                        // Remove found component
                        group.Components.Remove(existing);
                        // Update existing properties
                        existing.UpdateComponentProperties(component);
                        // Add to group
                        components.Add(existing);
                    }
                    else
                    {
                        components.Add(component);
                        // Components that were added
                        updated.Added.Add(component);
                    }
                }

                // Components that were removed
                updated.Removed.AddRange(group.Components);
                // Set group components
                group.Components = components;
                // Sort components
                if (group.AutoSort != aGroupData.AutoSort)
                {
                    group.AutoSort = aGroupData.AutoSort;
                    if (aGroupData.AutoSort && aSorter != null)
                    {                        
                        group.Components = aSorter.Sort(components);
                    }
                }
            }
            return updated;
        }

        private bool UpdateBillGroups(IDictionary<string, Group> aGroups, Updated<Component> aUpdated)
        {
            // Remove components
            foreach (var removed in aUpdated.Removed)
            {
                RemoveComponent(aGroups, removed.Guid);
            }

            // Add new components
            if (aUpdated.Added.Count > 0)
            {
                // Get default group
                Group defaultGroup = GetDefaultGroup(aGroups);
                if (defaultGroup == null)
                {
                    return false;
                }
                // Add components to default group
                foreach (var added in aUpdated.Added)
                {
                    defaultGroup.Components.Add(added);
                }
                // Sort components in group
                var sortType = Utils.GetSortType(NodeType.Bill, string.Empty);
                var sorter = SortFactory.GetSort(sortType);
                if (sorter != null)
                {
                    defaultGroup.Components = sorter.Sort(defaultGroup.Components);
                }
            }
            return true;
        }

        private bool RemoveComponent(IDictionary<string, Group> aGroups, Guid aGuid)
        {
            bool removed = false;
            foreach (var group in aGroups.AsNotNull())
            {
                removed = group.Value.Components.RemoveAll(x => x.Guid == aGuid) != 0;
                if (!removed)
                    removed = RemoveComponent(group.Value.SubGroups, aGuid);

                if (removed)
                    return true;
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
                Group grp;
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
            Group defaultGroup;
            if (!aGroups.TryGetValue("", out defaultGroup))
            {
                defaultGroup = new Group() { Name = "" };
                aGroups.Add(defaultGroup.Name, defaultGroup);
            }
            return defaultGroup;
        }

        private IList<Component> RemoveGroup(IDictionary<string, Group> aGroups, SubGroupInfo aGroupInfo, bool aRemoveComponents)
        {
            List<Component> removed = new List<Component>();

            // Get group
            Group group;
            if (!aGroups.TryGetValue(aGroupInfo.GroupName, out group))
            {
                return removed;
            }

            // Remove group or subgroup
            if (string.IsNullOrEmpty(aGroupInfo.SubGroupName))
            {
                if (!aRemoveComponents)
                {
                    foreach (var subGroup in group.SubGroups)
                    {
                        removed.AddRange(subGroup.Value.Components);
                    }
                    removed.AddRange(group.Components);
                }
                aGroups.Remove(aGroupInfo.GroupName);
            }
            else
            {
                Group subGroup;
                if (group.SubGroups.TryGetValue(aGroupInfo.SubGroupName, out subGroup))
                {
                    if (!aRemoveComponents)
                    {
                        removed.AddRange(subGroup.Components);
                    }
                    group.SubGroups.Remove(aGroupInfo.SubGroupName);
                }
            }

            if (!aRemoveComponents)
            {
                Group defaultGroup = GetDefaultGroup(aGroups);
                defaultGroup.Components.AddRange(removed);
            }

            return removed;
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

        private Group GetGroup(string aCfgName, NodeType aParentType, SubGroupInfo aGroupInfo)
        {
            var groups = GetConfigGroups(aCfgName, aParentType);
            if (groups != null)
            {
                return GetGroup(groups, aGroupInfo);
            }
            return null;
        }

        private Group GetGroup(IDictionary<string, Group> aGroups, SubGroupInfo aSubGroupInfo)
        {
            Group group;
            if (aGroups.TryGetValue(aSubGroupInfo.GroupName, out group))
            {
                if (!string.IsNullOrEmpty(aSubGroupInfo.SubGroupName))
                {
                    group.SubGroups?.TryGetValue(aSubGroupInfo.SubGroupName, out group);
                }
            }
            return group;
        }

        private void MoveComponents(IDictionary<string, Group> aGroups, NodeType aParentType, MoveInfo aMoveInfo)
        {
            var src = GetGroup(aGroups, aMoveInfo.Source);
            var dst = GetGroup(aGroups, aMoveInfo.Destination);

            foreach (var cmp in aMoveInfo.Components)
            {
                // Find component in source group
                var component = src.Components.Find(x => x.Guid == cmp.Guid);
                // Save changes in properties
                component.UpdateComponentProperties(cmp);
                // Update group info
                component.UpdateComponentGroupInfo(aParentType, aMoveInfo.Destination);
                // Add component to destination group
                dst.Components.Add(component);
                // Remove component from source group
                src.Components.Remove(component);
            }
        }

        #endregion Private
    }
}
