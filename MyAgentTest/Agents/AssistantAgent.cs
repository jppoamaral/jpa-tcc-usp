using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

public class AssistantAgent : IAgent
{
    public string Name => "AzureAssistantAgent";
    public ChatClient _client;
    public AssistantAgent(List<string> settings)
    {
        AzureOpenAIClient azureClient = new AzureOpenAIClient(new Uri(settings[0]), new AzureKeyCredential(settings[1]));
        _client = azureClient.GetChatClient(settings[2]);
    }
    public async Task RunAsync()
    {
        Console.WriteLine("\nUsing system message from assistant.txt");
        string systemMessage = System.IO.File.ReadAllText("assistant.txt");
        systemMessage = systemMessage.Trim();

        Console.WriteLine("\nEnter user message or type 'quit' to exit:");
        string userMessage = Console.ReadLine() ?? "";
        userMessage = userMessage.Trim();

        Console.WriteLine("\nSending prompt to Azure OpenAI endpoint...\n\n");
        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 800
        };

        ChatCompletion completion = await _client.CompleteChatAsync(
            [systemMessage, userMessage],
            chatCompletionOptions
        );
        Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
    }

    public string CallAssistant(string country)
    {
        Console.WriteLine("dentro do CallAssistant");
        string systemMessage = System.IO.File.ReadAllText("assistant.txt");
        systemMessage = systemMessage.Trim();

        string userMessage = country;

        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 800
        };

        ChatCompletion completion = _client.CompleteChat(
            [systemMessage, userMessage],
            chatCompletionOptions
        );

        Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        return completion.Content[0].Text;
    }
}
