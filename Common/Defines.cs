using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GostDOC.Common
{
    public enum NodeType
    {
        Root,
        Elements,
        Specification,
        Bill,
        Bill_D27,
        Configuration,
        Group,
        SubGroup
    }

    public enum GraphPageType 
    {
        General
    }

    public enum ComponentType
    {
        Component,
        Document,
        ComponentPCB
    }
}
