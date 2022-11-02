using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MajickSharp.Discord;

namespace WizardDashboard.Models
{
    public class DashboardUser
    {
        public string ID { get; internal set; }
        public string Username { get; internal set; }
        public string Avatar { get; internal set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public Dictionary<string, ServerData> Servers { get; internal set; }
        public DashboardUser(string new_id, string new_username, string new_avatar, string new_token, string new_refresh)
        {
            ID = new_id;
            Username = new_username;
            Avatar = new_avatar;
            Token = new_token;
            RefreshToken = new_refresh;
            Servers = new Dictionary<string, ServerData>();
        }
        public void SetServers(Dictionary<string, ServerData> new_servers)
        {
            Servers = new_servers;
        }
    }
    public class ServerData
    {
        public string ID { get; internal set; }
        public string Name { get; internal set; }
        public string Icon { get; internal set; }
        public bool IsOwner { get; internal set; }
        public int PermFlag { get; internal set; }
        public bool IsWizardServer { get; set; }
        public bool HasDashboardAccess { get; set; }
        public List<DiscordPermission> NamedPermissions { get; internal set; }
        public ServerData(string new_id, string new_name, string new_icon)
        {
            ID = new_id;
            Name = new_name;
            Icon = new_icon;
            IsOwner = false;
            PermFlag = 0;
            IsWizardServer = false;
            HasDashboardAccess = false;
            NamedPermissions = new List<DiscordPermission>();
        }
        public ServerData(string new_id, string new_name, string new_icon, bool is_owner, int new_permissions) 
        {
            ID = new_id;
            Name = new_name;
            Icon = new_icon;
            IsOwner = is_owner;
            PermFlag = new_permissions;            
            IsWizardServer = false;
            HasDashboardAccess = false;
            NamedPermissions = GetPermNames(new_permissions);
        }
        private List<DiscordPermission> GetPermNames(int perm_flag)
        {
            List<DiscordPermission> PermNames = new List<DiscordPermission>();
            if (perm_flag >= (int)DiscordPermission.MANAGE_EMOJIS)
            {
                PermNames.Add(DiscordPermission.MANAGE_EMOJIS);
                perm_flag -= (int)DiscordPermission.MANAGE_EMOJIS;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_WEBHOOKS)
            {
                PermNames.Add(DiscordPermission.MANAGE_WEBHOOKS);
                perm_flag -= (int)DiscordPermission.MANAGE_WEBHOOKS;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_ROLES)
            {
                PermNames.Add(DiscordPermission.MANAGE_ROLES);
                perm_flag -= (int)DiscordPermission.MANAGE_ROLES;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_NICKNAMES)
            {
                PermNames.Add(DiscordPermission.MANAGE_NICKNAMES);
                perm_flag -= (int)DiscordPermission.MANAGE_NICKNAMES;
            }
            if (perm_flag >= (int)DiscordPermission.CHANGE_NICKNAME)
            {
                PermNames.Add(DiscordPermission.CHANGE_NICKNAME);
                perm_flag -= (int)DiscordPermission.CHANGE_NICKNAME;
            }
            if (perm_flag >= (int)DiscordPermission.USE_VAD)
            {
                PermNames.Add(DiscordPermission.USE_VAD);
                perm_flag -= (int)DiscordPermission.USE_VAD;
            }
            if (perm_flag >= (int)DiscordPermission.MOVE_MEMBERS)
            {
                PermNames.Add(DiscordPermission.MOVE_MEMBERS);
                perm_flag -= (int)DiscordPermission.MOVE_MEMBERS;
            }
            if (perm_flag >= (int)DiscordPermission.DEAFEN_MEMBERS)
            {
                PermNames.Add(DiscordPermission.DEAFEN_MEMBERS);
                perm_flag -= (int)DiscordPermission.DEAFEN_MEMBERS;
            }
            if (perm_flag >= (int)DiscordPermission.MUTE_MEMBERS)
            {
                PermNames.Add(DiscordPermission.MUTE_MEMBERS);
                perm_flag -= (int)DiscordPermission.MUTE_MEMBERS;
            }
            if (perm_flag >= (int)DiscordPermission.SPEAK)
            {
                PermNames.Add(DiscordPermission.SPEAK);
                perm_flag -= (int)DiscordPermission.SPEAK;
            }
            if (perm_flag >= (int)DiscordPermission.CONNECT)
            {
                PermNames.Add(DiscordPermission.CONNECT);
                perm_flag -= (int)DiscordPermission.CONNECT;
            }
            if (perm_flag >= (int)DiscordPermission.VIEW_GUILD_INSIGHTS)
            {
                PermNames.Add(DiscordPermission.VIEW_GUILD_INSIGHTS);
                perm_flag -= (int)DiscordPermission.VIEW_GUILD_INSIGHTS;
            }
            if (perm_flag >= (int)DiscordPermission.USE_EXTERNAL_EMOJIS)
            {
                PermNames.Add(DiscordPermission.USE_EXTERNAL_EMOJIS);
                perm_flag -= (int)DiscordPermission.USE_EXTERNAL_EMOJIS;
            }
            if (perm_flag >= (int)DiscordPermission.MENTION_EVERYONE)
            {
                PermNames.Add(DiscordPermission.MENTION_EVERYONE);
                perm_flag -= (int)DiscordPermission.MENTION_EVERYONE;
            }
            if (perm_flag >= (int)DiscordPermission.READ_MESSAGE_HISTORY)
            {
                PermNames.Add(DiscordPermission.READ_MESSAGE_HISTORY);
                perm_flag -= (int)DiscordPermission.READ_MESSAGE_HISTORY;
            }
            if (perm_flag >= (int)DiscordPermission.ATTACH_FILES)
            {
                PermNames.Add(DiscordPermission.ATTACH_FILES);
                perm_flag -= (int)DiscordPermission.ATTACH_FILES;
            }
            if (perm_flag >= (int)DiscordPermission.EMBED_LINKS)
            {
                PermNames.Add(DiscordPermission.EMBED_LINKS);
                perm_flag -= (int)DiscordPermission.EMBED_LINKS;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_MESSAGES)
            {
                PermNames.Add(DiscordPermission.MANAGE_MESSAGES);
                perm_flag -= (int)DiscordPermission.MANAGE_MESSAGES;
            }
            if (perm_flag >= (int)DiscordPermission.SEND_TTS_MESSAGES)
            {
                PermNames.Add(DiscordPermission.SEND_TTS_MESSAGES);
                perm_flag -= (int)DiscordPermission.SEND_TTS_MESSAGES;
            }
            if (perm_flag >= (int)DiscordPermission.SEND_MESSAGES)
            {
                PermNames.Add(DiscordPermission.SEND_MESSAGES);
                perm_flag -= (int)DiscordPermission.SEND_MESSAGES;
            }
            if (perm_flag >= (int)DiscordPermission.VIEW_CHANNEL)
            {
                PermNames.Add(DiscordPermission.VIEW_CHANNEL);
                perm_flag -= (int)DiscordPermission.VIEW_CHANNEL;
            }
            if (perm_flag >= (int)DiscordPermission.STREAM)
            {
                PermNames.Add(DiscordPermission.STREAM);
                perm_flag -= (int)DiscordPermission.STREAM;
            }
            if (perm_flag >= (int)DiscordPermission.PRIORITY_SPEAKER)
            {
                PermNames.Add(DiscordPermission.PRIORITY_SPEAKER);
                perm_flag -= (int)DiscordPermission.PRIORITY_SPEAKER;
            }
            if (perm_flag >= (int)DiscordPermission.VIEW_AUDIT_LOG)
            {
                PermNames.Add(DiscordPermission.VIEW_AUDIT_LOG);
                perm_flag -= (int)DiscordPermission.VIEW_AUDIT_LOG;
            }
            if (perm_flag >= (int)DiscordPermission.ADD_REACTIONS)
            {
                PermNames.Add(DiscordPermission.ADD_REACTIONS);
                perm_flag -= (int)DiscordPermission.ADD_REACTIONS;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_GUILDS)
            {
                PermNames.Add(DiscordPermission.MANAGE_GUILDS);
                perm_flag -= (int)DiscordPermission.MANAGE_GUILDS;
            }
            if (perm_flag >= (int)DiscordPermission.MANAGE_CHANNELS)
            {
                PermNames.Add(DiscordPermission.MANAGE_CHANNELS);
                perm_flag -= (int)DiscordPermission.MANAGE_CHANNELS;
            }
            if (perm_flag >= (int)DiscordPermission.ADMINISTRATOR)
            {
                PermNames.Add(DiscordPermission.ADMINISTRATOR);
                perm_flag -= (int)DiscordPermission.ADMINISTRATOR;
            }
            if (perm_flag >= (int)DiscordPermission.BAN_MEMBERS)
            {
                PermNames.Add(DiscordPermission.BAN_MEMBERS);
                perm_flag -= (int)DiscordPermission.BAN_MEMBERS;
            }
            if (perm_flag >= (int)DiscordPermission.KICK_MEMBERS)
            {
                PermNames.Add(DiscordPermission.KICK_MEMBERS);
                perm_flag -= (int)DiscordPermission.KICK_MEMBERS;
            }
            if (perm_flag >= (int)DiscordPermission.CREATE_INSTANT_INVITE)
            {
                PermNames.Add(DiscordPermission.CREATE_INSTANT_INVITE);
                perm_flag -= (int)DiscordPermission.CREATE_INSTANT_INVITE;
            }
            return PermNames;
        }
    }
    public class DashboardServer
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsPremium { get; set; }
        public bool FullAccess { get; set; }
        public string UserID { get; set; }
        public string HelpdeskChannelID { get; set; }
        public string HelpdeskWebhookID { get; set; }
        public string WizardCategoryID { get; set; }
        public string LockoutChannelID { get; set; }
        public DashboardLinkup ServerLinkupData { get; set; }
        public ModeratorSettings ModerationSettings { get; set; }
        public DashboardVerification Verification { get; set; }
        public WizardAccessSettings AccessSettings { get; set; }
        public Dictionary<string, DashboardRole> Roles { get; internal set; }
        public Dictionary<string, DashboardChannel> Channels { get; internal set; }
        public Dictionary<string, DashboardPlugin> Plugins { get; internal set; }
        public Dictionary<string, DashboardRoleGroup> RoleGroups { get; internal set; }
        public Dictionary<string, ServerNavItem> OtherServers { get; internal set; }
        public DashboardServer()
        {
            FullAccess = false;
            AccessSettings = new WizardAccessSettings();
            ServerLinkupData = new DashboardLinkup();
            Verification = new DashboardVerification();
            Roles = new Dictionary<string, DashboardRole>();
            Channels = new Dictionary<string, DashboardChannel>();
            Plugins = new Dictionary<string, DashboardPlugin>();
            RoleGroups = new Dictionary<string, DashboardRoleGroup>();
            OtherServers = new Dictionary<string, ServerNavItem>();
        }
    }
    public class ServerNavItem
    {
        public string ID { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public bool IsWizardServer { get; set; }
        public ServerNavItem() { }
    }
    public class DashboardPlugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PluginCommand> Commands { get; internal set; }
        public List<string> CommandRoles { get; internal set; }
        public DashboardPlugin()
        {
            Commands = new List<PluginCommand>();
            CommandRoles = new List<string>();
        }
    }
    public class PluginCommand
    {
        public string Name { get; set; }
        public string Usage { get; set; }
        public string Description { get; set; }
        public PluginCommand() { }

    }
    public class DashboardChannel
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public ChannelType Type { get; set; }
        public DashboardChannel() { }
    }
    public class DashboardRole
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public int Position { get; set; }
        public int PermFlag { get; set; }
        public DashboardRole() { }
    }
    public class DashboardRoleGroup
    {
        public bool RequireAll { get; set; }
        public string BaseRoleID { get; set; }
        public List<string> GroupedRoleIDs { get; internal set; }
        public List<string> RejectedRoleIDs { get; internal set; }
        public List<string> RequiredRoleIDs { get; internal set; }
        public List<string> AllowedGroupRoleIDs { get; internal set; }
        public List<string> AllowedRejectedRoleIDs { get; internal set; }
        public List<string> AllowedRequiredRoleIDs { get; internal set; }
        public DashboardRoleGroup()
        {
            GroupedRoleIDs = new List<string>();
            RejectedRoleIDs = new List<string>();
            RequiredRoleIDs = new List<string>();
            AllowedGroupRoleIDs = new List<string>();
            AllowedRejectedRoleIDs = new List<string>();
            AllowedRequiredRoleIDs = new List<string>();
        }
    }
    public class DashboardLinkup
    {
        public string LinkupChannelID { get; set; }
        public string LinkupWebhookID { get; set; }
        public string PremiumLinkupChannelID { get; set; }
        public string PremiumLinkupWebhookID { get; set; }
        public Dictionary<string, ServerData> LinkupConnections { get; set; }
        public Dictionary<string, ServerData> PremiumConnections { get; set; }
        public DashboardLinkup()
        {
            LinkupConnections = new Dictionary<string, ServerData>();
            PremiumConnections = new Dictionary<string, ServerData>();
        }
    }
    public class DashboardVerification
    {
        public string VerifyRoleID { get; set; }
        public string MemberRoleID { get; set; }
        public string VerifyChannelID { get; set; }
        public string VerifyMessageID { get; set; }
        public string VerifyMessageText { get; set; }
        public DashboardVerification() { }
    }
    public class WizardAccessSettings
    {
        public string CommandPrefix { get; set; }
        public List<string> ImmunityRoles { get; set; }
        public List<string> CommandRoles { get; set; }
        public List<string> RejectedRoles { get; set; }
        public List<string> LinkupRoles { get; set; }
        public List<string> DashboardRoles { get; set; }
        public List<string> CommandUsers { get; set; }
        public List<string> RejectedUsers { get; set; }
        public List<string> LinkupUsers { get; set; }
        public List<string> DashboardUsers { get; set; }
        public WizardAccessSettings()
        {
            CommandPrefix = "^";
            ImmunityRoles = new List<string>();
            CommandRoles = new List<string>();
            RejectedRoles = new List<string>();
            LinkupRoles = new List<string>();
            DashboardRoles = new List<string>();
            CommandUsers = new List<string>();
            RejectedUsers = new List<string>();
            LinkupUsers = new List<string>();
            DashboardUsers = new List<string>();
        }
    }
    public class ModeratorSettings
    {
        public string MutedRoleID { get; set; }
        public string LockoutChannelID { get; set; }
        public string LockoutRoleID { get; set; }
        public ModeratorSettings() { }
    }
}