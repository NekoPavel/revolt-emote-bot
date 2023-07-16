using System.Reflection;
using RevoltSharp;
using RevoltSharp.Commands;
using RevoltSharp.Rest;

class Program
{
    static void Main(string[] args)
    {
        Start().GetAwaiter().GetResult();
    }

    public static RevoltClient Client;
    public static async Task Start()
    {
        Client = new RevoltClient(Environment.GetEnvironmentVariable("REVOLT_BOT_TOKEN"), ClientMode.WebSocket, new ClientConfig
        {
            UserBot = true
        });
        await Client.StartAsync();
        CommandHandler Commands = new CommandHandler(Client);
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
    public CommandService Service = new CommandService();
    private void Client_OnMessageRecieved(Message msg)
    {
        UserMessage Message = msg as UserMessage;
        if (Message == null || Message.Author.IsBot)
            return;
        int argPos = 0;
        if (!(Message.HasCharPrefix('!', ref argPos) || Message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
            return;
        CommandContext context = new CommandContext(Client, Message);
        Service.ExecuteAsync(context, argPos, null);
    }
}
public class Parent
{
    public string type { get; set; }
    public string id { get; set; }

}
public class AddemoteCmd : ModuleBase
{
    [Command("addemote")]
    public async Task Addemote()
    {
        byte[] image = System.IO.File.ReadAllBytes("pagmanbounce.webp");
        FileAttachment FileAttachment = await Context.Channel.UploadFileAsync(image, "pagmanbounce.webp", UploadFileType.Emojis);

        Emoji test = await Context.Server.CreateEmojiAsync(FileAttachment.Id, "pagmanbounce", false);
    }
}