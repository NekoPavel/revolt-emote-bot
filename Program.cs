using System.Reflection;
using System.Text.RegularExpressions;
using RevoltSharp;
using RevoltSharp.Commands;
using RevoltSharp.Rest;
using System.Text.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static void Main(string[] args)
    {
        Start().GetAwaiter().GetResult();
    }

    public static RevoltClient Client;
    public static async Task Start()
    {
        string token = Environment.GetEnvironmentVariable("REVOLT_BOT_TOKEN") ?? throw new ArgumentException("No token in enviorment");
        Client = new RevoltClient(token, ClientMode.WebSocket, new ClientConfig
        {
            UserBot = true,
            Debug = new ClientDebugConfig
            {
                LogRestRequest = true,
                LogRestRequestJson = true,
                LogRestResponseJson = true,
                LogWebSocketError = true,
                LogWebSocketFull = true
            }
        });
        await Client.StartAsync();
        CommandHandler Commands = new(Client);
        Commands.Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        await Task.Delay(-1);
    }
}
public class CommandHandler
{
    public CommandHandler(RevoltClient client)
    {
        Client = client;
        client.OnMessageRecieved += Client_OnMessageRecieved;
    }
    public RevoltClient Client;
    public CommandService Service = new();
    private void Client_OnMessageRecieved(Message msg)
    {
        if (msg is not UserMessage Message)
            return;
        int argPos = 0;
        if (!(Message.HasCharPrefix('!', ref argPos) || Message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
            return;
        CommandContext context = new(Client, Message);
        _ = Service.ExecuteAsync(context, argPos, null);
    }
}
public partial class AddEmoteCmd : ModuleBase
{
    [Command ("ping")]
    public async Task Ping(){
        await ReplyAsync("Pong");
    }
    [Command("yoink")]
    public async Task Yoink([Remainder] string emoteLink)
    {
        if (emoteLink == null)
        {
            await ReplyAsync("You need to provide an emote to yoink! ");
            return;
        }
        if (EmoteRegex().Match(emoteLink) == null)
        {
            await ReplyAsync("That's not a valid emote! ");
            return;
        }
        string emoteId = emoteLink.Split("/")[^1];
        string emoteUrl = $"https://cdn.7tv.app/emote/{emoteId}/3x.webp";

        byte[] image;
        using (var httpClient = new HttpClient())
        using (HttpResponseMessage response = await httpClient.GetAsync(emoteUrl))
        {
            image = await response.Content.ReadAsByteArrayAsync();
        }
        Uri emoteInfoUri = new($"https://api.7tv.app/v2/emotes/{emoteId}");
        string json;
        string emoteName;
        using (var httpClient = new HttpClient())
        using (HttpResponseMessage response = await httpClient.GetAsync(emoteInfoUri))
        {
            json = await response.Content.ReadAsStringAsync();
            
            dynamic emoteInfo = JObject.Parse(json);
            emoteName = emoteInfo.name;
            emoteName = emoteName.ToLower();
        }

        //byte[] image = File.ReadAllBytes("pagmanbounce.webp");
        try
        {
            Emoji newEmoji = await Context.Server.CreateEmojiAsync(image,$"{emoteName}.webp",emoteName);
            await ReplyAsync($"Yoinked {emoteName} :{newEmoji.Id}:");
        }
        catch
        {
            await ReplyAsync("Something went wrong while uploading the emote! ");
        }
        
        //FileAttachment FileAttachment = await Context.Channel.UploadFileAsync(image, "pagmanbounce.webp", UploadFileType.Emoji);

        //Emoji test = await Context.Server.CreateEmojiAsync(FileAttachment.Id, "pagmanbounce", false);

    }

    [GeneratedRegex("^https://7tv\\.app/emotes/[a-z0-9]{24}$")]
    private static partial Regex EmoteRegex();
}