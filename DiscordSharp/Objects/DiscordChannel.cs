﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DiscordSharp
{
    public class DiscordChannelBase
    {
        [JsonProperty("id")]
        public string id { get; internal set; }
        [JsonProperty("is_private")]
        public bool is_private { get; internal set; }

        internal DiscordChannelBase() { }
    }

    public class DiscordChannel : DiscordChannelBase
    {
        public string type { get; set; }
        public string name { get; set; }
        public string topic { get; set; }
        public List<DiscordPermissionOverride> PermissionOverrides { get; set; }

        public DiscordServer parent { get; internal set; }

        public DiscordMessage SendMessage(string message)
        {
            string url = Endpoints.BaseAPI + Endpoints.Channels + $"/{id}" + Endpoints.Messages;
            JObject result = JObject.Parse(WebWrapper.Post(url, DiscordClient.token, JsonConvert.SerializeObject(Utils.GenerateMessage(message))));

            DiscordMessage m = new DiscordMessage
            {
                id = result["id"].ToString(),
                attachments = result["attachments"].ToObject<string[]>(),
                author = this.parent.members.Find(x => x.ID == result["author"]["id"].ToString()),
                channel = this,
                content = result["content"].ToString(),
                RawJson = result,
                timestamp = result["timestamp"].ToObject<DateTime>()
            };
            return m;
        }

        private void DeleteMessage(DiscordMessage message)
        {
            string url = Endpoints.BaseAPI + Endpoints.Channels + $"/{id}" + Endpoints.Messages + $"/{message.id}";
            var result = JObject.Parse(WebWrapper.Delete(url, DiscordClient.token));
        }

        internal DiscordChannel() { }
    }

    public class DiscordPrivateChannel : DiscordChannelBase
    {
        public DiscordMember recipient { get; set; }
        [JsonProperty("last_message_id")]
        private string LastMessageID { get; set; }

        internal DiscordPrivateChannel() { }
    }

    //kinda like the author
    public class DiscordRecipient
    {
        public string username { get; set; }
        public string id { get; set; }
        internal DiscordRecipient() { }
    }

    public class DiscordServer
    {
        public string id { get; internal set; }
        public string name { get; internal set; }
        
#pragma warning disable 0612
        private DiscordMember _owner;
        public DiscordMember owner { get { return _owner; } internal set
            {
                _owner = value;
            }
        }
#pragma warning restore 0612

        public List<DiscordChannel> channels { get; internal set; }
        public List<DiscordMember> members { get; internal set; }
        public List<DiscordRole> roles { get; internal set; }

        internal DiscordClient parentclient { get; set; }

        internal DiscordServer()
        {
            channels = new List<DiscordChannel>();
            members = new List<DiscordMember>();
        }

        public void ChangeIcon(Bitmap image)
        {
            Bitmap resized = new Bitmap((Image)image, 200, 200);

            string base64 = Convert.ToBase64String(Utils.ImageToByteArray(resized));
            string type = "image/jpeg;base64";
            string req = $"data:{type},{base64}";
            string guildjson = JsonConvert.SerializeObject(new { icon = req, name = this.name });
            string url = Endpoints.BaseAPI + Endpoints.Guilds + "/" + this.id;
            var result = JObject.Parse(WebWrapper.Patch(url, DiscordClient.token, guildjson));
        }

        public void ChangeName(string NewGuildName)
        {
            string editGuildUrl = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}";
            var newNameJson = JsonConvert.SerializeObject(new { name = NewGuildName });
            var result = JObject.Parse(WebWrapper.Patch(editGuildUrl, DiscordClient.token, newNameJson));
        }

        public void AssignRoleToMember(DiscordRole role, DiscordMember member)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Members + $"/{member.ID}";
            string message = JsonConvert.SerializeObject(new { roles = new string[] { role.id } });
            Console.WriteLine(WebWrapper.Patch(url, DiscordClient.token, message));
        }
        public void AssignRoleToMember(List<DiscordRole> roles, DiscordMember member)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Members + $"/{member.ID}";
            List<string> rolesAsIds = new List<string>();
            roles.ForEach(x => rolesAsIds.Add(x.id));
            string message = JsonConvert.SerializeObject(new { roles = rolesAsIds.ToArray() });
            Console.WriteLine(WebWrapper.Patch(url, DiscordClient.token, message));
        }

        public DiscordRole CreateRole()
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Roles;
            var result = JObject.Parse(WebWrapper.Post(url, DiscordClient.token, ""));

            if (result != null)
            {
                DiscordRole d = new DiscordRole
                {
                    color = new Color(result["color"].ToObject<int>().ToString("x")),
                    hoist = result["hoist"].ToObject<bool>(),
                    id = result["id"].ToString(),
                    managed = result["managed"].ToObject<bool>(),
                    name = result["name"].ToString(),
                    permissions = new DiscordPermission(result["permissions"].ToObject<uint>()),
                    position = result["position"].ToObject<int>()
                };

                this.roles.Add(d);
                return d;
            }
            return null;
        }

        public DiscordRole EditRole(DiscordRole role)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Roles + $"/{role.id}";
            string request = JsonConvert.SerializeObject(
                new
                {
                    color = decimal.Parse(role.color.ToDecimal().ToString()),
                    hoist = role.hoist,
                    name = role.name,
                    permissions = role.permissions.GetRawPermissions()
                }
            );

            var result = JObject.Parse(WebWrapper.Patch(url, DiscordClient.token, request));
            if (result != null)
            {
                DiscordRole d = new DiscordRole
                {
                    color = new Color(result["color"].ToObject<int>().ToString("x")),
                    hoist = result["hoist"].ToObject<bool>(),
                    id = result["id"].ToString(),
                    managed = result["managed"].ToObject<bool>(),
                    name = result["name"].ToString(),
                    permissions = new DiscordPermission(result["permissions"].ToObject<uint>()),
                    position = result["position"].ToObject<int>()
                };

                this.roles.Remove(d);
                this.roles.Add(d);
                return d;
            }

            return null;
        }

        public void DeleteRole(DiscordRole role)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Roles + $"/{role.id}";
            WebWrapper.Delete(url, DiscordClient.token);
        }

        public void DeleteChannel(DiscordChannel channel)
        {
            string url = Endpoints.BaseAPI + Endpoints.Channels + $"/{channel.id}";
            WebWrapper.Delete(url, DiscordClient.token);
        }

        public DiscordChannel CreateChannel(string ChannelName, bool voice)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{this.id}" + Endpoints.Channels;
            var reqJson = JsonConvert.SerializeObject(new { name = ChannelName, type = voice ? "voice" : "text" });
            var result = JObject.Parse(WebWrapper.Post(url, DiscordClient.token, reqJson));
            if (result != null)
            {
                DiscordChannel dc = new DiscordChannel { name = result["name"].ToString(), id = result["id"].ToString(), type = result["type"].ToString(), is_private = result["is_private"].ToObject<bool>(), topic = result["topic"].ToString() };
                this.channels.Add(dc);
                return dc;
            }
            return null;
        }

        /// <summary>
        /// Retrieves a DiscordMember List of members banned in this server.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public List<DiscordMember> GetBans()
        {
            List<DiscordMember> returnVal = new List<DiscordMember>();
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{id}" + Endpoints.Bans;
            try
            {
                JArray response = JArray.Parse(WebWrapper.Get(url, DiscordClient.token));
                if (response != null && response.Count > 0)
                {
                    parentclient.GetTextClientLogger.Log($"Ban count: {response.Count}");

                    foreach (var memberStub in response)
                    {
                        DiscordMember temp = JsonConvert.DeserializeObject<DiscordMember>(memberStub["user"].ToString());
                        if (temp != null)
                            returnVal.Add(temp);
                        else
                            parentclient.GetTextClientLogger.Log($"memberStub[\"user\"] was null?! Username: {memberStub["user"]["username"].ToString()} ID: {memberStub["user"]["username"].ToString()}", MessageLevel.Error);
                    }
                }
                else
                    return returnVal;
            }
            catch (Exception ex)
            {
                parentclient.GetTextClientLogger.Log($"An error ocurred while retrieving bans for server \"{name}\"\n\tMessage: {ex.Message}\n\tStack: {ex.StackTrace}",
                    MessageLevel.Error);
            }
            return returnVal;
        }

        public void RemoveBan(string userID)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{id}" + Endpoints.Bans + $"/{userID}";
            try
            {
                WebWrapper.Delete(url, DiscordClient.token);
            }
            catch (Exception ex)
            {
                parentclient.GetTextClientLogger.Log($"Error during RemoveBan\n\tMessage: {ex.Message}\n\tStack: {ex.StackTrace}", MessageLevel.Error);
            }
        }
        public void RemoveBan( DiscordMember member)
        {
            string url = Endpoints.BaseAPI + Endpoints.Guilds + $"/{id}" + Endpoints.Bans + $"/{member.ID}";
            try
            {
                WebWrapper.Delete(url, DiscordClient.token);
            }
            catch (Exception ex)
            {
                parentclient.GetTextClientLogger.Log($"Error during RemoveBan\n\tMessage: {ex.Message}\n\tStack: {ex.StackTrace}", MessageLevel.Error);
            }
        }
    }
}
