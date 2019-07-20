using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace ModularBOT.Entity
{

    public class GuildObject
    {
        public string CommandPrefix { get; set; }

        public bool LockPFChanges { get; set; }
        public ulong ID { get; set; }
        public List<GuildCommand> GuildCommands { get; set; }

        public void SaveJson()
        {
            using (StreamWriter sw = new StreamWriter($"guilds/{ID}.guild"))
            {

                sw.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
                sw.Flush();
                sw.Close();
            }
        }
    }
}
