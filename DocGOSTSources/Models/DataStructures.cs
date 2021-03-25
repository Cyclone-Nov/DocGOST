using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Common;
using SoftCircuits.Collections;

namespace GostDOC.Models
{
    public class Component
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public ComponentType Type { get; set; }
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();
        public float Count { get; set; } = 1;

        public Component(Guid aGuid)
        {
            Guid = aGuid;
        }

        public Component(Guid aGuid, float aCount)
        {
            Guid = aGuid;
            Count = aCount;
        }

        public Component(ComponentXml aComponent)
        {
            Properties.AddRange(aComponent.Properties);
        }
    }

    public class Group
    {        
        public string Name { get; set; }            
        /// <summary>
        /// имя для сортировки данной группы среди прочих групп, если на задано то соритровать необходимо по имени
        /// </summary>
        /// <value>
        /// The name of the sort.
        /// </value>
        public string SortName { get; set; }
        public bool AutoSort { get; set; } = true;
        public List<Component> Components { get; set; } = new List<Component>();
        public IDictionary<string, Group> SubGroups { get; set; } = new Dictionary<string, Group>();

        /// <summary>
        /// глубокое копирование группы
        /// </summary>
        /// <returns></returns>
        public Group DeepCopy()
        {
            Group copy = (Group)this.MemberwiseClone();
            if (this.SubGroups?.Count() > 0)
                copy.SubGroups = this.SubGroups.ToDictionary(entry => entry.Key, 
                                                             entry => entry.Value.DeepCopy());
            if(this.Components?.Count > 0)
                copy.Components = new List<Component>(this.Components);

            return copy;
        }
    }

    public class Configuration
    {
        public string Name { get; set; }

        public IDictionary<string, object> PrivateProperties { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, string> Graphs { get; set; } = new Dictionary<string, string>();

        public IDictionary<string, Group> Specification { get; set; } = new OrderedDictionary<string, Group>();
        
        public IDictionary<string, Group> Bill { get; set; } = new Dictionary<string, Group>();
        public Group D27 { get; set; } = new Group();

        /// <summary>
        /// изменить автосортировку
        /// </summary>
        /// <param name="aEnable">включить - <c>true</c>, отключить - <c>false</c></param>
        public void ChangeAutoSort(bool aEnable)
        {
            D27.AutoSort = aEnable;
            void ChangeAutoSortInGroups(IDictionary<string, Group> aGroups)
            {
                foreach (var grp in aGroups)
                {
                    grp.Value.AutoSort = aEnable;
                    if (grp.Value.SubGroups.Count() > 0)
                        foreach (var subgrp in grp.Value.SubGroups)
                            subgrp.Value.AutoSort = aEnable;
                }
            }
            ChangeAutoSortInGroups(Bill);
            ChangeAutoSortInGroups(Specification);
        }

        /// <summary>
        /// глубокое копировани конфигурации
        /// </summary>
        /// <returns></returns>
        public Configuration DeepCopy()
        {
            Configuration copy = (Configuration)this.MemberwiseClone();
            if (this.Graphs?.Count() > 0)
                copy.Graphs = this.Graphs.ToDictionary(entry => entry.Key,
                                                       entry => entry.Value);
                                                                   
            if (this.Specification?.Count() > 0)
                copy.Specification = this.Specification.ToDictionary(entry => entry.Key,
                                                                     entry => entry.Value.DeepCopy());

            if (this.Bill?.Count() > 0)
                copy.Bill = this.Bill.ToDictionary(entry => entry.Key,
                                                   entry => entry.Value.DeepCopy());

            copy.D27 = this.D27.DeepCopy();

            return copy;
        }
    }

    public class Project
    {
        public string Name { get; set; }
        public ProjectType Type { get; set; } = ProjectType.GostDoc;
        public string Version { get; set; }
        public IDictionary<string, Configuration> Configurations { get; } = new Dictionary<string, Configuration>();

        /// <summary>
        /// изменить автосортировку во всем проекте
        /// </summary>
        /// <param name="aEnable">if set to <c>true</c> [a enable].</param>
        public void ChangeAutoSort(bool aEnable)
        {
            foreach (var cfg in Configurations)
                cfg.Value.ChangeAutoSort(aEnable);
        }
    }
}
