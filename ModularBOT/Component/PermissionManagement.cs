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
using Discord.Commands;
using System.Threading;

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
        private static bool gotOwner = false;
        bool writingToDisk = false;
        
        public IReadOnlyCollection<RegisteredEntity> RegisteredEntities { get { return _entities.AsReadOnly(); } }
        public RegisteredEntity DefaultAdmin { get; private set; }

        public PermissionManager(IServiceProvider services)
        {
            _services = services;

            _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Creating default administrator info"));

            if (!gotOwner)
            {
                DefaultAdmin = new RegisteredEntity
                {
                    AccessLevel = AccessLevels.Administrator,
                    EntityID = _services.GetRequiredService<DiscordShardedClient>()
                .GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Id,
                    WarnIfBlacklisted = true//though this should never happen.
                };//This will not be added to list, as it doesn't count.
                gotOwner = true;
            }


            //load json.
            if (File.Exists("Permissions.cnf"))
            {
                using (StreamReader sr = new StreamReader("Permissions.cnf"))
                {
                    string json = sr.ReadToEnd();
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Permissions", "permissions.cnf file found! Attempting to populate RegisteredEntities"));
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
            
            
            if (item.Id == DefaultAdmin.EntityID)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Detected entity as bot owner."));
                return DefaultAdmin.AccessLevel;
            }

            RegisteredEntity df = _entities.FirstOrDefault(z => z.EntityID == item.Id);

            if (df!= null)
            {
                return df.AccessLevel;
            }
            if(df == null)
            {
                
                if (item is SocketGuildUser sgu)
                {
                    List<ulong> Common = sgu.Roles.Select(s1 => s1.Id).ToList().Intersect(_entities.Select(s2 => s2.EntityID).ToList()).ToList();
                    ulong r = 0;
                    if (Common.Count > 0)
                    {
                        r = Common.Max();
                    }
                    df = _entities.FirstOrDefault(xx => xx.EntityID == r);
                    if(df!= null)
                    {
                        return df.AccessLevel;
                    }
                }
            }
            _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Specified entity isn't on the list. Assume NORMAL."));
            return AccessLevels.Normal;
        }

        /// <summary>
        /// Check permission of an entity if registered, output inherited role.
        /// </summary>
        /// <param name="item">Item that interfaces with ISnowflakeEntity. Preferably IUser or IRole.</param>
        /// <returns>Item's accessLevel, or AccessLevels.Normal if nothing is found.</returns>
        /// <param name="inheritedRole">Returns an IRole if one is found, otherwise null.</param>
        public AccessLevels GetAccessLevel(ISnowflakeEntity item, out IRole inheritedRole, out bool BotOwner, out bool InList)
        {
            
            if (item.Id == DefaultAdmin.EntityID)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Detected entity as bot owner."));
                inheritedRole = null;
                BotOwner = true;
                InList = true;
                return DefaultAdmin.AccessLevel;
            }

            RegisteredEntity df = _entities.FirstOrDefault(z => z.EntityID == item.Id);

            if (df != null)
            {
                inheritedRole = null;
                BotOwner = false;
                InList = true;
                return df.AccessLevel;
            }
            if (df == null)
            {

                if (item is SocketGuildUser sgu)
                {
                    List<ulong> Common = sgu.Roles.Select(s1 => s1.Id).ToList().Intersect(_entities.Select(s2 => s2.EntityID).ToList()).ToList();
                    ulong r = 0;
                    if (Common.Count >0)
                    {
                        r = Common.Max();
                    }
                    
                    df = _entities.FirstOrDefault(xx => xx.EntityID == r);
                    if (df != null)
                    {
                        inheritedRole = sgu.Roles.FirstOrDefault(x=> x.Id==r) ?? null;
                        BotOwner = false;
                        InList = false;
                        return df.AccessLevel;
                    }
                }
            }
            _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Specified entity isn't on the list. Assume NORMAL."));
            inheritedRole = null;
            BotOwner = false;
            InList = false;
            return AccessLevels.Normal;
        }

        public bool IsEntityRegistered(ISnowflakeEntity item)
        {
            RegisteredEntity df = _entities.FirstOrDefault(z => z.EntityID == item.Id);
            return df != null;//true if exist
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
            if(user.Id == DefaultAdmin.EntityID)
            {
                throw new InvalidOperationException("You can't add or modify the bot owner's user permissions!");
            }
            RegisteredEntity r = new RegisteredEntity
            {
                EntityID = user.Id,
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
        /// Register a new role, or modify an existing.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="level"></param>
        /// <returns>0: if nothing changed. 1: if new user created; 2: if user edited; </returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal int RegisterEntity(IRole role, AccessLevels level)
        {
            if(level == AccessLevels.Blacklisted)
            {
                throw new InvalidOperationException("You can't blacklist an entire role.");
                //Unfortunately this would cause the warning system to work incorrectly.
                //Example: One user with BL role interacts with bot, gets the warning -> Warning flag resets.
                //         Another user with same BL role (who never got warned before) would NOT get warning.
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
        /// Register a new role, or modify an existing.
        /// </summary>
        /// <param name="Context">Context for this registration</param>
        /// <param name="genericID">ID this registration</param>
        /// <param name="level">Level for this registration</param>
        /// <returns>0: if nothing changed. 1: if new user created; 2: if user edited; </returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal int RegisterEntity(ICommandContext Context, ulong GenericID, AccessLevels level)
        {
            IRole role = null;
            IGuildUser user = null;
            IWebhook wuser = null;
            if(Context.Guild != null)
            {
                role = Context.Guild?.GetRole(GenericID);
                if(role == null)
                {
                    user = Context.Guild?.GetUserAsync(GenericID, CacheMode.AllowDownload).GetAwaiter().GetResult();
                    if(user == null)
                    {
                        wuser = Context.Guild?.GetWebhookAsync(GenericID).GetAwaiter().GetResult();
                        if(wuser == null)
                        {
                            throw new InvalidCastException("The entity id did not match a user, webhook, or role. Please make sure you got it right!");
                        }
                    }
                }
            }
            else
            {
                throw new InvalidCastException("You should use this command in a guild.");
            }
            if(role != null)
            {
                if (level == AccessLevels.Blacklisted)
                {
                    throw new InvalidOperationException("You can't blacklist an entire role.");
                    //Unfortunately this would cause the warning system to work incorrectly.
                    //Example: One user with BL role interacts with bot, gets the warning -> Warning flag resets.
                    //         Another user with same BL role (who never got warned before) would NOT get warning.
                }
            }
            RegisteredEntity r = new RegisteredEntity
            {
                EntityID = GenericID,
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
            if(entity.Id == DefaultAdmin.EntityID)
            {
                throw new InvalidOperationException("You can't remove the bot owner's user permissions!");
            }
            RegisteredEntity r = _entities.FirstOrDefault(x => x.EntityID == entity.Id);
            if(r!=null)
            {
                _entities.Remove(r);
                SaveJson();
                return true;
            }
            return false;
        }

        public void SaveJson()
        {
            SpinWait.SpinUntil(() => !writingToDisk);//wait until we are done here.
            if(writingToDisk)
            {
                return;//if something causes it to start writing again, just ignore it.
            }
            writingToDisk = true;
            try
            {
                using (StreamWriter sw = new StreamWriter("Permissions.cnf"))
                {
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Saving entities. Permissions.cnf"));
                    sw.WriteLine(JsonConvert.SerializeObject(RegisteredEntities,Formatting.Indented));
                    sw.Flush();
                    sw.Close();
                    _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Permissions", "Write success!"),ConsoleColor.Green);
                    writingToDisk = false;
                }
            }
            catch (Exception ex)
            {
                _services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Permissions", "Write failed!",ex));
                writingToDisk = false;
            }
        }

        public Embed GetAccessDeniedMessage(ICommandContext Context,AccessLevels requestedAccessLevel)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithTitle("Access Denied");
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithDescription($"You do not have permission to use this command. Requires `AccessLevels.{requestedAccessLevel.ToString()}` or higher.");
            b.WithColor(Color.Red);
            b.WithFooter("ModularBOT • Core");
            return b.Build();
        }

        public Embed GetAccessDeniedMessage(IUser AuthorUser, AccessLevels requestedAccessLevel)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithTitle("Access Denied");
            b.WithAuthor(AuthorUser);
            b.WithDescription($"You do not have permission to use this command. Requires `AccessLevels.{requestedAccessLevel.ToString()}` or higher.");
            b.WithColor(Color.Red);
            b.WithFooter("ModularBOT • Core");
            return b.Build();
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
