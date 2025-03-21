﻿using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace DisRipper
{
    public class HttpHandler
    {
        private readonly HttpClient httpClient;
        private string? token;
        private readonly Structs.Discord discord;
        private HttpStatusCode statusCode;
        private string? TestResponseString;
        private string DiscordEmoji = "https://cdn.discordapp.com/emojis/";
        private string DiscordSticker = "https://media.discordapp.net/stickers/";
        private string Lossless = "?quality=lossless&size=2048";

        public HttpHandler()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://discordapp.com/api/v6");
            discord = new Structs.Discord();
        }

        private bool TestConnection()
        {
            if (Utility.IsTokenCanceled())
                return false;

            HttpResponseMessage? request = SendRequest(discord.Self);
            if (request?.StatusCode == HttpStatusCode.OK)
            {
                statusCode = request.StatusCode;
                TestResponseString = request.Content.ReadAsStringAsync().Result;
                return true;
            }

            return false;
        }

        public HttpResponseMessage? SendRequest(string location)
        {
            if (Utility.IsTokenCanceled())
                return null;

            using (var messageRequest = new HttpRequestMessage(HttpMethod.Get, httpClient.BaseAddress+location))
            {
                if(string.IsNullOrEmpty(token)) { Utility.PrintToResponseBox(this, new Utility.PrintEventArgs("TokenSource is not set!")); return null; }
                messageRequest.Headers.Add("Authorization", token);

                HttpResponseMessage response = httpClient.SendAsync(messageRequest).Result;
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Utility.PrintToResponseBox(this, new Utility.PrintEventArgs($"{ex.Message}\n\n{response.Content.ReadAsStringAsync().Result}"));
                    return null;
                }

                return response;
            }
        }

        public HttpResponseMessage? SendRequest(ulong id, string fileExtension, bool sticker = false)
        {
            if (Utility.IsTokenCanceled())
                return null;

            Uri location;

            switch (sticker)
            {
                case true:
                    location = new Uri(DiscordSticker);
                    break;
                case false:
                    location = new Uri(DiscordEmoji);
                    break;
            }

            using (var messageRequest = new HttpRequestMessage(HttpMethod.Get, $"{location}{id}{fileExtension}{Lossless}"))
            {
#if DEBUG
                File.AppendAllText("urls.txt", $"{messageRequest.RequestUri.ToString()}\n");
#endif

                HttpResponseMessage response = httpClient.SendAsync(messageRequest).Result;
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch(Exception ex)
                {
                    Utility.PrintToResponseBox(this, new Utility.PrintEventArgs($"{ex.Message}"));
                    return null;
                }

                return response;
            }
        }

        public bool SetToken(string token)
        {
            if (Utility.IsTokenCanceled())
                return false;

            this.token = token;
            return TestConnection();
        }

        internal HttpStatusCode GetLastStatusCode()
        {
            return statusCode;
        }

        internal string GetTestResponse()
        {
            return TestResponseString ?? string.Empty;
        }

        public async Task<JObject?> GetGuild(ulong GuildId)
        {
            if (Utility.IsTokenCanceled())
                return null;

            HttpResponseMessage? Response = SendRequest($"{discord.Guild}");
            try
            {
                Response?.EnsureSuccessStatusCode();
                if (Response != null || !String.IsNullOrEmpty(Response.Content.ReadAsStream().ToString()))
                {
                    JObject Guild = JObject.Parse(Response.Content.ReadAsStringAsync().Result);
                    return Guild;
                }

                return null;
            }
            catch(HttpRequestException ex)
            {
                Utility.PrintToResponseBox(this, new Utility.PrintEventArgs($"HttpHandler->GetGuild: {ex.HttpRequestError}"));
                return null;
            }

        }
    }
}