using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RestSharp;
using System.Data.OleDb;
using MajickSharp.Discord;
using WizardDashboard.Models;
using System.Net;

namespace WizardDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private const string client_id = "";
        private const string client_secret = "";
        private const string token = "";
        private static OleDbCommand command;
        private static OleDbDataReader reader;
        private static OleDbConnection connection = new OleDbConnection("Provider=SQLOLEDB;" +
                "Data Source=SERVER\\SQLEXPRESS;" +
                "Initial Catalog=NAME;" +
                "Integrated Security=SSPI;");
        private static DashboardUser current_user;
        private static DashboardServer current_server;
        // GET: Dashboard
        [HttpGet]
        public ActionResult Index(string user_id = "", string server_id = "")
        {
            string token;
            string username;
            string avatar;
            string refresh_token;
            bool account_premium;
            Dictionary<string, ServerData> Servers = new Dictionary<string, ServerData>();
            //If they've left the dashboard and returned. Log them back in
            if (user_id == "" | user_id == null) { return RedirectToAction("Login", "Interface"); }
            //Pull their data by ID from the database and populate the model
            if(connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Get_DashboardUser '" + user_id + "'", connection);
            reader = command.ExecuteReader();
            reader.Read();
            token = reader.GetString(0);
            username = reader.GetString(1);
            avatar = reader.GetString(2);
            refresh_token = reader.GetString(3);
            account_premium = reader.GetBoolean(4);
            connection.Close();
            current_user = new DashboardUser(user_id, username, avatar, token, refresh_token);
            //Get their servers
            MajickSharp.Json.JsonObject UserGuildResponseContent;
            RestClient UserGuildClient = new RestClient("https://discord.com/api");
            RestRequest UserGuildRequest = new RestRequest("/users/@me/guilds", Method.GET);
            UserGuildRequest.RequestFormat = DataFormat.Json;
            UserGuildRequest.AddParameter("client_id", client_id);
            UserGuildRequest.AddParameter("client_secret", client_secret);
            UserGuildRequest.AddHeader("Content-Type", "application/json");
            UserGuildRequest.AddHeader("Authorization", "Bearer " + current_user.Token);
            IRestResponse UserGuildResponse = UserGuildClient.Execute(UserGuildRequest);
            UserGuildResponseContent = new MajickSharp.Json.JsonObject(UserGuildResponse.Content);
            foreach (MajickSharp.Json.JsonObject json_server in UserGuildResponseContent.ObjectLists["objects"])
            {
                int perm_flag = 0;
                bool is_owner = false;
                bool is_wizard_server = false;
                string user_server_id = json_server.Attributes["id"].text_value;
                string server_name = json_server.Attributes["name"].text_value;
                string server_icon = json_server.Attributes["icon"].text_value;
                bool.TryParse(json_server.Attributes["owner"].text_value, out is_owner);
                int.TryParse(json_server.Attributes["permissions"].text_value, out perm_flag);
                ServerData current_server = new ServerData(user_server_id, server_name, server_icon, is_owner, perm_flag);
                //check here for dashboard access
                string dash_role_list = "";
                string dash_user_list = "";
                string dash_rejected_roles = "";
                string dash_rejected_users = "";
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Get_DashboardAccess '" + user_server_id + "'", connection);
                reader = command.ExecuteReader();
                reader.Read();
                if (reader.HasRows)
                {
                    is_wizard_server = true;
                    dash_role_list = reader.GetString(0);
                    dash_user_list = reader.GetString(1);
                    dash_rejected_roles = reader.GetString(2);
                    dash_rejected_users = reader.GetString(3);
                }
                connection.Close();
                if (is_wizard_server)
                {
                    RestClient GuildClient = new RestClient("https://discord.com/api");
                    RestRequest GuildRequest = new RestRequest("/guilds/" + user_server_id, Method.GET);
                    MajickSharp.Json.JsonObject GuildResponseContent;
                    GuildRequest.RequestFormat = DataFormat.Json;
                    GuildRequest.AddParameter("client_id", client_id);
                    GuildRequest.AddParameter("client_secret", client_secret);
                    GuildRequest.AddHeader("Content-Type", "application/json");
                    GuildRequest.AddHeader("Authorization", "Bot " + token);
                    IRestResponse GuildResponse = GuildClient.Execute(GuildRequest);
                    GuildResponseContent = new MajickSharp.Json.JsonObject(GuildResponse.Content);
                    DiscordGuild server = new DiscordGuild(GuildResponseContent);
                    if (server.members.ContainsKey(current_user.ID))
                    {
                        foreach (string dash_role in dash_role_list.Split(','))
                        {
                            if (server.members[current_user.ID].roles.Contains(dash_role))
                            {
                                current_server.HasDashboardAccess = true;
                                break;
                            }
                        }
                        foreach (string rejected_role in dash_rejected_roles.Split(','))
                        {
                            if (server.members[current_user.ID].roles.Contains(rejected_role))
                            {
                                current_server.HasDashboardAccess = false;
                                break;
                            }
                        }
                        List<string> DashboardUsers = dash_user_list.Split(',').ToList();
                        List<string> RejectedUsers = dash_rejected_users.Split(',').ToList();
                        if (DashboardUsers.Contains(current_user.ID)) { current_server.HasDashboardAccess = true; }
                        if (RejectedUsers.Contains(current_user.ID)) { current_server.HasDashboardAccess = false; }
                    }
                    else if(current_server.ID == "526042568601894913" && current_user.ID == "165335722289528835")
                    {
                        current_server.HasDashboardAccess = true;
                    }
                }
                Servers.Add(current_server.ID, current_server);
            }
            current_user.SetServers(Servers);
            //Once clicking into the Guild will need to get the data as Wizard for all roles/channels
            //Also that's when they'll get to add the bot
            return RedirectToAction("Servers");
        }
        [HttpGet]
        public ActionResult Servers(string server_id = "")
        {
            //Will need to check against their current server id if it exists.
            WizardAccessSettings current_settings = new WizardAccessSettings();
            current_server = new DashboardServer();
            //Get the server data here as Wizard
            if (current_user == null) { return RedirectToAction("Login", "Interface"); }
            current_server.UserID = current_user.ID;
            if (server_id == "" | server_id == null)
            {
                current_server.ID = "NOSERVER";
                //Check to see which servers the user has permission to access the Dashboard for to add the icon to the server nav
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                foreach (ServerData user_server in current_user.Servers.Values)
                {
                    string dash_role_list = "";
                    string dash_user_list = "";
                    string dash_rejected_roles = "";
                    string dash_rejected_users = "";
                    ServerNavItem server_nav = new ServerNavItem();
                    command = new OleDbCommand("P_Get_IsWizardServer '" + user_server.ID + "'", connection);
                    reader = command.ExecuteReader();
                    reader.Read();
                    if (reader.GetBoolean(0)) { server_nav.IsWizardServer = true; }
                    else { server_nav.IsWizardServer = false; }
                    command = new OleDbCommand("P_Get_DashboardAccess '" + user_server + "'", connection);
                    reader = command.ExecuteReader();
                    reader.Read();
                    if (reader.HasRows)
                    {
                        server_nav.IsWizardServer = true;
                        dash_role_list = reader.GetString(0);
                        dash_user_list = reader.GetString(1);
                        dash_rejected_roles = reader.GetString(2);
                        dash_rejected_users = reader.GetString(3);
                    }
                    server_nav.ID = user_server.ID;
                    server_nav.Name = user_server.Name;
                    server_nav.Icon = user_server.Icon;
                    if (user_server.NamedPermissions.Contains(DiscordPermission.ADMINISTRATOR) | user_server.IsOwner)
                    {
                        //Only the server owner and administrators will have "full access" to the dashboard
                        //Full Access: can change Dashboard and Immunity roles, probably some other things
                        //Have a log for Dahsboard commits, list changes, and what user posted it.
                        if (user_server.ID == current_server.ID && user_server.IsOwner) { current_server.FullAccess = true; }
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                    else if (user_server.HasDashboardAccess)
                    {
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                }
                connection.Close();
                return View(current_server);
            }
            System.Threading.Thread.Sleep(150);
            if (IsWizardServer(server_id))
            {
                MajickSharp.Json.JsonObject GuildResponseContent;
                RestClient GuildClient = new RestClient("https://discord.com/api");
                RestRequest GuildRequest = new RestRequest("/guilds/" + server_id, Method.GET);
                GuildRequest.RequestFormat = DataFormat.Json;
                GuildRequest.AddParameter("client_id", client_id);
                GuildRequest.AddParameter("client_secret", client_secret);
                GuildRequest.AddHeader("Content-Type", "application/json");
                GuildRequest.AddHeader("Authorization", "Bot " + token);
                IRestResponse GuildResponse = GuildClient.Execute(GuildRequest);
                GuildResponseContent = new MajickSharp.Json.JsonObject(GuildResponse.Content);
                DiscordGuild server = new DiscordGuild(GuildResponseContent);
                current_server.ID = server.id;
                current_server.Name = server.name;
                current_server.Icon = server.icon;
                foreach (DiscordRole role in server.roles.Values)
                {
                    DashboardRole dash_role = new DashboardRole();
                    dash_role.ID = role.id;
                    dash_role.Name = role.name;
                    dash_role.Color = ConvertToHex(role.color);
                    dash_role.Position = role.position;
                    dash_role.PermFlag = role.permissions;
                    current_server.Roles.Add(dash_role.ID, dash_role);
                }
                MajickSharp.Json.JsonObject ChannelResponseContent;
                RestClient ChannelClient = new RestClient("https://discord.com/api");
                RestRequest ChannelRequest = new RestRequest("/guilds/" + server_id + "/channels", Method.GET);
                ChannelRequest.RequestFormat = DataFormat.Json;
                ChannelRequest.AddParameter("client_id", client_id);
                ChannelRequest.AddParameter("client_secret", client_secret);
                ChannelRequest.AddHeader("Content-Type", "application/json");
                ChannelRequest.AddHeader("Authorization", "Bot " + token);
                IRestResponse ChannelResponse = ChannelClient.Execute(ChannelRequest);
                ChannelResponseContent = new MajickSharp.Json.JsonObject(ChannelResponse.Content);
                foreach (MajickSharp.Json.JsonObject json_channel in ChannelResponseContent.ObjectLists["objects"])
                {
                    DiscordChannel channel = new DiscordChannel(json_channel);
                    DashboardChannel dash_channel = new DashboardChannel();
                    dash_channel.ID = channel.id;
                    dash_channel.Name = channel.name;
                    dash_channel.Type = channel.type;
                    current_server.Channels.Add(dash_channel.ID, dash_channel);
                }
                if (current_user == null) { return RedirectToAction("Login", "Interface"); }
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                //Check to see which servers the user has permission to access the Dashboard for to add the icon to the server nav
                foreach (ServerData user_server in current_user.Servers.Values)
                {
                    ServerNavItem server_nav = new ServerNavItem();
                    command = new OleDbCommand("P_Get_IsWizardServer '" + user_server.ID + "'", connection);
                    reader = command.ExecuteReader();
                    reader.Read();
                    if (reader.GetBoolean(0)) { server_nav.IsWizardServer = true; }
                    else { server_nav.IsWizardServer = false; }
                    server_nav.ID = user_server.ID;
                    server_nav.Name = user_server.Name;
                    server_nav.Icon = user_server.Icon;
                    if (user_server.NamedPermissions.Contains(DiscordPermission.ADMINISTRATOR) | user_server.IsOwner)
                    {
                        //Only the server owner and administrators will have "full access" to the dashboard
                        //Full Access: can change Dashboard and Immunity roles, probably some other things
                        //Have a log for Dahsboard commits, list changes, and what user posted it.
                        if (user_server.ID == current_server.ID && user_server.IsOwner) { current_server.FullAccess = true; }
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                    else if (user_server.HasDashboardAccess)
                    {
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                }
                //Get the plugin data
                command = new OleDbCommand("P_Get_DashboardPlugins", connection);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string plugin_name = reader.GetString(0);
                    if (plugin_name != "help" && plugin_name != "majick")
                    {
                        DashboardPlugin plugin = new DashboardPlugin();
                        plugin.Name = plugin_name;

                        current_server.Plugins.Add(plugin.Name, plugin);
                    }
                }
                foreach (DashboardPlugin plugin in current_server.Plugins.Values)
                {
                    //Fill in the command roles
                    command = new OleDbCommand("P_Get_PluginCommandRoles '" + current_server.ID + "','" + plugin.Name + "'", connection);
                    reader = command.ExecuteReader();
                    reader.Read();
                    string command_roles = reader.GetString(0);
                    foreach (string command_role_id in command_roles.Split(','))
                    {
                        plugin.CommandRoles.Add(command_role_id);
                    }
                    //Fill in the command data
                    command = new OleDbCommand("P_Get_DashboardPluginCommands '" + plugin.Name + "'", connection);
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string command_name = reader.GetString(0);
                        string command_usage = reader.GetString(1);
                        string command_description = reader.GetString(2);
                        PluginCommand command = new PluginCommand();
                        command.Name = command_name;
                        command.Usage = command_usage;
                        command.Description = command_description;
                        plugin.Commands.Add(command);
                    }
                }
                //Get server settings data here
                command = new OleDbCommand("P_Get_DashboardSettings '" + server_id + "'", connection);
                reader = command.ExecuteReader();
                reader.Read();
                //This is where you assign the lists of roles and users that are allowed/disallowed from using Wizard. (Registered Muted role is in Moderator)
                string command_prefix = reader.GetString(0);
                string immunity_role_list = reader.GetString(1);
                string command_role_list = reader.GetString(2);
                string linkup_role_list = reader.GetString(3);
                string rejected_role_list = reader.GetString(4);
                string dashboard_role_list = reader.GetString(5);
                string command_user_list = reader.GetString(6);
                string linkup_user_list = reader.GetString(7);
                string rejected_user_list = reader.GetString(8);
                string dashboard_user_list = reader.GetString(9);
                string helpdesk_channel_id = reader.GetString(10);
                string helpdesk_webhook_id = reader.GetString(11);
                string wizard_category_id = reader.GetString(12);
                string lockout_channe_id = reader.GetString(13);
                current_settings.CommandPrefix = command_prefix;
                current_settings.ImmunityRoles = immunity_role_list.Split(',').ToList();
                current_settings.CommandRoles = command_role_list.Split(',').ToList();
                current_settings.LinkupRoles = linkup_role_list.Split(',').ToList();
                current_settings.RejectedRoles = rejected_role_list.Split(',').ToList();
                current_settings.DashboardRoles = dashboard_role_list.Split(',').ToList();
                current_settings.CommandUsers = command_user_list.Split(',').ToList();
                current_settings.LinkupUsers = linkup_user_list.Split(',').ToList();
                current_settings.RejectedUsers = rejected_user_list.Split(',').ToList();
                current_settings.DashboardUsers = dashboard_user_list.Split(',').ToList();
                current_server.AccessSettings = current_settings;
                current_server.HelpdeskChannelID = helpdesk_channel_id;
                current_server.HelpdeskWebhookID = helpdesk_webhook_id;
                current_server.WizardCategoryID = wizard_category_id;
                current_server.LockoutChannelID = lockout_channe_id;
                //Fill server role groups here
                command = new OleDbCommand("P_Get_ServerGroupRoles '" + current_server.ID + "'", connection);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string group_role_ids;
                    string rejected_role_ids;
                    string required_role_ids;
                    DashboardRoleGroup current_group = new DashboardRoleGroup();
                    current_group.BaseRoleID = reader.GetString(0);
                    group_role_ids = reader.GetString(1);
                    rejected_role_ids = reader.GetString(2);
                    required_role_ids = reader.GetString(3);
                    current_group.RequireAll = reader.GetBoolean(4);
                    current_group.GroupedRoleIDs = group_role_ids.Split(',').ToList();
                    current_group.RejectedRoleIDs = rejected_role_ids.Split(',').ToList();
                    current_group.RequiredRoleIDs = required_role_ids.Split(',').ToList();
                    current_server.RoleGroups.Add(current_group.BaseRoleID, current_group);
                }
                //Fill the lists for the select-boxes here
                bool group_role_allowed;
                bool rejected_role_allowed;
                bool required_role_allowed;
                foreach (DashboardRole current_role in current_server.Roles.Values)
                {
                    if (current_role.Name != "@everyone")
                    {
                        foreach (DashboardRoleGroup role_group in current_server.RoleGroups.Values)
                        {
                            //Any role can be a base role
                            //Role groups will have a limit of 2 unique assignments per group, and 3 groups per server for free
                            //Role groups will have up to 6 unique assignments and as many assignments are are supported for premium
                            //Any role can be either grouped, rejected, or required only once on any role grouping. cannot include itself. does not include @everyone
                            group_role_allowed = false;
                            rejected_role_allowed = false;
                            required_role_allowed = false;
                            if (!role_group.GroupedRoleIDs.Contains(current_role.ID))
                            {
                                if (!role_group.RejectedRoleIDs.Contains(current_role.ID) && !role_group.RequiredRoleIDs.Contains(current_role.ID))
                                {
                                    group_role_allowed = true;
                                    if (current_role.ID == role_group.BaseRoleID) { group_role_allowed = false; }
                                    //If the current role is already a base role
                                    if (current_server.RoleGroups.ContainsKey(current_role.ID))
                                    {
                                        //A Role can NOT group a role that also already requires it
                                        if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                        //A Role can NOT group a role that also already rejects it
                                        if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                        foreach (string rejected_role_id in role_group.RejectedRoleIDs)
                                        {
                                            if (role_group.RequireAll)
                                            {
                                                //A Role can NOT group a role that requires a role that it rejects, if require all is set for that role (will cause a fight in add/remove)
                                                if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(rejected_role_id))
                                                {
                                                    group_role_allowed = false;
                                                    break;
                                                }
                                            }
                                            //A Role can NOT group a role that groups a role that is already rejected (will cause a fight in add/remove)
                                            if (current_server.RoleGroups[current_role.ID].GroupedRoleIDs.Contains(rejected_role_id))
                                            {
                                                group_role_allowed = false;
                                                break;
                                            }
                                        }
                                        foreach (string grouped_role_id in role_group.GroupedRoleIDs)
                                        {
                                            if (role_group.RequireAll)
                                            {
                                                //A Role can NOT group a role that rejects a role that is already grouped (will cause a fight in add/remove)
                                                if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(grouped_role_id))
                                                {
                                                    group_role_allowed = false;
                                                    break;
                                                }
                                            }
                                        }
                                        //A Role can NOT group a role that requires a role that is not also required
                                        foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                        {
                                            if (!role_group.RequiredRoleIDs.Contains(required_role_id))
                                            {
                                                group_role_allowed = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (!role_group.RejectedRoleIDs.Contains(current_role.ID))
                            {
                                if (!role_group.GroupedRoleIDs.Contains(current_role.ID) && !role_group.RequiredRoleIDs.Contains(current_role.ID))
                                {
                                    rejected_role_allowed = true;
                                    if (current_role.ID == role_group.BaseRoleID) { rejected_role_allowed = false; }
                                    if (current_server.RoleGroups.ContainsKey(current_role.ID))
                                    {
                                        //A Role can NOT reject a role that also already groups it
                                        if (current_server.RoleGroups[current_role.ID].GroupedRoleIDs.Contains(role_group.BaseRoleID)) { rejected_role_allowed = false; }
                                        //A Role can NOT reject a role that also already requires it
                                        if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                        foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                        {
                                            if (current_server.RoleGroups.ContainsKey(required_role_id))
                                            {
                                                //A Role can NOT reject a role that is grouped to a required role (will cause a fight in add/remove)
                                                if (current_server.RoleGroups[required_role_id].GroupedRoleIDs.Contains(current_role.ID))
                                                {
                                                    rejected_role_allowed = false;
                                                    break;
                                                }
                                                //A Role can NOT reject a role that is required by a required role (will cause a fight in add/remove)
                                                if (current_server.RoleGroups[required_role_id].RequiredRoleIDs.Contains(current_role.ID))
                                                {
                                                    rejected_role_allowed = false;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    foreach (string grouped_role_id in role_group.GroupedRoleIDs)
                                    {
                                        if (current_server.RoleGroups.ContainsKey(grouped_role_id))
                                        {
                                            //A Role can NOT reject a role that is grouped to grouped role (will cause a fight in add/remove)
                                            if (current_server.RoleGroups[grouped_role_id].GroupedRoleIDs.Contains(current_role.ID))
                                            {
                                                rejected_role_allowed = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (!role_group.RequiredRoleIDs.Contains(current_role.ID))
                            {
                                if (!role_group.GroupedRoleIDs.Contains(current_role.ID) && !role_group.RejectedRoleIDs.Contains(current_role.ID))
                                {
                                    required_role_allowed = true;
                                    if (current_role.ID == role_group.BaseRoleID) { required_role_allowed = false; }
                                    if (current_server.RoleGroups.ContainsKey(current_role.ID))
                                    {
                                        //A Role can NOT require a role that also already rejects it
                                        if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(role_group.BaseRoleID)) { required_role_allowed = false; }
                                        //A Role can NOT require a role that also already requires it
                                        if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { required_role_allowed = false; }
                                        foreach (string grouped_role_id in current_server.RoleGroups[current_role.ID].GroupedRoleIDs)
                                        {
                                            if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(grouped_role_id))
                                            {
                                                //A Role can NOT require a role that rejects a grouped role
                                                required_role_allowed = false;
                                                break;
                                            }
                                            if (current_server.RoleGroups.ContainsKey(grouped_role_id))
                                            {
                                                if (current_server.RoleGroups[grouped_role_id].RejectedRoleIDs.Contains(current_role.ID))
                                                {
                                                    //A Role can NOT require a role that is rejected by a grouped role
                                                    required_role_allowed = false;
                                                    break;
                                                }
                                            }
                                        }
                                        foreach (string rejected_role_id in role_group.RejectedRoleIDs)
                                        {
                                            if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(rejected_role_id))
                                            {
                                                //A Role can NOT require a role that requires a rejected role
                                                required_role_allowed = false;
                                                break;
                                            }
                                        }
                                        foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                        {
                                            if (role_group.RequireAll)
                                            {
                                                //A Role can NOT require a role that rejects another required role, if requre all is set
                                                if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(required_role_id))
                                                {
                                                    required_role_allowed = false;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (group_role_allowed) { role_group.AllowedGroupRoleIDs.Add(current_role.ID); }
                            if (rejected_role_allowed) { role_group.AllowedRejectedRoleIDs.Add(current_role.ID); }
                            if (required_role_allowed) { role_group.AllowedRequiredRoleIDs.Add(current_role.ID); }
                        }
                    }
                }
                //Fill server Linkup Connections here
                command = new OleDbCommand("P_Get_ServerLinkupData '" + current_server.ID + "'", connection);
                reader = command.ExecuteReader();
                reader.Read();
                string linkup_channel_id = reader.GetString(0);
                string linkup_webhook_id = reader.GetString(1);
                string linkup_connections = reader.GetString(2);
                string premium_linkup_channel = reader.GetString(3);
                string premium_linkup_webhook = reader.GetString(4);
                string premium_linkup_connections = reader.GetString(5);
                List<string> LinkupConnections = linkup_connections.Split(',').ToList();
                List<string> PremiumLinkupConnections = premium_linkup_connections.Split(',').ToList();
                Dictionary<string, ServerData> BasicConnections = new Dictionary<string, ServerData>();
                Dictionary<string, ServerData> PremiumConnections = new Dictionary<string, ServerData>();
                DashboardLinkup LinkupData = new DashboardLinkup();
                LinkupData.LinkupChannelID = linkup_channel_id;
                LinkupData.LinkupWebhookID = linkup_webhook_id;
                LinkupData.LinkupConnections = BasicConnections;
                LinkupData.PremiumLinkupChannelID = premium_linkup_channel;
                LinkupData.PremiumLinkupWebhookID = premium_linkup_webhook;
                LinkupData.PremiumConnections = PremiumConnections;
                current_server.ServerLinkupData = LinkupData;
                foreach (string linkup_id in LinkupConnections)
                {
                    if (linkup_id != "")
                    {
                        MajickSharp.Json.JsonObject LinkupResponseContent;
                        RestClient LinkupClient = new RestClient("https://discord.com/api");
                        RestRequest LinkupRequest = new RestRequest("/guilds/" + linkup_id, Method.GET);
                        LinkupRequest.RequestFormat = DataFormat.Json;
                        LinkupRequest.AddParameter("client_id", client_id);
                        LinkupRequest.AddParameter("client_secret", client_secret);
                        LinkupRequest.AddHeader("Content-Type", "application/json");
                        LinkupRequest.AddHeader("Authorization", "Bot " + token);
                        IRestResponse LinkupResponse = LinkupClient.Execute(LinkupRequest);
                        LinkupResponseContent = new MajickSharp.Json.JsonObject(LinkupResponse.Content);
                        if (LinkupResponse.StatusCode == HttpStatusCode.OK)
                        {
                            ServerData LinkupServer = new ServerData(LinkupResponseContent.Attributes["id"].text_value, LinkupResponseContent.Attributes["name"].text_value, LinkupResponseContent.Attributes["icon"].text_value);
                            if (!BasicConnections.ContainsKey(LinkupServer.ID)) { BasicConnections.Add(LinkupServer.ID, LinkupServer); }
                        }
                    }
                }
                foreach (string premium_id in PremiumLinkupConnections)
                {
                    if (premium_id != "")
                    {
                        MajickSharp.Json.JsonObject PremiumResponseContent;
                        RestClient PremiumClient = new RestClient("https://discord.com/api");
                        RestRequest PremiumRequest = new RestRequest("/guilds/" + premium_id, Method.GET);
                        PremiumRequest.RequestFormat = DataFormat.Json;
                        PremiumRequest.AddParameter("client_id", client_id);
                        PremiumRequest.AddParameter("client_secret", client_secret); ;
                        PremiumRequest.AddHeader("Content-Type", "application/json");
                        PremiumRequest.AddHeader("Authorization", "Bot " + token);
                        IRestResponse PremiumResponse = PremiumClient.Execute(PremiumRequest);
                        PremiumResponseContent = new MajickSharp.Json.JsonObject(PremiumResponse.Content);
                        ServerData PremiumServer = new ServerData(PremiumResponseContent.Attributes["id"].text_value, PremiumResponseContent.Attributes["name"].text_value, PremiumResponseContent.Attributes["icon"].text_value);
                        if (!PremiumConnections.ContainsKey(PremiumServer.ID)) { PremiumConnections.Add(PremiumServer.ID, PremiumServer); }
                    }
                }
                //Get moderator plugin things
                command = new OleDbCommand("P_Get_DashboardModerator '" + server_id + "'", connection);
                reader = command.ExecuteReader();
                reader.Read();
                ModeratorSettings mod_settings = new ModeratorSettings();
                mod_settings.MutedRoleID = reader.GetString(0);
                mod_settings.LockoutChannelID = reader.GetString(1);
                mod_settings.LockoutRoleID = reader.GetString(2);
                current_server.ModerationSettings = mod_settings;
                //Get verification plugin things
                command = new OleDbCommand("P_Get_WizardVerification '" + server_id + "'", connection);
                reader = command.ExecuteReader();
                reader.Read();
                DashboardVerification verification = new DashboardVerification();
                verification.VerifyRoleID = reader.GetString(0);
                verification.MemberRoleID = reader.GetString(1);
                verification.VerifyChannelID = reader.GetString(2);
                verification.VerifyMessageID = reader.GetString(3);
                verification.VerifyMessageText = reader.GetString(4);
                connection.Close();
                current_server.Verification = verification;
            }
            else 
            {
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                //Check to see which servers the user has permission to access the Dashboard for to add the icon to the server nav
                foreach (ServerData user_server in current_user.Servers.Values)
                {
                    ServerNavItem server_nav = new ServerNavItem();
                    command = new OleDbCommand("P_Get_IsWizardServer '" + user_server.ID + "'", connection);
                    reader = command.ExecuteReader();
                    reader.Read();
                    if (reader.GetBoolean(0)) { server_nav.IsWizardServer = true; }
                    else { server_nav.IsWizardServer = false; }
                    server_nav.ID = user_server.ID;
                    server_nav.Name = user_server.Name;
                    server_nav.Icon = user_server.Icon;
                    if (user_server.NamedPermissions.Contains(DiscordPermission.ADMINISTRATOR) | user_server.IsOwner)
                    {
                        //Only the server owner and administrators will have "full access" to the dashboard
                        //Full Access: can change Dashboard and Immunity roles, probably some other things
                        //Have a log for Dahsboard commits, list changes, and what user posted it.
                        if (user_server.ID == current_server.ID && user_server.IsOwner) { current_server.FullAccess = true; }
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                    else if (user_server.HasDashboardAccess)
                    {
                        current_server.OtherServers.Add(server_nav.ID, server_nav);
                    }
                }
                connection.Close();
                current_server.ID = "NO_WIZARD%" + server_id;
                current_server.Name = current_user.Servers[server_id].Name;
            }
            return View(current_server);
        }
        private bool IsWizardServer(string server_id)
        {
            bool WizardServer;
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Get_IsWizardServer '" + server_id + "'", connection);
            reader = command.ExecuteReader();
            reader.Read();
            WizardServer = reader.GetBoolean(0);
            connection.Close();
            return WizardServer;
        }
        private string ConvertToHex(int color_flag)
        {
            string hex_code = "";
            int top_red;
            int bottom_red;
            int top_green;
            int bottom_green;
            int top_blue;
            int bottom_blue;
            bottom_blue = color_flag % 16;
            color_flag -= bottom_blue;
            color_flag = color_flag / 16;
            top_blue = color_flag % 16;
            color_flag -= top_blue;
            color_flag = color_flag / 16;
            bottom_green = color_flag % 16;
            color_flag -= bottom_green;
            color_flag = color_flag / 16;
            top_green = color_flag % 16;
            color_flag -= top_green;
            color_flag = color_flag / 16;
            bottom_red = color_flag % 16;
            color_flag -= bottom_red;
            color_flag = color_flag / 16;
            top_red = color_flag;
            hex_code = "#" + GetHexValue(top_red) + GetHexValue(bottom_red) + GetHexValue(top_green) + GetHexValue(bottom_green) + GetHexValue(top_blue) + GetHexValue(bottom_blue);
            if (hex_code == "#000000") { return "#999999"; }
            return hex_code;
        }
        private string GetHexValue(int digit)
        {
            string value = "";
            if (digit > 9)
            {
                switch (digit)
                {
                    case 10:
                        value = "A";
                        break;
                    case 11:
                        value = "B";
                        break;
                    case 12:
                        value = "C";
                        break;
                    case 13:
                        value = "D";
                        break;
                    case 14:
                        value = "E";
                        break;
                    case 15:
                        value = "F";
                        break;
                }
            }
            else { value = digit.ToString(); }
            return value;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CommitRoleGroupCreated(string server_id, string role_id, string user_id)
        {
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Create Role Group','" + role_id + "'", connection);
            reader = command.ExecuteReader();
            command = new OleDbCommand("P_Ins_NewRoleGroup '" + server_id + "','" + role_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();
            return true;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CommitRoleAdded(string data_string)
        {
            string[] data_values = data_string.Split('_');
            string plugin = data_values[0];
            string type = data_values[1];
            string user_id = data_values[2];
            string server_id = data_values[3];
            string role_id = data_values[4];
            string rolegroup_role_id = "";
            if (data_values.Length == 6) { rolegroup_role_id = data_values[5]; }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            switch (plugin)
            {
                case "settings":
                    switch (type)
                    {
                        case "immunity":
                            //Add the immunity role to the database and return
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Immunity Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_AddImmunityRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "command":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Wizard Command Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_AddCommandRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "rejected":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Wizard Rejected Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_AddRejectedRole '" + server_id + "', '" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "dashboard":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Dashboard Access Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_AddDashboardRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "commandrole":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Settings Command Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_AddPluginCommandRole '" + server_id + "','" + plugin + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                    }
                    break;
                case "linkup":
                    if (type == "broadcastrole")
                    {
                        command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Linkup Broadcast Role','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                        command = new OleDbCommand("P_Upd_AddLinkupRole '" + server_id + "','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                    }
                    else
                    {
                        command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Linkup Command Role','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                        command = new OleDbCommand("P_UPD_AddPluginCommandRole '" + server_id + "','Linkup','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                    }
                    break;
                case "moderator":
                    command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Moderator Command Role','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    command = new OleDbCommand("P_UPD_AddPluginCommandRole '" + server_id + "','" + plugin + "','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    break;
                case "roles":
                    command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Add Roles Command Role','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    command = new OleDbCommand("P_UPD_AddPluginCommandRole '" + server_id + "','Roles','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    break;
                case "rolegroup":
                    switch (type)
                    {
                        case "grouped":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Grouped A Role to " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_AddRolegroupGroupedRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "rejected":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Rejected A Role on " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_AddRolegroupRejectedRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "required":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Required A Role on " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_AddRolegroupRequiredRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                    }
                    break;
            }
            connection.Close();
            return true;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CommitRoleRemoved(string data_string)
        {
            string[] data_values = data_string.Split('_');
            string plugin = data_values[0];
            string type = data_values[1];
            string user_id = data_values[2];
            string server_id = data_values[3];
            string role_id = data_values[4];
            string rolegroup_role_id = "";
            if (data_values.Length == 6) { rolegroup_role_id = data_values[5]; }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            switch (plugin)
            {
                case "settings":
                    switch (type)
                    {
                        case "immunity":
                            //Add the immunity role to the database and return
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Immunity Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_RemoveImmunityRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "command":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Wizard Command Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_RemoveCommandRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "rejected":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Wizard Rejected Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_RemoveRejectedRole '" + server_id + "', '" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "dashboard":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Dashboard Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_Upd_RemoveDashboardRole '" + server_id + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "commandrole":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Settings Command Role','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_RemovePluginCommandRole '" + server_id + "','" + plugin + "','" + role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                    }
                    break;
                case "linkup":
                    if (type == "broadcastrole")
                    {
                        command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Linkup Broadcast Role','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                        command = new OleDbCommand("P_Upd_RemoveLinkupRole '" + server_id + "','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                    }
                    else
                    {
                        command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Linkup Command Role','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                        command = new OleDbCommand("P_UPD_RemovePluginCommandRole '" + server_id + "','Server Linkup','" + role_id + "'", connection);
                        reader = command.ExecuteReader();
                    }
                    break;
                case "moderator":
                    command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Moderator Command Role','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    command = new OleDbCommand("P_UPD_RemovePluginCommandRole '" + server_id + "','" + plugin + "','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    break;
                case "roles":
                    command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Remove Roles Command Role','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    command = new OleDbCommand("P_UPD_RemovePluginCommandRole '" + server_id + "','Role Commands','" + role_id + "'", connection);
                    reader = command.ExecuteReader();
                    break;
                case "rolegroup":
                    switch (type)
                    {
                        case "grouped":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Decoupled A Role from " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_RemoveRolegroupGroupedRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "rejected":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Unrestricted A Role on " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_RemoveRolegroupRejectedRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                        case "required":
                            command = new OleDbCommand("P_Ins_LogDashboardAction '" + server_id + "','" + user_id + "','Requirement Removed from " + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            command = new OleDbCommand("P_UPD_RemoveRolegroupRequiredRole '" + server_id + "','" + role_id + "','" + rolegroup_role_id + "'", connection);
                            reader = command.ExecuteReader();
                            break;
                    }
                    break;
            }
            connection.Close();
            return true;
        }
        [AjaxOnly]
        [HttpGet]
        public string GetAllowedGroupingRoles(string base_role_id, string group_type)
        {
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Get_ServerRoleGroup '" + current_server.ID + "','" + base_role_id + "'", connection);
            reader = command.ExecuteReader();
            reader.Read();
            string group_role_ids;
            string rejected_role_ids;
            string required_role_ids;
            DashboardRoleGroup role_group = new DashboardRoleGroup();
            role_group.BaseRoleID = base_role_id;
            group_role_ids = reader.GetString(0);
            rejected_role_ids = reader.GetString(1);
            required_role_ids = reader.GetString(2);
            role_group.RequireAll = reader.GetBoolean(3);
            role_group.GroupedRoleIDs = group_role_ids.Split(',').ToList();
            role_group.RejectedRoleIDs = rejected_role_ids.Split(',').ToList();
            role_group.RequiredRoleIDs = required_role_ids.Split(',').ToList();
            connection.Close();
            string allowed_group_role_ids = "";
            string allowed_rejected_role_ids = "";
            string allowed_required_role_ids = "";
            foreach (DashboardRole current_role in current_server.Roles.Values)
            {
                if (current_role.Name != "@everyone")
                {
                    //Any role can be a base role
                    //Role groups will have a limit of 2 unique assignments per group, and 3 groups per server for free
                    //Role groups will have up to 6 unique assignments and as many assignments are are supported for premium
                    //Any role can be either grouped, rejected, or required only once on any role grouping. cannot include itself. does not include @everyone
                    bool group_role_allowed = false;
                    bool rejected_role_allowed = false;
                    bool required_role_allowed = false;
                    if (!role_group.GroupedRoleIDs.Contains(current_role.ID))
                    {
                        if (!role_group.RejectedRoleIDs.Contains(current_role.ID) && !role_group.RequiredRoleIDs.Contains(current_role.ID))
                        {
                            group_role_allowed = true;
                            if (current_role.ID == role_group.BaseRoleID) { group_role_allowed = false; }
                            //If the current role is already a base role
                            if (current_server.RoleGroups.ContainsKey(current_role.ID))
                            {
                                //A Role can NOT group a role that also already requires it
                                if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                //A Role can NOT group a role that also already rejects it
                                if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                foreach (string rejected_role_id in role_group.RejectedRoleIDs)
                                {
                                    if (role_group.RequireAll)
                                    {
                                        //A Role can NOT group a role that requires a role that it rejects, if require all is set for that role (will cause a fight in add/remove)
                                        if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(rejected_role_id))
                                        {
                                            group_role_allowed = false;
                                            break;
                                        }
                                    }
                                    //A Role can NOT group a role that groups a role that is already rejected (will cause a fight in add/remove)
                                    if (current_server.RoleGroups[current_role.ID].GroupedRoleIDs.Contains(rejected_role_id))
                                    {
                                        group_role_allowed = false;
                                        break;
                                    }
                                }
                                foreach (string grouped_role_id in role_group.GroupedRoleIDs)
                                {
                                    if (role_group.RequireAll)
                                    {
                                        //A Role can NOT group a role that rejects a role that is already grouped (will cause a fight in add/remove)
                                        if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(grouped_role_id))
                                        {
                                            group_role_allowed = false;
                                            break;
                                        }
                                    }
                                }
                                //A Role can NOT group a role that requires a role that is not also required
                                foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                {
                                    if (!role_group.RequiredRoleIDs.Contains(required_role_id))
                                    {
                                        group_role_allowed = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!role_group.RejectedRoleIDs.Contains(current_role.ID))
                    {
                        if (!role_group.GroupedRoleIDs.Contains(current_role.ID) && !role_group.RequiredRoleIDs.Contains(current_role.ID))
                        {
                            rejected_role_allowed = true;
                            if (current_role.ID == role_group.BaseRoleID) { rejected_role_allowed = false; }
                            if (current_server.RoleGroups.ContainsKey(current_role.ID))
                            {
                                //A Role can NOT reject a role that also already groups it
                                if (current_server.RoleGroups[current_role.ID].GroupedRoleIDs.Contains(role_group.BaseRoleID)) { rejected_role_allowed = false; }
                                //A Role can NOT reject a role that also already requires it
                                if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { group_role_allowed = false; }
                                foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                {
                                    if (current_server.RoleGroups.ContainsKey(required_role_id))
                                    {
                                        //A Role can NOT reject a role that is grouped to a required role (will cause a fight in add/remove)
                                        if (current_server.RoleGroups[required_role_id].GroupedRoleIDs.Contains(current_role.ID))
                                        {
                                            rejected_role_allowed = false;
                                            break;
                                        }
                                        //A Role can NOT reject a role that is required by a required role (will cause a fight in add/remove)
                                        if (current_server.RoleGroups[required_role_id].RequiredRoleIDs.Contains(current_role.ID))
                                        {
                                            rejected_role_allowed = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            foreach (string grouped_role_id in role_group.GroupedRoleIDs)
                            {
                                if (current_server.RoleGroups.ContainsKey(grouped_role_id))
                                {
                                    //A Role can NOT reject a role that is grouped to grouped role (will cause a fight in add/remove)
                                    if (current_server.RoleGroups[grouped_role_id].GroupedRoleIDs.Contains(current_role.ID))
                                    {
                                        rejected_role_allowed = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!role_group.RequiredRoleIDs.Contains(current_role.ID))
                    {
                        if (!role_group.GroupedRoleIDs.Contains(current_role.ID) && !role_group.RejectedRoleIDs.Contains(current_role.ID))
                        {
                            required_role_allowed = true;
                            if (current_role.ID == role_group.BaseRoleID) { required_role_allowed = false; }
                            if (current_server.RoleGroups.ContainsKey(current_role.ID))
                            {
                                //A Role can NOT require a role that also already rejects it
                                if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(role_group.BaseRoleID)) { required_role_allowed = false; }
                                //A Role can NOT require a role that also already requires it
                                if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(role_group.BaseRoleID)) { required_role_allowed = false; }
                                foreach (string grouped_role_id in current_server.RoleGroups[current_role.ID].GroupedRoleIDs)
                                {
                                    if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(grouped_role_id))
                                    {
                                        //A Role can NOT require a role that rejects a grouped role
                                        required_role_allowed = false;
                                        break;
                                    }
                                    if (current_server.RoleGroups.ContainsKey(grouped_role_id))
                                    {
                                        if (current_server.RoleGroups[grouped_role_id].RejectedRoleIDs.Contains(current_role.ID))
                                        {
                                            //A Role can NOT require a role that is rejected by a grouped role
                                            required_role_allowed = false;
                                            break;
                                        }
                                    }
                                }
                                foreach (string rejected_role_id in role_group.RejectedRoleIDs)
                                {
                                    if (current_server.RoleGroups[current_role.ID].RequiredRoleIDs.Contains(rejected_role_id))
                                    {
                                        //A Role can NOT require a role that requires a rejected role
                                        required_role_allowed = false;
                                        break;
                                    }
                                }
                                foreach (string required_role_id in current_server.RoleGroups[current_role.ID].RequiredRoleIDs)
                                {
                                    if (role_group.RequireAll)
                                    {
                                        //A Role can NOT require a role that rejects another required role, if requre all is set
                                        if (current_server.RoleGroups[current_role.ID].RejectedRoleIDs.Contains(required_role_id))
                                        {
                                            required_role_allowed = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (group_role_allowed) { allowed_group_role_ids += current_role.ID + "_"; }
                    if (rejected_role_allowed) { allowed_rejected_role_ids += current_role.ID + "_"; }
                    if (required_role_allowed) { allowed_required_role_ids += current_role.ID + "_"; }
                }
            }
            if (allowed_group_role_ids != "") { allowed_group_role_ids = allowed_group_role_ids.Substring(0, allowed_group_role_ids.Length - 1); }
            if (allowed_rejected_role_ids != "") { allowed_rejected_role_ids = allowed_rejected_role_ids.Substring(0, allowed_rejected_role_ids.Length - 1); }
            if (allowed_required_role_ids != "") { allowed_required_role_ids = allowed_required_role_ids.Substring(0, allowed_required_role_ids.Length - 1); }
            switch (group_type)
            {
                case "GROUPED":
                    return allowed_group_role_ids;
                case "REJECTED":
                    return allowed_rejected_role_ids;
                case "REQUIRED":
                    return allowed_required_role_ids;
                default:
                    return "";
            }
        }
        [AjaxOnly]
        [HttpGet]
        public bool CreateHelpdesk(string user_id) 
        {
            string EveryoneID = "";
            string new_channel_id = "";
            foreach(DashboardRole role in current_server.Roles.Values)
            {
                if(role.Name == "@everyone")
                {
                    EveryoneID = role.ID;
                    break;
                }
            }
            int perm_flag = (int)DiscordPermission.VIEW_CHANNEL;
            PermissionOverwrite default_everyone_overwrite = new PermissionOverwrite(EveryoneID, "role", -1, perm_flag);
            PermissionOverwrite default_wizard_overwrite = new PermissionOverwrite("246278818074066946", "member", perm_flag, -1);
            List<PermissionOverwrite> default_overwrites = new List<PermissionOverwrite>();
            default_overwrites.Add(default_wizard_overwrite);
            default_overwrites.Add(default_everyone_overwrite);
            DiscordChannel new_channel = null;
            if (current_server.WizardCategoryID == "")
            {
                DiscordChannel wizard_category;
                RestClient CategoryClient;
                RestRequest CategoryRequest;
                IRestResponse CategoryResponse;
                ChannelUpdateObject wizard_channels = new ChannelUpdateObject();
                wizard_channels.name = "Wizard Channels";
                wizard_channels.type = ChannelType.GUILD_CATEGORY;
                wizard_channels.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject CategoryRequestBody;
                MajickSharp.Json.JsonObject CategoryResponseContent;
                CategoryClient = new RestClient("https://discord.com/api");
                CategoryRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                CategoryRequest.RequestFormat = DataFormat.Json;
                CategoryRequest.AddHeader("Content-Type", "application/json");
                CategoryRequest.AddHeader("Authorization", "Bot " + token);
                CategoryRequestBody = wizard_channels.ToJson();
                CategoryRequest.AddJsonBody(CategoryRequestBody.ToRawText(false));
                CategoryResponse = CategoryClient.Execute(CategoryRequest);
                CategoryResponseContent = new MajickSharp.Json.JsonObject(CategoryResponse.Content);
                wizard_category = new DiscordChannel(CategoryResponseContent);
                current_server.WizardCategoryID = wizard_category.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerWizardCategory '" + current_server.ID + "','" + wizard_category.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.HelpdeskChannelID == "")
            {
                RestClient HelpdeskClient;
                RestRequest HelpdeskRequest;
                IRestResponse HelpdeskResponse;
                ChannelUpdateObject wizard_support = new ChannelUpdateObject();
                wizard_support.name = "wizard-support";
                wizard_support.type = ChannelType.GUILD_TEXT;
                wizard_support.topic = "Only messages with 150 characters or more will be broadcast";
                wizard_support.parent_id = current_server.WizardCategoryID;
                wizard_support.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject HelpdeskRequestBody;
                MajickSharp.Json.JsonObject HelpdeskResponseContent;
                HelpdeskClient = new RestClient("https://discord.com/api");
                HelpdeskRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                HelpdeskRequest.RequestFormat = DataFormat.Json;
                HelpdeskRequest.AddHeader("Content-Type", "application/json");
                HelpdeskRequest.AddHeader("Authorization", "Bot " + token);
                HelpdeskRequestBody = wizard_support.ToJson();
                HelpdeskRequest.AddJsonBody(HelpdeskRequestBody.ToRawText(false));
                HelpdeskResponse = HelpdeskClient.Execute(HelpdeskRequest);
                HelpdeskResponseContent = new MajickSharp.Json.JsonObject(HelpdeskResponse.Content);
                new_channel = new DiscordChannel(HelpdeskResponseContent);
                new_channel_id = new_channel.id;
                current_server.WizardCategoryID = new_channel.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerHelpdeskChannel '" + current_server.ID + "','" + new_channel.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.ServerLinkupData.PremiumLinkupWebhookID == "")
            {
                DiscordWebhook webhook;
                RestClient WebhookClient;
                RestRequest WebhookRequest;
                IRestResponse WebhookResponse;
                MajickSharp.Json.JsonObject GuildRequestBody = new MajickSharp.Json.JsonObject();
                MajickSharp.Json.JsonObject GuildResponseContent = new MajickSharp.Json.JsonObject();
                WebhookClient = new RestClient("https://discord.com/api");
                WebhookRequest = new RestRequest("/channels/" + new_channel.id + "/webhooks", Method.POST);
                WebhookRequest.RequestFormat = DataFormat.Json;
                WebhookRequest.AddHeader("Content-Type", "application/json");
                WebhookRequest.AddHeader("Authorization", "Bot " + token);
                GuildRequestBody.AddAttribute("name", "Wizard Support Liason");
                WebhookRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
                WebhookResponse = WebhookClient.Execute(WebhookRequest);
                GuildResponseContent = new MajickSharp.Json.JsonObject(WebhookResponse.Content);
                webhook = new DiscordWebhook(GuildResponseContent);
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerHelpdeskWebhook '" + current_server.ID + "','" + webhook.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Helpdesk Channel Created','" + new_channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();
            return true; 
        }
        [AjaxOnly]
        [HttpGet]
        public bool DeleteHelpdesk(string user_id)
        {
            string channel_id = current_server.ServerLinkupData.LinkupChannelID;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            IRestResponse rsGuildResponse;
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id, Method.DELETE);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Helpdesk Channel Deleted','" + channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();

            return rsGuildResponse.IsSuccessful;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CreateBasicLinkup(string user_id) 
        {
            string EveryoneID = "";
            string new_channel_id = "";
            foreach (DashboardRole role in current_server.Roles.Values)
            {
                if (role.Name == "@everyone")
                {
                    EveryoneID = role.ID;
                    break;
                }
            }
            int perm_flag = (int)DiscordPermission.VIEW_CHANNEL;
            PermissionOverwrite default_everyone_overwrite = new PermissionOverwrite(EveryoneID, "role", -1, perm_flag);
            PermissionOverwrite default_wizard_overwrite = new PermissionOverwrite(client_id, "member", perm_flag, -1);
            List<PermissionOverwrite> default_overwrites = new List<PermissionOverwrite>();
            default_overwrites.Add(default_wizard_overwrite);
            default_overwrites.Add(default_everyone_overwrite);
            DiscordChannel new_channel = null;
            if (current_server.WizardCategoryID == "")
            {
                DiscordChannel wizard_category;
                RestClient CategoryClient;
                RestRequest CategoryRequest;
                IRestResponse CategoryResponse;
                ChannelUpdateObject wizard_channels = new ChannelUpdateObject();
                wizard_channels.name = "Wizard Channels";
                wizard_channels.type = ChannelType.GUILD_CATEGORY;
                wizard_channels.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject CategoryRequestBody;
                MajickSharp.Json.JsonObject CategoryResponseContent;
                CategoryClient = new RestClient("https://discord.com/api");
                CategoryRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                CategoryRequest.RequestFormat = DataFormat.Json;
                CategoryRequest.AddHeader("Content-Type", "application/json");
                CategoryRequest.AddHeader("Authorization", "Bot " + token);
                CategoryRequestBody = wizard_channels.ToJson();
                CategoryRequest.AddJsonBody(CategoryRequestBody.ToRawText(false));
                CategoryResponse = CategoryClient.Execute(CategoryRequest);
                CategoryResponseContent = new MajickSharp.Json.JsonObject(CategoryResponse.Content);
                wizard_category = new DiscordChannel(CategoryResponseContent);
                current_server.WizardCategoryID = wizard_category.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerWizardCategory '" + current_server.ID + "','" + wizard_category.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.ServerLinkupData.PremiumLinkupChannelID == "")
            {
                RestClient BasicLinkupClient;
                RestRequest BasicLinkupRequest;
                IRestResponse BasicLinkupResponse;
                ChannelUpdateObject wizard_support = new ChannelUpdateObject();
                wizard_support.name = "wizard-linkup";
                wizard_support.type = ChannelType.GUILD_TEXT;
                wizard_support.topic = "Send messages to your friends in this Linkup Channel!!";
                wizard_support.parent_id = current_server.WizardCategoryID;
                wizard_support.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject BasicLinkupRequestBody;
                MajickSharp.Json.JsonObject BasicLinkupResponseContent;
                BasicLinkupClient = new RestClient("https://discord.com/api");
                BasicLinkupRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                BasicLinkupRequest.RequestFormat = DataFormat.Json;
                BasicLinkupRequest.AddHeader("Content-Type", "application/json");
                BasicLinkupRequest.AddHeader("Authorization", "Bot " + token);
                BasicLinkupRequestBody = wizard_support.ToJson();
                BasicLinkupRequest.AddJsonBody(BasicLinkupRequestBody.ToRawText(false));
                BasicLinkupResponse = BasicLinkupClient.Execute(BasicLinkupRequest);
                BasicLinkupResponseContent = new MajickSharp.Json.JsonObject(BasicLinkupResponse.Content);
                new_channel = new DiscordChannel(BasicLinkupResponseContent);
                new_channel_id = new_channel.id;
                current_server.WizardCategoryID = new_channel.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerLinkupChannel '" + current_server.ID + "','" + new_channel.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.ServerLinkupData.PremiumLinkupWebhookID == "")
            {
                DiscordWebhook webhook;
                RestClient WebhookClient;
                RestRequest WebhookRequest;
                IRestResponse WebhookResponse;
                MajickSharp.Json.JsonObject GuildRequestBody = new MajickSharp.Json.JsonObject();
                MajickSharp.Json.JsonObject GuildResponseContent = new MajickSharp.Json.JsonObject();
                WebhookClient = new RestClient("https://discord.com/api");
                WebhookRequest = new RestRequest("/channels/" + new_channel.id + "/webhooks", Method.POST);
                WebhookRequest.RequestFormat = DataFormat.Json;
                WebhookRequest.AddHeader("Content-Type", "application/json");
                WebhookRequest.AddHeader("Authorization", "Bot " + token);
                GuildRequestBody.AddAttribute("name", "Wizard Linkhook");
                WebhookRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
                WebhookResponse = WebhookClient.Execute(WebhookRequest);
                GuildResponseContent = new MajickSharp.Json.JsonObject(WebhookResponse.Content);
                webhook = new DiscordWebhook(GuildResponseContent);
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerLinkupWebhook '" + current_server.ID + "','" + webhook.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Basic Linkup Channel Created','" + new_channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();
            return true;
        }
        [AjaxOnly]
        [HttpGet]
        public bool DeleteBasicLinkup(string user_id)
        {
            string channel_id = current_server.ServerLinkupData.LinkupChannelID;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            IRestResponse rsGuildResponse;
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id, Method.DELETE);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Basic Linkup Channel Deleted','" + channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();

            return rsGuildResponse.IsSuccessful;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CreatePremiumLinkup(string user_id) 
        {
            string EveryoneID = "";
            string new_channel_id = "";
            foreach (DashboardRole role in current_server.Roles.Values)
            {
                if (role.Name == "@everyone")
                {
                    EveryoneID = role.ID;
                    break;
                }
            }
            int perm_flag = (int)DiscordPermission.VIEW_CHANNEL;
            PermissionOverwrite default_everyone_overwrite = new PermissionOverwrite(EveryoneID, "role", -1, perm_flag);
            PermissionOverwrite default_wizard_overwrite = new PermissionOverwrite(client_id, "member", perm_flag, -1);
            List<PermissionOverwrite> default_overwrites = new List<PermissionOverwrite>();
            default_overwrites.Add(default_wizard_overwrite);
            default_overwrites.Add(default_everyone_overwrite);
            DiscordChannel new_channel = null;
            if (current_server.WizardCategoryID == "")
            {
                DiscordChannel wizard_category;
                RestClient CategoryClient;
                RestRequest CategoryRequest;
                IRestResponse CategoryResponse;
                ChannelUpdateObject wizard_channels = new ChannelUpdateObject();
                wizard_channels.name = "Wizard Channels";
                wizard_channels.type = ChannelType.GUILD_CATEGORY;
                wizard_channels.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject CategoryRequestBody;
                MajickSharp.Json.JsonObject CategoryResponseContent;
                CategoryClient = new RestClient("https://discord.com/api");
                CategoryRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                CategoryRequest.RequestFormat = DataFormat.Json;
                CategoryRequest.AddHeader("Content-Type", "application/json");
                CategoryRequest.AddHeader("Authorization", "Bot " + token);
                CategoryRequestBody = wizard_channels.ToJson();
                CategoryRequest.AddJsonBody(CategoryRequestBody.ToRawText(false));
                CategoryResponse = CategoryClient.Execute(CategoryRequest);
                CategoryResponseContent = new MajickSharp.Json.JsonObject(CategoryResponse.Content);
                wizard_category = new DiscordChannel(CategoryResponseContent);
                current_server.WizardCategoryID = wizard_category.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerWizardCategory '" + current_server.ID + "','" + wizard_category.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.ServerLinkupData.PremiumLinkupChannelID == "")
            {
                RestClient PremiumLinkupClient;
                RestRequest PremiumLinkupRequest;
                IRestResponse PremiumLinkupResponse;
                ChannelUpdateObject wizard_support = new ChannelUpdateObject();
                wizard_support.name = "premium-linkup";
                wizard_support.type = ChannelType.GUILD_TEXT;
                wizard_support.topic = "Enjoy your secondary Linkup Group. Thank you for getting Premium! :)";
                wizard_support.parent_id = current_server.WizardCategoryID;
                wizard_support.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject PremiumLinkupRequestBody;
                MajickSharp.Json.JsonObject PremiumLinkupResponseContent;
                PremiumLinkupClient = new RestClient("https://discord.com/api");
                PremiumLinkupRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                PremiumLinkupRequest.RequestFormat = DataFormat.Json;
                PremiumLinkupRequest.AddHeader("Content-Type", "application/json");
                PremiumLinkupRequest.AddHeader("Authorization", "Bot " + token);
                PremiumLinkupRequestBody = wizard_support.ToJson();
                PremiumLinkupRequest.AddJsonBody(PremiumLinkupRequestBody.ToRawText(false));
                PremiumLinkupResponse = PremiumLinkupClient.Execute(PremiumLinkupRequest);
                PremiumLinkupResponseContent = new MajickSharp.Json.JsonObject(PremiumLinkupResponse.Content);
                new_channel = new DiscordChannel(PremiumLinkupResponseContent);
                new_channel_id = new_channel.id;
                current_server.WizardCategoryID = new_channel.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_PremiumLinkupChannel '" + current_server.ID + "','" + new_channel.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if(current_server.ServerLinkupData.PremiumLinkupWebhookID == "")
            {
                DiscordWebhook webhook;
                RestClient WebhookClient;
                RestRequest WebhookRequest;
                IRestResponse WebhookResponse;
                MajickSharp.Json.JsonObject GuildRequestBody = new MajickSharp.Json.JsonObject();
                MajickSharp.Json.JsonObject GuildResponseContent = new MajickSharp.Json.JsonObject();
                WebhookClient = new RestClient("https://discord.com/api");
                WebhookRequest = new RestRequest("/channels/" + new_channel.id + "/webhooks", Method.POST);
                WebhookRequest.RequestFormat = DataFormat.Json;
                WebhookRequest.AddHeader("Content-Type", "application/json");
                WebhookRequest.AddHeader("Authorization", "Bot " + token);
                GuildRequestBody.AddAttribute("name", "Premium Linkhook");
                WebhookRequest.AddJsonBody(GuildRequestBody.ToRawText(false));
                WebhookResponse = WebhookClient.Execute(WebhookRequest);
                GuildResponseContent = new MajickSharp.Json.JsonObject(WebhookResponse.Content);
                webhook = new DiscordWebhook(GuildResponseContent);
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_PremiumLinkupWebhook '" + current_server.ID + "','" + webhook.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Premium Linkup Channel Created','" + new_channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();
            return true;
        }
        [AjaxOnly]
        [HttpGet]
        public bool DeletePremiumLinkup(string user_id)
        {
            string channel_id = current_server.ServerLinkupData.PremiumLinkupChannelID;
            RestClient rcGuildClient;
            RestRequest rrGuildRequest;
            IRestResponse rsGuildResponse;
            Dictionary<string, DiscordChannel> channels = new Dictionary<string, DiscordChannel>();
            rcGuildClient = new RestClient("https://discord.com/api");
            rrGuildRequest = new RestRequest("/channels/" + channel_id, Method.DELETE);
            rrGuildRequest.RequestFormat = DataFormat.Json;
            rrGuildRequest.AddHeader("Content-Type", "application/json");
            rrGuildRequest.AddHeader("Authorization", "Bot " + token);
            rsGuildResponse = rcGuildClient.Execute(rrGuildRequest);
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Premium Linkup Channel Deleted','" + channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();

            return rsGuildResponse.IsSuccessful;
        }
        [AjaxOnly]
        [HttpGet]
        public bool CreateLockoutChannel(string user_id) 
        {
            string EveryoneID = "";
            string new_channel_id = "";
            foreach (DashboardRole role in current_server.Roles.Values)
            {
                if (role.Name == "@everyone")
                {
                    EveryoneID = role.ID;
                    break;
                }
            }
            int perm_flag = (int)DiscordPermission.VIEW_CHANNEL;
            PermissionOverwrite default_everyone_overwrite = new PermissionOverwrite(EveryoneID, "role", -1, perm_flag);
            PermissionOverwrite default_wizard_overwrite = new PermissionOverwrite(client_id, "member", perm_flag, -1);
            List<PermissionOverwrite> default_overwrites = new List<PermissionOverwrite>();
            default_overwrites.Add(default_wizard_overwrite);
            default_overwrites.Add(default_everyone_overwrite);
            DiscordChannel new_channel = null;
            if (current_server.WizardCategoryID == "")
            {
                DiscordChannel wizard_category;
                RestClient CategoryClient;
                RestRequest CategoryRequest;
                IRestResponse CategoryResponse;
                ChannelUpdateObject wizard_channels = new ChannelUpdateObject();
                wizard_channels.name = "Wizard Channels";
                wizard_channels.type = ChannelType.GUILD_CATEGORY;
                wizard_channels.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject CategoryRequestBody;
                MajickSharp.Json.JsonObject CategoryResponseContent;
                CategoryClient = new RestClient("https://discord.com/api");
                CategoryRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                CategoryRequest.RequestFormat = DataFormat.Json;
                CategoryRequest.AddHeader("Content-Type", "application/json");
                CategoryRequest.AddHeader("Authorization", "Bot " + token);
                CategoryRequestBody = wizard_channels.ToJson();
                CategoryRequest.AddJsonBody(CategoryRequestBody.ToRawText(false));
                CategoryResponse = CategoryClient.Execute(CategoryRequest);
                CategoryResponseContent = new MajickSharp.Json.JsonObject(CategoryResponse.Content);
                wizard_category = new DiscordChannel(CategoryResponseContent);
                current_server.WizardCategoryID = wizard_category.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerWizardCategory '" + current_server.ID + "','" + wizard_category.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (current_server.LockoutChannelID == "")
            {
                RestClient LockoutClient;
                RestRequest LockoutRequest;
                IRestResponse LockoutResponse;
                ChannelUpdateObject wizard_support = new ChannelUpdateObject();
                wizard_support.name = "wizard-lockout";
                wizard_support.type = ChannelType.GUILD_TEXT;
                wizard_support.parent_id = current_server.WizardCategoryID;
                wizard_support.permission_overwrites = default_overwrites;
                MajickSharp.Json.JsonObject LockoutRequestBody;
                MajickSharp.Json.JsonObject LockoutResponseContent;
                LockoutClient = new RestClient("https://discord.com/api");
                LockoutRequest = new RestRequest("/guilds/" + current_server.ID + "/channels", Method.POST);
                LockoutRequest.RequestFormat = DataFormat.Json;
                LockoutRequest.AddHeader("Content-Type", "application/json");
                LockoutRequest.AddHeader("Authorization", "Bot " + token);
                LockoutRequestBody = wizard_support.ToJson();
                LockoutRequest.AddJsonBody(LockoutRequestBody.ToRawText(false));
                LockoutResponse = LockoutClient.Execute(LockoutRequest);
                LockoutResponseContent = new MajickSharp.Json.JsonObject(LockoutResponse.Content);
                new_channel = new DiscordChannel(LockoutResponseContent);
                new_channel_id = new_channel.id;
                current_server.WizardCategoryID = new_channel.id;
                if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
                connection.Open();
                command = new OleDbCommand("P_Upd_ServerLockoutChannel '" + current_server.ID + "','" + new_channel.id + "'", connection);
                reader = command.ExecuteReader();
                connection.Close();
            }
            if (connection.State == System.Data.ConnectionState.Open) { connection.Close(); }
            connection.Open();
            command = new OleDbCommand("P_Ins_LogDashboardAction '" + current_server.ID + "','" + user_id + "','Lockout Channel Created','" + new_channel_id + "'", connection);
            reader = command.ExecuteReader();
            connection.Close();
            return true; 
        }
        [AjaxOnly]
        [HttpGet]
        public bool CreateVerifyChannel(string user_id) { return true; }
        [AjaxOnly]
        [HttpGet]
        public bool DeleteVerifyChannel(string user_id) { return true; }
        [HttpGet]
        public ActionResult Premium()
        {
            return View(current_server);
        }
    }
}
