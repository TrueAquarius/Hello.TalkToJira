using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using System.Net;


namespace TrueAquarius.TalkToJira;


public static class Program
{
    private static Configuration config = Configuration.Instance;
    private static ChatHistory history = new ChatHistory();

    // Define colors for different message types
    private const ConsoleColor UserColor = ConsoleColor.Cyan;
    private const ConsoleColor BotColor = ConsoleColor.Green;
    private const ConsoleColor SystemColor = ConsoleColor.Yellow;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor InfoColor = ConsoleColor.White;

    public static async Task Main(string[] args)
    {
        // Load Azure OpenAI Key and Endpoint from Environment variables  
        string? azureOpenAIAPIKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        string? azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

        if (string.IsNullOrEmpty(azureOpenAIAPIKey)
            || string.IsNullOrEmpty(azureOpenAIEndpoint))
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Please set relevant environment variables.");
            Console.ResetColor();
            return;
        }

        // Check if Jira Configuration is set up correctly
        if (string.IsNullOrEmpty(config.Jira.BaseURL)
            || Uri.TryCreate(config.Jira.BaseURL, UriKind.Absolute, out Uri? baseUri) == false
            || string.IsNullOrEmpty(config.Jira.ApiToken)
            || string.IsNullOrEmpty(config.Jira.Username))
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Please set the Jira configuration in the configuration file.");
            Console.ResetColor();
            return;
        }

        // Get Proxy Settings from Environment variables (optional)
        string? httpProxyUrl = Environment.GetEnvironmentVariable("HTTP_PROXY");
        
        // Configure HttpClient with proxy  
        var httpClientHandler = new HttpClientHandler
        {
            Proxy = new System.Net.WebProxy(httpProxyUrl),
            UseProxy = !string.IsNullOrEmpty(httpProxyUrl)
        };

        var httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(azureOpenAIEndpoint)
        };

        // Build and configure kernel  
        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
           deploymentName: config.DeploymentName,
           endpoint: azureOpenAIEndpoint,
           apiKey: azureOpenAIAPIKey,
           httpClient: httpClient
        );

        var kernel = builder.Build();


        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        
        // Add Jira plugin to the kernel
        JiraPlugin jiraPlugin = new JiraPlugin(config.Jira.BaseURL, config.Jira.Username, config.Jira.ApiToken);
        kernel.Plugins.AddFromObject(jiraPlugin, "Jira");
        

        OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            MaxTokens = config.MaxOutputTokenCount,
            Temperature = config.Temperature,
        };


        // Initialize History  
        history.Clear();
        history.AddSystemMessage(config.SystemPrompt);


        Console.ForegroundColor = SystemColor;
        Console.WriteLine("\n========== start chatting with Jira now ===============");
        Console.WriteLine("Type '/help' for help. Type '/exit' or '/quit' to quit.\n");

        while (true)
        {
            // Prompt the user for input
            Console.ForegroundColor = UserColor;
            Console.Write("\n> ");
            string? userPrompt = Console.ReadLine();
            Console.WriteLine("");

            // Check if the user input is null or empty
            if (string.IsNullOrEmpty(userPrompt))
            {
                Console.ForegroundColor = ErrorColor;
                Console.WriteLine("Please enter a valid prompt.");
                continue;
            }

            // Handle the command input by the user
            CommandType commandType = HandleCommand(userPrompt);

            // If the command type is EXIT, leave the loop to exit the chat
            if (commandType == CommandType.EXIT) break;

            switch (commandType)
            {
                case CommandType.PROMPT:
                    // User prompt is valid, continue to process it  
                    break;
                case CommandType.EMPTY:
                    // User entered an empty command, prompt again  
                    continue;
                case CommandType.COMMAND:
                    // Command was handled, continue to next iteration  
                    continue;
            }

            history.AddUserMessage(userPrompt);

            var response = chatCompletionService.GetStreamingChatMessageContentsAsync(history, settings, kernel);

            Console.ForegroundColor = BotColor;
            var botResponseBuilder = new System.Text.StringBuilder();

            // Await foreach to iterate over the IAsyncEnumerable
            await foreach (var update in response)
            {
                if (update is not null && update.Content is not null)
                {
                    foreach (var updatePart in update.Content)
                    {
                        Console.Write(updatePart); // Stream to console
                        botResponseBuilder.Append(updatePart); // Build complete response
                    }
                }
            }

            System.Console.WriteLine("");

            history.AddMessage(AuthorRole.Assistant, botResponseBuilder.ToString());

            CutChatHistory();
        }

        // Clean up, kiss and say bye bye
        Console.ForegroundColor = SystemColor;
        Console.WriteLine("\nBye bye!\nIt was fun to talk to you.\n\n\n");
        Console.ResetColor();
    }



    /// <summary>
    /// Cuts the chat history to max history length specified in the configuration.
    /// </summary>
    private static void CutChatHistory()
    {
        // Verlauf ggf. kürzen (SystemPrompt bleibt immer erhalten)
        int maxHistory = config.HistoryLength * 2; // User+Bot pro Runde
        if (history.Count > maxHistory + 1)
        {
            // Entferne die ältesten User+Bot-Paare, SystemPrompt bleibt
            history.RemoveRange(1, history.Count - (maxHistory + 1));
        }
    }


    /// <summary>
    /// Handles the command input by the user.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns>true: The command required the program to terminate.</returns>
    private static CommandType HandleCommand(string commandLine)
    {
        commandLine = commandLine?.Trim() ?? string.Empty;

        if (!commandLine.StartsWith("/"))
        {
            // If the command does not start with '/', treat it as a user prompt
            return CommandType.PROMPT;
        }

        commandLine = commandLine.Substring(1).Trim(); // Remove the leading '/' and trim whitespace

        if (string.IsNullOrEmpty(commandLine))
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Command cannot be empty. Type '/help' for help. Type '/exit' or '/quit' to quit.");
            return CommandType.EMPTY;
        }

        string cmd = commandLine.Split(' ')[0]; // Get the command name (first word)

        switch (cmd)
        {
            case "exit":
            case "quit":
                return CommandType.EXIT;
            case "help":
                return HelpCommand(CommandBody(commandLine));
            case "config":
                ShowCurrentConfiguration();
                return CommandType.COMMAND;
            case "system":
                ShowSystemPrompt();
                return CommandType.COMMAND;
            case "version":
                ShowVersion();
                return CommandType.COMMAND;
            case "model":
                return ModelCommand(CommandBody(commandLine));
            case "clear":
                history.RemoveRange(1, history.Count - 1); // Keep only the SystemPrompt
                return CommandType.COMMAND;
            case "history":
                return HistoryCommand(CommandBody(commandLine));
            case "temperature":
                return TemperatureCommand(CommandBody(commandLine));
            case "tokens":
                return TokensCommand(CommandBody(commandLine));
            default:
                Console.ForegroundColor = ErrorColor;
                Console.WriteLine("Unknown command. Type '/help' to see list of commands.");
                return CommandType.EMPTY;
        }
    }


    /// <summary>
    /// Removes the actual command part from the command line input and 
    /// returns all parameters (the "body" of the command).
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private static string CommandBody(string command)
    {
        if (string.IsNullOrEmpty(command) || !command.Contains(' '))
        {
            return string.Empty;
        }
        int spaceIndex = command.IndexOf(' ');
        return command.Substring(spaceIndex + 1).Trim();
    }


    /// <summary>
    /// Displays the help message with available commands and their descriptions.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static CommandType HelpCommand(string commandLine)
    {
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Available commands:");
        Console.WriteLine("/clear               -   Clear chat history");
        Console.WriteLine("/config              -   Show configuration");
        Console.WriteLine("/exit or /quit       -   Exit the chat");
        Console.WriteLine("/help                -   Show this help message");
        Console.WriteLine("/history             -   Show history length");
        Console.WriteLine("/history [length]    -   Set history length");
        Console.WriteLine("/model               -   Show the current model");
        Console.WriteLine("/model [model]       -   Set the model");
        Console.WriteLine("/quit or /exit       -   Exit the chat");
        Console.WriteLine("/system              -   Show the system prompt");
        Console.WriteLine("/temperature         -   Show the temperature");
        Console.WriteLine("/temperature [value] -   Set the current temperature");
        Console.WriteLine("/tokens              -   Show the current max. output tokens");
        Console.WriteLine("/tokens [value]      -   Set the max. output tokens");
        Console.WriteLine("/version             -   Show Version of Chat Bot");
        Console.WriteLine("Type your question or prompt to start chatting with the AI.");

        return CommandType.COMMAND;
    }


    /// <summary>
    /// Displays the current system prompt of the chat bot.
    /// </summary>
    private static void ShowSystemPrompt()
    {
        Console.ForegroundColor = SystemColor;
        Console.WriteLine("Current system prompt:");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(config.SystemPrompt);
    }


    /// <summary>
    /// Displays the current configuration of the chat bot, including model, history length, temperature, and max output tokens.
    /// </summary>
    private static void ShowCurrentConfiguration()
    {
        Console.ForegroundColor = SystemColor;
        Console.WriteLine("Current configuration:");
        Console.Write(" - Current model:       ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(config.DeploymentName);

        Console.ForegroundColor = SystemColor;
        Console.Write(" - History max. length: ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(config.HistoryLength);

        Console.ForegroundColor = SystemColor;
        Console.Write(" - Temperature:         ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(config.Temperature);

        Console.ForegroundColor = SystemColor;
        Console.Write(" - Max. Output Tokens:  ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(config.MaxOutputTokenCount);
    }


    /// <summary>
    /// Displays the version information of the chat bot application.
    /// </summary>
    private static void ShowVersion()
    {
        Console.ForegroundColor = SystemColor;
        Console.Write(" - Version:      ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(BuildInfo.Version);

        Console.ForegroundColor = SystemColor;
        Console.Write(" - Build Number: ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(BuildInfo.BuildNumber);

        Console.ForegroundColor = SystemColor;
        Console.Write(" - Date:         ");
        Console.ForegroundColor = InfoColor;
        Console.WriteLine(BuildInfo.BuildDate);
    }


    /// <summary>
    /// Handles the model command input by the user.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static CommandType ModelCommand(string commandLine)
    {
#if false
        if (string.IsNullOrEmpty(commandLine))
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("Current Model: " + config.DeploymentName);
            return CommandType.COMMAND;
        }

        string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        config.DeploymentName = parts[0];
        config.Save();

        chatClient = azureClient.GetChatClient(config.DeploymentName);

        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Model changed to: " + config.DeploymentName);
#endif
        Console.ForegroundColor = ErrorColor;
        Console.WriteLine("Model command is not implemented yet.");
        return CommandType.COMMAND;
    }


    /// <summary>
    /// Handles the history command input by the user.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static CommandType HistoryCommand(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("Current history length: " + history.Count / 2);
            Console.WriteLine("Max history length:     " + config.HistoryLength);
            return CommandType.COMMAND;
        }

        string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        bool success = int.TryParse(parts[0], out int newHistoryLength);

        if (!success || newHistoryLength < 0)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Invalid history length. Please enter a valid number greater than or equal to zero.");
            return CommandType.COMMAND;
        }

        config.HistoryLength = newHistoryLength;
        config.Save();
        CutChatHistory();

        Console.ForegroundColor = InfoColor;
        Console.WriteLine("History length set to: " + config.HistoryLength);

        return CommandType.COMMAND;
    }


    /// <summary>
    /// Handles the tokens command input by the user.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static CommandType TokensCommand(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("Current max. output tokens: " + config.MaxOutputTokenCount);
            return CommandType.COMMAND;
        }

        string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        bool success = int.TryParse(parts[0], out int newTokenCount);

        if (!success || newTokenCount <= 0)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Invalid token count. Please enter a valid number greater than zero.");
            return CommandType.COMMAND;
        }

        config.MaxOutputTokenCount = newTokenCount;
        config.Save();
#if false
        SetChatOptionsToCurrentConfiguration();
#endif
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Max. Output Token set to: " + config.MaxOutputTokenCount);

        return CommandType.COMMAND;
    }


    /// <summary>
    /// Handles the temperature command input by the user.
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    private static CommandType TemperatureCommand(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine("Current temperature: " + config.Temperature);
            return CommandType.COMMAND;
        }

        string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        bool success = float.TryParse(parts[0], out float newTemperature);
        
        if (!success || newTemperature < 0 || newTemperature > 1)
        {
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine("Invalid temperature. Please enter a valid number between 0 and 1.");
            return CommandType.COMMAND;
        }

        config.Temperature = newTemperature;
        config.Save();

#if false
        SetChatOptionsToCurrentConfiguration();
#endif
        Console.ForegroundColor = InfoColor;
        Console.WriteLine("Temperature set to: " + config.Temperature);
        
        return CommandType.COMMAND;
    }
}



