using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RestSharp;
using System.Data.OleDb;
using MajickSharp;
namespace WizardDashboard.Controllers
{
    public class InterfaceController : Controller
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

        [HttpGet]
        public ActionResult Login(string guild_id = "")
        {
            string login_url;
            if (guild_id == "") { login_url = "https://discord.com/api/oauth2/authorize?client_id=" + client_id + "&permissions=0&redirect_uri=http%3A%2F%2Flocalhost%3A58318%2FInterface%2FDiscordCallback&response_type=code&scope=identify%20guilds%20guilds.join"; }
            else { login_url = "https://discord.com/api/oauth2/authorize?client_id=" + client_id + "&permissions=0&redirect_uri=http%3A%2F%2Flocalhost%3A58318%2FInterface%2FDiscordCallback&response_type=code&scope=bot&guild_id=" + guild_id + "&disable_guild_select=true"; }
            //if (guild_id == "") { login_url = "https://discord.com/api/oauth2/authorize?client_id=" + client_id + "&permissions=0&redirect_uri=https%3A%2F%2Flocalhost%3A44300%2FInterface%2FDiscordCallback&response_type=code&scope=identify%20guilds"; }
            //else { login_url = "https://discord.com/api/oauth2/authorize?client_id=" + client_id + "&permissions=0&redirect_uri=https%3A%2F%2Flocalhost%3A44300%2FInterface%2FDiscordCallback&response_type=code&scope=bot&guild_id=" + guild_id + "&disable_guild_select=true"; }
            return Redirect(login_url);
        }
        // POST: Interface/DiscordCallback
        [HttpGet]
        public ActionResult DiscordCallback(string code, string state = "", string guild_id = "", int permissions = 0)
        {
            string token = "";
            int expires_in = -1;
            string refresh_token = "";
            string data_check = "P_Get_DashboardUser ";
            string data_insert = "P_Ins_NewDashboardUser ";
            string data_update = "P_Upd_DashboardUser ";
            MajickSharp.Json.JsonObject UserInfoResponseContent = new MajickSharp.Json.JsonObject();
            //Make the request here with the code to get back all the token information needed for seeing that user's guilds
            RestClient UserAuthClient = new RestClient("https://discord.com/api");
            RestRequest UserAuthRequest = new RestRequest("/oauth2/token", Method.POST);
            UserAuthRequest.AddParameter("client_id", client_id);
            UserAuthRequest.AddParameter("client_secret", client_secret);
            UserAuthRequest.AddParameter("grant_type", "authorization_code");
            UserAuthRequest.AddParameter("code", code);
            UserAuthRequest.AddParameter("redirect_uri", "http://localhost:58318/Interface/DiscordCallback");
            //UserAuthRequest.AddParameter("redirect_uri", "https://localhost:44300/Interface/DiscordCallback");
            if (guild_id == "") { UserAuthRequest.AddParameter("scope", "identify guilds"); }
            else { UserAuthRequest.AddParameter("scope", "bot"); }
            IRestResponse UserAuthResponse = UserAuthClient.Execute(UserAuthRequest);
            MajickSharp.Json.JsonObject AuthData = new MajickSharp.Json.JsonObject(UserAuthResponse.Content);
            if (AuthData.Attributes.ContainsKey("access_token")) { token = AuthData.Attributes["access_token"].text_value; }
            if (AuthData.Attributes.ContainsKey("access_token")) { int.TryParse(AuthData.Attributes["expires_in"].text_value, out expires_in); }
            if (AuthData.Attributes.ContainsKey("refresh_token")) { refresh_token = AuthData.Attributes["refresh_token"].text_value; }
            //Pull the User and Guild information here to establish a baseline for dashboard access
            RestRequest UserInfoRequest = new RestRequest("/users/@me", Method.GET);
            UserInfoRequest.RequestFormat = DataFormat.Json;
            UserInfoRequest.AddHeader("Content-Type", "application/json");
            UserInfoRequest.AddHeader("Authorization", "Bearer " + token);
            IRestResponse UserInfoResponse = UserAuthClient.Execute(UserInfoRequest);
            UserInfoResponseContent = new MajickSharp.Json.JsonObject(UserInfoResponse.Content);
            //If they cancel will return 403 Unauthorized Error
            if(UserInfoResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) { return RedirectToAction("Index", "Home"); }
            //If this doesn't have id something failed
            if (UserInfoResponseContent.Attributes.ContainsKey("id"))
            {
                data_check += "'" + UserInfoResponseContent.Attributes["id"].text_value + "'";
                connection.Open();
                command = new OleDbCommand(data_check, connection);
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    //Evaluate the properties here for use on the web page and for data inserts
                    //Insert the data into the database for use on the dash page
                    data_insert += "'" + UserInfoResponseContent.Attributes["id"].text_value + "','" + token + "','" + UserInfoResponseContent.Attributes["username"].text_value + "','" + UserInfoResponseContent.Attributes["avatar"].text_value + "','" + refresh_token + "'";
                    command = new OleDbCommand(data_insert, connection);
                    reader = command.ExecuteReader();
                }
                else
                {
                    //this dash user already exists check that the tokens match and update if necessary
                    data_update += "'" + UserInfoResponseContent.Attributes["id"].text_value + "','" + token + "','" + UserInfoResponseContent.Attributes["username"].text_value + "','" + UserInfoResponseContent.Attributes["avatar"].text_value + "','" + refresh_token + "'";
                    command = new OleDbCommand(data_update, connection);
                    reader = command.ExecuteReader();
                }
                connection.Close();
            }
            if (guild_id == "")
            {
                return RedirectToAction("Index", "Dashboard", new { user_id = UserInfoResponseContent.Attributes["id"].text_value });
            }
            else { return RedirectToAction("Servers", "Dashboard", new { server_id = guild_id }); }
        }
        public ActionResult RefreshToken(string old_refresh_token)
        {
            string token = "";
            int expires_in = -1;
            string refresh_token = "";
            string data_check = "P_Get_DashboardUser ";
            string data_insert = "P_Ins_NewDashboardUser ";
            string data_update = "P_Upd_DashboardUser ";
            MajickSharp.Json.JsonObject UserInfoResponseContent = new MajickSharp.Json.JsonObject();
            //Make the request here with the code to get back all the token information needed for seeing that user's guilds
            RestClient UserAuthClient = new RestClient("https://discord.com/api");
            RestRequest UserAuthRequest = new RestRequest("/oauth2/token", Method.POST);
            UserAuthRequest.AddParameter("client_id", client_id);
            UserAuthRequest.AddParameter("client_secret", client_secret);
            UserAuthRequest.AddParameter("grant_type", "refresh_token");
            UserAuthRequest.AddParameter("refresh_token", old_refresh_token);
            UserAuthRequest.AddParameter("redirect_uri", "http://localhost:58318/Interface/DiscordCallback");
            //UserAuthRequest.AddParameter("redirect_uri", "https://localhost:44300/Interface/DiscordCallback");
            UserAuthRequest.AddParameter("scope", "identify guilds");
            IRestResponse UserAuthResponse = UserAuthClient.Execute(UserAuthRequest);
            MajickSharp.Json.JsonObject AuthData = new MajickSharp.Json.JsonObject(UserAuthResponse.Content);
            if (AuthData.Attributes.ContainsKey("access_token")) { token = AuthData.Attributes["access_token"].text_value; }
            if (AuthData.Attributes.ContainsKey("access_token")) { int.TryParse(AuthData.Attributes["expires_in"].text_value, out expires_in); }
            if (AuthData.Attributes.ContainsKey("refresh_token")) { refresh_token = AuthData.Attributes["refresh_token"].text_value; }
            //Pull the User and Guild information here to establish a baseline for dashboard access
            RestRequest UserInfoRequest = new RestRequest("/users/@me", Method.GET);
            UserInfoRequest.RequestFormat = DataFormat.Json;
            UserInfoRequest.AddHeader("Content-Type", "application/json");
            UserInfoRequest.AddHeader("Authorization", "Bearer " + token);
            IRestResponse UserInfoResponse = UserAuthClient.Execute(UserInfoRequest);
            UserInfoResponseContent = new MajickSharp.Json.JsonObject(UserInfoResponse.Content);
            //If they cancel will return 403 Unauthorized Error
            if (UserInfoResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) { return RedirectToAction("Index", "Home"); }
            //If this doesn't have id something failed
            if (UserInfoResponseContent.Attributes.ContainsKey("id"))
            {
                data_check += "'" + UserInfoResponseContent.Attributes["id"].text_value + "'";
                connection.Open();
                command = new OleDbCommand(data_check, connection);
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    //Evaluate the properties here for use on the web page and for data inserts
                    //Insert the data into the database for use on the dash page
                    data_insert += "'" + UserInfoResponseContent.Attributes["id"].text_value + "','" + token + "','" + UserInfoResponseContent.Attributes["username"].text_value + "','" + UserInfoResponseContent.Attributes["avatar"].text_value + "','" + refresh_token + "'";
                    command = new OleDbCommand(data_insert, connection);
                    reader = command.ExecuteReader();
                }
                else
                {
                    //this dash user already exists check that the tokens match and update if necessary
                    data_update += "'" + UserInfoResponseContent.Attributes["id"].text_value + "','" + token + "','" + UserInfoResponseContent.Attributes["username"].text_value + "','" + UserInfoResponseContent.Attributes["avatar"].text_value + "','" + refresh_token + "'";
                    command = new OleDbCommand(data_update, connection);
                    reader = command.ExecuteReader();
                }
                connection.Close();
            }
            return RedirectToAction("Index", "Dashboard", new { user_id = UserInfoResponseContent.Attributes["id"].text_value });
        }
    }
}
