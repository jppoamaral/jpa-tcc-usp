// Implicit using statements are included
using System.Text;
using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Azure;

// Add Azure OpenAI packages
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Threading.Tasks;

// Build a config object and retrieve user settings.
class ChatMessageLab
{
    static string? oaiEndpoint;
    static string? oaiKey;
    static string? oaiDeploymentName;
    static async Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

        oaiEndpoint = config["AzureOAIEndpoint"];
        // oaiKey = config["AzureOAIKey"];
        oaiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        oaiDeploymentName = config["AzureOAIDeploymentName"];
        List<string> settings = new List<string>();
        settings.Add(oaiEndpoint ?? "");
        settings.Add(oaiKey ?? "");
        settings.Add(oaiDeploymentName ?? "");

        ChatAgent agent = new ChatAgent(settings);
        await agent.RunAsync();
    }

    // Define the function that gets the response from Azure OpenAI endpoint
    private static void GetResponseFromOpenAI(List<ChatMessage> messagesList)
    {
        Console.WriteLine("\nSending prompt to Azure OpenAI endpoint...\n\n");

        if (string.IsNullOrEmpty(oaiEndpoint) || string.IsNullOrEmpty(oaiKey) || string.IsNullOrEmpty(oaiDeploymentName))
        {
            Console.WriteLine("Please check your appsettings.json file for missing or incorrect values.");
            return;
        }

        // Configure the Azure OpenAI client
        AzureOpenAIClient azureClient = new(new Uri(oaiEndpoint),
            new AzureKeyCredential(oaiKey));
        ChatClient chatClient = azureClient.GetChatClient(oaiDeploymentName);

        // Get response from Azure OpenAI
        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 800
        };

        ChatCompletion completion = chatClient.CompleteChat(
            messagesList,
            chatCompletionOptions
        );

        Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        messagesList.Add(new AssistantChatMessage(completion.Content[0].Text));
    }
}
