using System.Reflection;
using System.Text.RegularExpressions;
using RevoltSharp;
using RevoltSharp.Commands;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata;

public class Program
{
    public static readonly bool debug = bool.Parse(Environment.GetEnvironmentVariable("BOT_DEBUG") ?? "False");
    static void Main(string[] args)
    {
        Start().GetAwaiter().GetResult();
    }

    public static RevoltClient Client;
    public static async Task Start()
    {

        // Environment.SetEnvironmentVariable("REVOLT_BOT_TOKEN", "ONqeoPMOF-TiKj0zG9CskOlrxy4-div8_QHKxth57r80SFdtfZNJTQqqOR5duvMP"); lol old leaked token
        string token = Environment.GetEnvironmentVariable("REVOLT_BOT_TOKEN") ?? throw new ArgumentException("No token in enviroment");
        Client = new RevoltClient(token, ClientMode.WebSocket, new ClientConfig
        {
            UserBot = true,
            Debug = new ClientDebugConfig
            {
                LogRestRequest = debug,
                LogRestRequestJson = debug,
                LogRestResponseJson = debug,
                LogWebSocketError = debug,
                LogWebSocketFull = debug
            }
        });
        await Client.StartAsync();
        CommandHandler Commands = new(Client);
        Commands.Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);

        await Task.Delay(-1);
    }
    public static async Task<bool> EmoteExists(CommandContext context, string emoteName)
    {
        List<Emoji> serverEmojis = (await context.Server.GetEmojisAsync()).ToList();
        return serverEmojis.Exists(x => x.Name.Equals(emoteName));
    }
    public static async Task<byte[]> GetImage(Uri uri)
    {
        using var httpClient = new HttpClient();
        using HttpResponseMessage response = await httpClient.GetAsync(uri);
        return await response.Content.ReadAsByteArrayAsync();
    }
    public static async Task<byte[]> EmoteIdToImage(string emoteId)
    {
        Uri emoteUrl;
        byte[] image;
        for (int i = 4; i > 0; i--)
        {
            emoteUrl = new($"https://cdn.7tv.app/emote/{emoteId}/{i}]x.webp");
            image = await GetImage(emoteUrl);
            if (image.Length <= 512000)
            {
                return image;
            }
        }
        throw new Exception("Image too large");
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
    [Command("ping")]
    public async Task Ping()
    {
        await ReplyAsync("Pong");
    }
    [Command("yoink")]
    public async Task Yoink([Remainder] string input)
    {
        string emoteLink = input.Split(" ")[0];
        bool addAnyway = false;
        if (input.Split(" ").Length > 1)
        {
            bool.TryParse(input.Split(" ")[1], out addAnyway);
        }
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

        string emoteId = emoteLink.Split("/")[^1].Trim();
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

        if (await Program.EmoteExists(Context, emoteName) && !addAnyway)
        {
            await ReplyAsync("Emote already exists, if you want to add anyway, call '!yoink <emotelink> True'");
            return;
        }

        byte[] image;
        try
        {
            image = await Program.EmoteIdToImage(emoteId);
        }
        catch (Exception ex)
        {
            await ReplyAsync(ex.Message);
            return;
        }


        //byte[] image = File.ReadAllBytes("pagmanbounce.webp");
        try
        {
            Emoji newEmoji = await Context.Server.CreateEmojiAsync(image, $"{emoteName}.webp", emoteName);
            await ReplyAsync($"Added `{emoteName}` to the server! :{newEmoji.Id}:");
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