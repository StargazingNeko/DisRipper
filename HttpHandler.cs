using System;
using System.Net;
using System.Net.Http;
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
            using (var messageRequest = new HttpRequestMessage(HttpMethod.Get, httpClient.BaseAddress+location))
            {
                if(string.IsNullOrEmpty(token)) { MessageBox.Show("Token is not set!"); return null; }

                messageRequest.Headers.Add("Authorization", token);

                HttpResponseMessage response = httpClient.SendAsync(messageRequest).Result;
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\n\n{response.Content.ReadAsStringAsync().Result}");
                    return null;
                }

                return response;
            }
        }

        public HttpResponseMessage? SendRequest(ulong id, bool sticker = false)
        {
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

            using (var messageRequest = new HttpRequestMessage(HttpMethod.Get, $"{location}{id}{Lossless}"))
            {
                HttpResponseMessage response = httpClient.SendAsync(messageRequest).Result;
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return null;
                }

                return response;
            }
        }

        public bool SetToken(string token)
        {
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
    }
}