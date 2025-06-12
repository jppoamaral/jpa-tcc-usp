using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

public class ChatAgent : IAgent
{
    public string Name => "AzureChatAgent";

    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _deployment;

    public ChatAgent(IConfiguration config)
    {
        _endpoint = config["AzureOAIEndpoint"]!;
        _key = config["AzureOAIKey"]!;
        _deployment = config["AzureOAIDeploymentName"]!;

        AzureOpenAIClient azureClient = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_key));    
        ChatClient _client = azureClient.GetChatClient(_deployment);
    }
    public async Task RunAsync(CancellationToken token)
    {
        //Initialize messages list
        var messagesList = new List<ChatMessage>();

        do
        {
            // Pause for system message update
            Console.WriteLine("-----------\nPausing the app to allow you to change the system prompt.\nPress any key to continue...");
            Console.ReadKey();

            Console.WriteLine("\nUsing system message from system.txt");
            string systemMessage = System.IO.File.ReadAllText("system.txt");
            systemMessage = systemMessage.Trim();

            Console.WriteLine("\nEnter user message or type 'quit' to exit:");
            string userMessage = Console.ReadLine() ?? "";
            userMessage = userMessage.Trim();

            if (systemMessage.ToLower() == "quit" || userMessage.ToLower() == "quit")
            {
                break;
            }
            else if (string.IsNullOrEmpty(systemMessage) || string.IsNullOrEmpty(userMessage))
            {
                Console.WriteLine("Please enter a system and user message.");
                continue;
            }
            else
            {
                // Format and send the request to the model
                messagesList.Add(new SystemChatMessage(systemMessage));
                messagesList.Add(new UserChatMessage(userMessage));
                GetResponseFromOpenAI(messagesList);
                //GetResponseFromOpenAI(systemMessage, userMessage);
            }
        } while (true);

    }

    // Define the function that gets the response from Azure OpenAI endpoint
    private static void GetResponseFromOpenAI(List<ChatMessage> messagesList)
    {
        Console.WriteLine("\nSending prompt to Azure OpenAI endpoint...\n\n");

        // // Configure the Azure OpenAI client
        // AzureOpenAIClient azureClient = new(new Uri(oaiEndpoint),
        //     new AzureKeyCredential(oaiKey));
        // ChatClient chatClient = azureClient.GetChatClient(oaiDeploymentName);

        // Get response from Azure OpenAI
        ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions()
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 800
        };

        // ChatCompletion completion = chatClient.CompleteChat(
        //     messagesList,
        //     chatCompletionOptions
        // );

        // Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        // messagesList.Add(new AssistantChatMessage(completion.Content[0].Text));
    }
}
