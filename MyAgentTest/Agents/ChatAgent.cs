using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

public class ChatAgent : IAgent
{
    public string Name => "AzureChatAgent";
    public ChatClient _client;
    public List<ChatMessage> messagesList = new List<ChatMessage>();
    private List<string> _settings;
    ChatTool callAssistantTool = ChatTool.CreateFunctionTool(
        functionName: nameof(CallAssistantAgent),
        functionDescription: "Get the user's travel history",
        functionParameters: BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "country": {
                    "type": "string",
                    "description": "The country name, e.g. Portugal. This can have the value 'country' if the user does not know which country they have visited. This can also be a list of countries, e.g. Portugal, Spain, and France. This can also be the number of coutries the user has visited, e.g. five. This is a number in words, not digits. This can have the value 'number' if the user does not know how many countries they have visited."
                }
            }
        }
        """)
    );

    public ChatAgent(List<string> settings)
    {
        AzureOpenAIClient azureClient = new AzureOpenAIClient(new Uri(settings[0]), new AzureKeyCredential(settings[1]));
        _client = azureClient.GetChatClient(settings[2]);
        _settings = settings;
    }
    public async Task RunAsync()
    {
        //Initialize messages list
        // var messagesList = new List<ChatMessage>();

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
                messagesList.Add(new SystemChatMessage(systemMessage));
                messagesList.Add(new UserChatMessage(userMessage));

                Console.WriteLine("\nSending prompt to Azure OpenAI endpoint...\n\n");
                ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions()
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 800,
                    Tools = { callAssistantTool }
                };

                ChatCompletion completion = await _client.CompleteChatAsync(
                    messagesList,
                    chatCompletionOptions
                );

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messagesList.Add(new AssistantChatMessage(completion));
                    Console.WriteLine("Tool call detected, processing...");
                    foreach (ChatToolCall toolCall in completion.ToolCalls)
                    {
                        Console.WriteLine($"Tool call detected: {toolCall.FunctionArguments}");
                        var toolResponse = GetToolCallContent(toolCall, chatCompletionOptions);
                        messagesList.Add(new ToolChatMessage(toolCall.Id, toolResponse.Content));
                    }
                }
                else if (completion.FinishReason == ChatFinishReason.Stop)
                {
                    Console.WriteLine("Chat completed without tool calls.");
                }
                else
                {
                    Console.WriteLine($"Chat finished with reason: {completion.FinishReason}");
                }
                completion = await _client.CompleteChatAsync(
                    messagesList,
                    chatCompletionOptions
                );
                Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
                // if (completion.Content[0].Text.Contains("Agent"))
                // {
                //     AssistantAgent assistant = new AssistantAgent(_settings);
                //     var assistantReply = assistant.CallAssistant(completion.Content[0].Text);
                //     messagesList.Add(new UserChatMessage(assistantReply));
                //     completion = await _client.CompleteChatAsync(messagesList, chatCompletionOptions);
                //     Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
                // }
            }
        } while (true);

    }
    private string CallAssistantAgent(ChatToolCall toolCall, ChatCompletionOptions options, string? country)
    {
        Console.WriteLine("dentro da chamada do assistente");
        Console.WriteLine($"Calling assistant with completion: {toolCall}");
        AssistantAgent assistant = new AssistantAgent(_settings);
        var assistantReply = assistant.CallAssistant(country);
        // messagesList.Add(new AssistantChatMessage(assistantReply));
        // var completion = _client.CompleteChat(messagesList, options);
        // Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
        return assistantReply;
    }

    public ToolChatMessage GetToolCallContent(ChatToolCall toolCall, ChatCompletionOptions options)
    {
        Console.WriteLine("Processing tool call content...");
        if (toolCall.FunctionName == callAssistantTool.FunctionName)
        {
            // Validate arguments before using them; it's not always guaranteed to be valid JSON!
            try
            {
                using JsonDocument argumentsDocument = JsonDocument.Parse(toolCall.FunctionArguments);
                JsonElement countryElement = default;
                // JsonElement numberElement = default;
                argumentsDocument.RootElement.TryGetProperty("country", out countryElement);
                // argumentsDocument.RootElement.TryGetProperty("number", out numberElement);

                string? country = countryElement.GetString() ?? string.Empty;
                // string? number = numberElement.GetString() ?? string.Empty;
                if (!string.IsNullOrEmpty(country))
                {
                    var assistantResponse = CallAssistantAgent(toolCall, options, country);
                    return new ToolChatMessage(toolCall.Id, assistantResponse);
                }
            }
            catch (JsonException)
            {
                // Handle the JsonException (bad arguments) here
            }
        }
        // Handle unexpected tool calls
        throw new NotImplementedException();
}   
}
