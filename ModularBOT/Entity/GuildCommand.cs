using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Component;

namespace ModularBOT.Entity
{
    public class GuildCommand
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public bool RequirePermission { get; set; }
        public AccessLevels CommandAccessLevel { get; set; } = AccessLevels.CommandManager;
        //access level, by default, won't be enforced unless RequirePermission = true
        public int? Counter { get; set; }
    }
}
