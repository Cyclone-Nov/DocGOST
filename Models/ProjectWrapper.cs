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
            Group group;
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
                Group subGroup;
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
