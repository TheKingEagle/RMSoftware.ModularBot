using Discord;
using Discord.WebSocket;
using RMSoftware.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RMSoftware.ModularBot
{

    public class CmdRoleManager
    {
        INIFile mgmt;
        INIFile userBlackList;
        public CmdRoleManager()
        {
            mgmt = new INIFile("cmdMgr.ini");
            userBlackList = new INIFile("blacklist.ini");
            if (!userBlackList.CheckForCategory("Blacklist"))
            {
                userBlackList.CreateCategory("Blacklist");
                userBlackList.SaveConfiguration();
            }
        }

        public bool AddUserToBlacklist(SocketUser user)
        {
            string guildCat = "Blacklist";
            bool check = userBlackList.GetCategoryByName(guildCat).CheckForEntry(user.Id.ToString());
            if (!check)
            {
                userBlackList.CreateEntry(guildCat, user.Id.ToString(), user.Username.Replace("=", "_"));
            }
            userBlackList.SaveConfiguration();
            return !check;
        }

        public bool DeleteUserFromBlacklist(SocketUser user)
        {
            string guildCat = "Blacklist";
            bool check = userBlackList.GetCategoryByName(guildCat).CheckForEntry(user.Id.ToString());
            if (check)
            {
                userBlackList.DeleteEntry(guildCat, user.Id.ToString());
            }
            userBlackList.SaveConfiguration();
            return check;
        }

        public void AddCommandManagerRole(SocketRole role)
        {
            string guildCat = role.Guild.Id.ToString();
            bool check = mgmt.CheckForCategory(guildCat);
            int indx = -1;
            if (check)
            {
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count - 1;
                mgmt.CreateEntry(guildCat, "role" + (indx + 1), role.Id);
            }
            else
            {
                mgmt.CreateCategory(guildCat);
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count - 1;
                mgmt.CreateEntry(guildCat, "role" + (indx + 1), role.Id);
            }
            mgmt.SaveConfiguration();
        }

        public bool UserBlacklisted(SocketUser user)
        {
            return userBlackList.GetCategoryByName("Blacklist").CheckForEntry(user.Id.ToString());
        }

        public string DeleteCommandManager(SocketRole role)
        {
            string guildCat = role.Guild.Id.ToString();
            bool check = mgmt.CheckForCategory(guildCat);
            int indx = -1;
            if (check)
            {
                indx = mgmt.GetCategoryByName(guildCat).Entries.Count - 1;
                try
                {
                    mgmt.DeleteEntry(guildCat, mgmt.GetCategoryByName(guildCat).Entries.Find(x => x.GetAsUlong() == role.Id).Name);
                    //Then get all entries and rename them.

                    for (int i = 0; i < mgmt.GetCategoryByName(guildCat).Entries.Count; i++)
                    {
                        mgmt.GetCategoryByName(guildCat).Entries[i].Name = $"role{i}";
                    }
                }
                catch (Exception ex)
                {

                    return ex.Message;
                }

            }
            else
            {
                return "No results. The guild ID didn't exist in the database.";
            }
            mgmt.SaveConfiguration();
            return "Command Manager database updated.";
        }

        public async Task<bool> CheckUserRole(SocketGuildUser user, DiscordSocketClient client)
        {
            var owner = (await client.GetApplicationInfoAsync()).Owner;
            if (owner.Id == user.Id)
            {
                return true;
            }
            string guildcat = user.Guild.Id.ToString();//if the category does not exist, return false... can't have that;
            if (!mgmt.CheckForCategory(guildcat))
            {
                return false;
            }
            foreach (var role in user.Roles)
            {
                ulong id = role.Id;
                if (mgmt.GetCategoryByName(guildcat).Entries.Exists(x => x.GetAsUlong() == id))
                {
                    return true;//keep doing it until it returns true.
                }
            }
            return false;//default;
        }

        public SocketRole[] GetRolesForGuild(SocketGuild guild)
        {
            List<SocketRole> items = new List<SocketRole>();
            string guildcat = guild.Id.ToString();
            if (!mgmt.CheckForCategory(guildcat))
            {
                return null;
            }
            foreach (var item in mgmt.GetCategoryByName(guildcat).Entries)
            {
                items.Add(guild.GetRole(item.GetAsUlong()));
            }
            return items.ToArray();
        }

        public SocketRole[] GetAllRoles()
        {
            List<SocketRole> items = new List<SocketRole>();
            foreach (var itemc in mgmt.Categories)
            {
                foreach (var item in itemc.Entries)
                {
                    items.Add(Program._client.GetGuild(Convert.ToUInt64(itemc.Name)).GetRole(item.GetAsUlong()));
                }
            }
            return items.ToArray();
        }
    }
}
