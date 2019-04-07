using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Discord;
namespace ModularBOT.Component
{
    /// <summary>
    /// The permission check system for ModularBOT v2
    /// By default: All users have some access to the bot, unless otherwise listed in RegisteredEntities.
    /// This system allows you to blacklist users, or give them a higher access level.
    /// </summary>
    public class PermissionManager
    {
        private List<RegisteredEntity> _entities;
        private IServiceProvider _services;
        
        public IReadOnlyCollection<RegisteredEntity> RegisteredEntities { get { return _entities.AsReadOnly(); } }
        public RegisteredEntity DefaultAdmin { get; private set; }

        public PermissionManager(IServiceProvider services)
        {
            _services = services;

            _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Creating default administrator info"));
            
            

          
            //load json.
            if (File.Exists("Permissions.json"))
            {
                using (StreamReader sr = new StreamReader("Permissions.json"))
                {
                    string json = sr.ReadToEnd();
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Permissions", "JSON file found! Attempting to populate RegisteredEntities"));
                    _entities = JsonConvert.DeserializeObject<List<RegisteredEntity>>(json);
                }
            }
            //if load fails
            if(_entities == null)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Permissions", "Creating a new entity list"));
                _entities = new List<RegisteredEntity>();
            }
        }

        /// <summary>
        /// Check permission of an entity if registered.
        /// </summary>
        /// <param name="item">Item that interfaces with ISnowflakeEntity. Preferably IUser or IRole.</param>
        /// <returns>Item's accessLevel, or AccessLevels.Normal if nothing is found.</returns>
        public AccessLevels GetAccessLevel(ISnowflakeEntity item)
        {
            DefaultAdmin = new RegisteredEntity
            {
                AccessLevel = AccessLevels.Administrator,
                EntityID = _services.GetRequiredService<DiscordShardedClient>()
                .GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Id,
                WarnIfBlacklisted = true//though this should never happen.
            };//This will not be added to list, as it doesn't count.
            if (item.Id == DefaultAdmin.EntityID)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Detected entity as bot owner."));
                return DefaultAdmin.AccessLevel;
            }

            RegisteredEntity df = _entities.FirstOrDefault(z => z.EntityID == item.Id);

            if (df!= null)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Specified entity is on the list."));
                return df.AccessLevel;
            }

            _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Specified entity isn't on the list. Assume NORMAL."));
            return AccessLevels.Normal;
        }

        public bool GetWarnOnBlacklist(ISnowflakeEntity item)
        {
            RegisteredEntity df = _entities.FirstOrDefault(z => z.EntityID == item.Id);

            if (df != null)
            {
                if(df.WarnIfBlacklisted)
                {
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "warn detected."));
                    df.WarnIfBlacklisted = false;
                    SaveJson();
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Register a new user, or modify an existing.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="level"></param>
        /// <returns>0: if nothing changed. 1: if new user created; 2: if user edited; </returns>
        internal int RegisterEntity(IUser user, AccessLevels level)
        {
            
            RegisteredEntity r = new RegisteredEntity
            {
                EntityID = user.Id,
                WarnIfBlacklisted = true,
                AccessLevel = level
            };
            RegisteredEntity c = _entities.FirstOrDefault(x => x.EntityID == r.EntityID);
            if (c != null)
            {
                if(c.AccessLevel == level)
                {
                    return 0;
                }
                c.AccessLevel = level;
                SaveJson();

                return 2;
            }
            else
            {
                _entities.Add(r);
                SaveJson();

                return 1;
            }
        }

        /// <summary>
        /// Register a new role, or modify an existing.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="level"></param>
        /// <returns>0: if nothing changed. 1: if new user created; 2: if user edited; </returns>
        internal int RegisterEntity(IRole role, AccessLevels level)
        {
            if(level == AccessLevels.Blacklisted)
            {
                throw new ArgumentException("You cannot blacklist a role!!! (for now)");
            }
            RegisteredEntity r = new RegisteredEntity
            {
                EntityID = role.Id,
                WarnIfBlacklisted = true,
                AccessLevel = level
            };
            RegisteredEntity c = _entities.FirstOrDefault(x => x.EntityID == r.EntityID);
            if (c != null)
            {
                if (c.AccessLevel == level)
                {
                    return 0;
                }
                c.AccessLevel = level;
                SaveJson();

                return 2;
            }
            else
            {
                _entities.Add(r);
                SaveJson();

                return 1;
            }
            
        }

        /// <summary>
        /// Removes a registered entity from the list.
        /// </summary>
        /// <param name="entity">Entity with an id. Preferably IRole or IUser.</param>
        /// <returns></returns>
        internal bool DeleteEntity(ISnowflakeEntity entity)
        {
            RegisteredEntity r = _entities.FirstOrDefault(x => x.EntityID == entity.Id);
            if(r!=null)
            {
                _entities.Remove(r);
                SaveJson();
                return true;
            }
            return false;
        }

        private void SaveJson()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("Permissions.json"))
                {
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Saving entities. Permissions.json"));
                    sw.WriteLine(JsonConvert.SerializeObject(RegisteredEntities,Formatting.Indented));
                    sw.Flush();
                    sw.Close();
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Write success!"),ConsoleColor.Green);
                }
            }
            catch (Exception ex)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Permissions", "Write failed!",ex));
            }
        }

    }

    public enum AccessLevels
    {
       /// <summary>
       /// Users with this access level CANNOT use the bot at all.
       /// </summary>
       Blacklisted = -1,

        /// <summary>
        /// Users with this access level may use unrestricted commands.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Users with this access level can do the following:
        ///    • Manage custom commands
        ///    • Run restricted custom commands
        ///    
        /// Note: This will NOT give you access to guild-permission based commands.
        /// </summary>
        CommandManager = 1,

        /// <summary>
        /// Users with this access level have the same permission set as the bot owner. USE WITH DESCRETION!
        /// Permissions:
        ///    • Stop or restart the bot via command
        ///    • Manage user permissions (blacklist users, make them administrators) (*Hence the warning above*)
        ///    • Manage custom commands
        ///    
        /// Note: This will NOT give you access to guild-permission based commands, if you don't have needed guild permissions.
        /// </summary>
        Administrator = 2,
    }

    public class RegisteredEntity
    {
        public AccessLevels AccessLevel { get; set; }

        public ulong EntityID { get; set; }

        public bool WarnIfBlacklisted { get; set; }
    }
    
}
