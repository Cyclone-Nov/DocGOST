﻿using System;
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
        public bool UpdateGroup(string aCfgName, DocType aDocType, SubGroupInfo aGroupInfo, GroupData aGroupData)
        {
            var groups = GetConfigGroups(aCfgName, aDocType);
            if (groups == null)
            {
                return false;
            }

            // Get sorter
            var sortType = Utils.GetSortType(aDocType, aGroupInfo.GroupName);
            var sorter = SortFactory.GetSort(sortType);
            // Update components
            UpdateComponents(groups, sorter, aGroupInfo, aGroupData);            
            return true;
        }

        public bool AddGroup(string aCfgName, DocType aDocType, SubGroupInfo aGroupInfo)
        {
            var groups = GetConfigGroups(aCfgName, aDocType);
            if (groups != null)
            {
                return AddGroup(groups, aGroupInfo);
            }
            return false;
        }

        public bool RemoveGroup(string aCfgName, DocType aDocType, SubGroupInfo aGroupInfo, bool aRemoveComponents)
        {
            var groups = GetConfigGroups(aCfgName, aDocType);
            if (groups != null)
            {
                // Remove components 
                var removed = RemoveGroup(groups, aGroupInfo, aRemoveComponents);

                // Update components group
                if (!aRemoveComponents)
                {
                    if (aDocType == DocType.Bill)
                    {
                        // Move components to default group
                        Group defaultGroup = GetDefaultGroup(groups);
                        defaultGroup.Components.AddRange(removed);
                        // Update group info for each component
                        UpdateComponentGroupInfo(removed, aDocType, new SubGroupInfo());
                    }
                    else if (aDocType == DocType.Specification)
                    {
                        // Move components to specification group
                        var groupInfo = new SubGroupInfo(aGroupInfo.GroupName, string.Empty);
                        Group group = GetGroup(groups, groupInfo);
                        if (group != null)
                        {
                            // Move components
                            group.Components.AddRange(removed);
                            // Update group info for each component
                            UpdateComponentGroupInfo(removed, aDocType, groupInfo);
                        }
                    }
                }
                return removed.Count > 0;
            }
            return false;
        }

        public void MoveComponents(string aCfgName, DocType aDocType, MoveInfo aMoveInfo)
        {
            var groups = GetConfigGroups(aCfgName, aDocType);
            if (groups != null)
            {
                MoveComponents(groups, aDocType, aMoveInfo);
            }
        }

        public GroupData GetGroupData(string aCfgName, DocType aDocType, SubGroupInfo aGroupInfo)
        {
            Group group = GetGroup(aCfgName, aDocType, aGroupInfo);
            if (group != null)
            {
                return new GroupData() { AutoSort = group.AutoSort, Components = group.Components };
            }
            return null;
        }

        public IDictionary<string, IEnumerable<string>> GetGroupNames(string aCfgName, DocType aDocType)
        {
            Dictionary<string, IEnumerable<string>> result = new Dictionary<string, IEnumerable<string>>();
            var groups = GetConfigGroups(aCfgName, aDocType);
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

        public string GetGraphValue(string aCfgName, string aGraphName)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                string val;
                if (cfg.Graphs.TryGetValue(aGraphName, out val))
                {
                    return val;
                }
            }
            return string.Empty;
        }

        public bool UpdateSpecificationPositions(Dictionary<string, List<Tuple<string, int>>> aPositions)
        {            
            foreach (var cfg in _docManager.Project.Configurations)
            {
                var cfg_name = cfg.Key;
                var groups = cfg.Value.Specification;

                foreach (var grp in groups)
                {
                    UpdatePositions(cfg_name, grp.Key, grp.Value.Components, aPositions);
                    foreach (var subgrp in grp.Value.SubGroups)
                    {
                        UpdatePositions(cfg_name, grp.Key, subgrp.Value.Components, aPositions);
                    }
                }
            }
            return true;
        }

        #endregion Public

        #region Private

        private void UpdatePositions(string aConfigName, string aGroup, List<Component> aComponents, Dictionary<string, List<Tuple<string, int>>> aPositions)
        {
            string key = ($"{aConfigName} {aGroup}").Trim();
            if (aPositions.TryGetValue(key, out var positions))
            {             
                int lastPosition = 0;
                foreach (var component in aComponents)
                {
                    lastPosition = SetSpecificationPosition(positions, component, lastPosition);
                }
            }
        }

        /// <summary>
        /// записать позицию в свойство Позиция для данного компонента
        /// </summary>
        /// <param name="aPositions"></param>
        /// <param name="aComponent"></param>
        /// <param name="aPrevPosition"></param>
        private int SetSpecificationPosition(List<Tuple<string, int>> aPositions, Component aComponent, int aPrevPosition)
        {
            int retposition = 0;
            if (aPositions != null)
            {
                string name = aComponent.GetProperty(Constants.ComponentName);
                string designator = aComponent.GetProperty(Constants.ComponentSign);
                string val = ($"{name} {designator}").Trim();
                var positions = aPositions.Where(item => string.Equals(item.Item1, val));
                if (positions != null && positions.Count() > 0)
                {
                    if (positions.Count() == 1)
                    {
                        retposition = positions.First().Item2;
                        aComponent.SetPropertyValue(Constants.ComponentPosition, retposition.ToString());
                    } else
                    {
                        foreach (var pos in positions)
                        {
                            retposition = pos.Item2;
                            if (retposition != 0 && (retposition - aPrevPosition == 1))
                            {
                                aComponent.SetPropertyValue(Constants.ComponentPosition, retposition.ToString());
                                return retposition;
                            }
                        }
                    }
                }
            }
            return retposition;
        }

        private void UpdateComponentGroupInfo(IList<Component> aComponents, DocType aDocType, SubGroupInfo aGroupInfo)
        {
            foreach (var component in aComponents)
            {
                component.UpdateComponentGroupInfo(aDocType, aGroupInfo);
            }
        }

        private IDictionary<string, Group> GetConfigGroups(string aCfgName, DocType aDocType)
        {
            Configuration cfg;
            if (_docManager.Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aDocType == DocType.Specification)
                {
                    return cfg.Specification;
                }
                else if (aDocType == DocType.Bill)
                {
                    return cfg.Bill;
                }
                else if (aDocType == DocType.D27)
                {                    
                    return new Dictionary<string, Group>() { { "", cfg.D27 } };
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
                group.AutoSort = aGroupData.AutoSort;
                if (aGroupData.AutoSort && aSorter != null)
                {                        
                    group.Components = aSorter.Sort(components);
                }
            }
            return updated;
        }

        private void RemoveComponents(IDictionary<string, Group> aGroups, IList<Component> aComponents)
        {
            foreach (var removed in aComponents)
            {
                RemoveComponent(aGroups, removed.Guid);
            }
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

        private Group GetGroup(string aCfgName, DocType aDocType, SubGroupInfo aGroupInfo)
        {
            var groups = GetConfigGroups(aCfgName, aDocType);
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

        private void MoveComponents(IDictionary<string, Group> aGroups, DocType aDocType, MoveInfo aMoveInfo)
        {
            var src = GetGroup(aGroups, aMoveInfo.Source);
            var dst = GetGroup(aGroups, aMoveInfo.Destination);

            foreach (var cmp in aMoveInfo.Components)
            {
                // Find component in source group
                var component = src.Components.Find(x => x.Guid == cmp.Guid);
                if (component == null)
                {
                    dst.Components.Add(cmp);
                    continue;
                }
                // Save changes in properties
                component.UpdateComponentProperties(cmp);
                // Update group info
                component.UpdateComponentGroupInfo(aDocType, aMoveInfo.Destination);
                // Add component to destination group
                dst.Components.Add(component);
                // Remove component from source group
                src.Components.Remove(component);
            }
        }

        #endregion Private
    }
}