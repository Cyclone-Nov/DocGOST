using System;
using System.Collections.Generic;
using System.Linq;
using GostDOC.Common;

namespace GostDOC.Models
{
    class DocManager
    {
        #region Singleton
        private static readonly Lazy<DocManager> _instance = new Lazy<DocManager>(() => new DocManager(), true);
        public static DocManager Instance => _instance.Value;
        DocManager()
        {
        }
        #endregion
        private XmlManager _xmlManager { get; } = new XmlManager();

        public Project Project { get; private set; } = new Project();

        #region Public
        public bool LoadData(string[] aFiles, string aMainFile)
        {
            return _xmlManager.LoadData(Project, aFiles, aMainFile);
        }

        public bool AddComponent(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType, Component aComponent)
        {
            Configuration cfg;
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return AddComponent(cfg.Specification, aGroupName, aSubGroupName, aComponent);
                }
                else if (aParentType == NodeType.Bill)
                {
                    return AddComponent(cfg.Bill, aGroupName, aSubGroupName, aComponent);
                }
            }
            return false;
        }

        public bool UpdateComponents(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType, IDictionary<Guid, Component> aComponents)
        {
            Configuration cfg;
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
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

        public bool Component(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType, Component aComponent)
        {
            Configuration cfg;
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
            {
                if (aParentType == NodeType.Specification)
                {
                    return AddComponent(cfg.Specification, aGroupName, aSubGroupName, aComponent);
                }
                else if (aParentType == NodeType.Bill)
                {
                    return AddComponent(cfg.Bill, aGroupName, aSubGroupName, aComponent);
                }
            }
            return false;
        }

        public bool AddGroup(string aCfgName, string aGroupName, string aSubGroupName, NodeType aParentType)
        {
            Configuration cfg;
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
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
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
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
            List<Component> components = new List<Component>();

            Configuration cfg;
            if (Project.Configurations.TryGetValue(aCfgName, out cfg))
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

                // Fill components
                if (grp != null)
                {
                    foreach (var component in grp.Components.Values)
                    {
                        components.Add(component);
                    }
                }
            }
            return components;
        }
        #endregion Public

        #region Private

        private void AddComponent(Group aGroup, Component aComponent)
        {
            Component existing = null;
            if (aGroup.Components.TryGetValue(aComponent.Guid, out existing))
            {
                // Update existing
                foreach (var prop in aComponent.Properties)
                {
                    existing.Properties[prop.Key] = prop.Value;
                }
            }
            else
            {
                // Add new component
                aGroup.Components.Add(aComponent.Guid, aComponent);
            }
        }

        private bool AddComponent(IDictionary<string, Group> aGroups, string aGroupName, string aSubGroupName, Component aComponent)
        {
            Group group = null;
            if (!aGroups.TryGetValue(aGroupName, out group))
            {
                return false;
            }

            if (string.IsNullOrEmpty(aSubGroupName))
            {
                AddComponent(group, aComponent);
                return true;
            }
            else
            {
                Group subGroup = null;
                if (!group.SubGroups.TryGetValue(aSubGroupName, out subGroup))
                {
                    AddComponent(subGroup, aComponent);
                    return true;
                }
            }
            return false;
        }

        private void UpdateComponents(Group aGroup, IDictionary<Guid, Component> aComponents)
        {
            foreach (var component in aComponents)
            {
                AddComponent(aGroup, component.Value);
            }

            foreach (var component in aGroup.Components.ToList())
            {
                if (!aComponents.ContainsKey(component.Key))
                {
                    aGroup.Components.Remove(component.Key);
                }
            }
        }

        private bool UpdateComponents(IDictionary<string, Group> aGroups, string aGroupName, string aSubGroupName, IDictionary<Guid, Component> aComponents)
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
            // Get default groups to copy components to
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
