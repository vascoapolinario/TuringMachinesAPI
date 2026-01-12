using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TuringMachinesAPI.Services
{
    public class DiscordWebhookService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;

        public DiscordWebhookService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _webhookUrl = config["Discord:WebhookUrl"]
                          ?? throw new Exception("Discord Webhook URL not set");
        }
        private async Task SendEmbedAsync(string title, string description, int color, string username = "Server Notifier")
        {
            var payload = new
            {
                username,
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color,
                        timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(_webhookUrl, data);
        }

        public async Task SendMessageAsync(string content)
        {
            var payload = new { content };
            var json = JsonSerializer.Serialize(payload);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(_webhookUrl, data);
        }

        public async Task NotifyNewPlayerAsync(string playerName)
        {
            await SendEmbedAsync(
                title: "New Player Created!",
                description: $"Player **{playerName}** has created an account in the game.",
                color: 0x57F287,
                username: "Players Bot"
            );
        }

        public async Task NotifyNewWorkshopItemAsync(string itemType, string itemName, string uploader)
        {
            await SendEmbedAsync(
                title: "New Workshop Item Uploaded!",
                description: $"**{uploader}** uploaded a {itemType} called: *{itemName}*.",
                color: 0x5865F2,
                username: "Workshop Bot"
            );
        }

        public async Task NotifyNewLobbyAsync(string creator, string code, string levelName)
        {
            await SendEmbedAsync(
                title: "New Lobby Created!",
                description: $"**{creator}** created a new lobby to solve level: {levelName}, with the code: `{code}`.",
                color: 0xFEE75C,
                username: "Lobby Bot"
            );
        }

        public async Task NotifyNewDiscussionAsync(string discussionTitle, string discussionAuthor)
        {
            await SendEmbedAsync(
                title: "New Discussion Created!",
                description: $"**{discussionAuthor}** started a new discussion: *{discussionTitle}*.",
                color: 0xEB459E,
                username: "Community Bot"
            );
        }

        public async Task NotifyNewDiscussionPostAsync(string discussionTitle, string postAuthorName)
        {
            await SendEmbedAsync(
                title: "New Post in Discussion!",
                description: $"**{postAuthorName}** posted a new message in the discussion: *{discussionTitle}*.",
                color: 0xEB459E,
                username: "Community Bot"
            );
        }
    }
}
