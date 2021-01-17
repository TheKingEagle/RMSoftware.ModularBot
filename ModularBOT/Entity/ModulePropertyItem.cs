using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Entity
{
    public class ModulePropertyItem
    {
        public string ModuleName { get; set; }
        public string ServiceClass { get; set; }
        public List<ulong> GuildsAvailable { get; set; }

        public List<string> ModuleGroups { get; set; }
    }
}
