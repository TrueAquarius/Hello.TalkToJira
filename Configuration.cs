using TrueAquarius.ConfigManager;

namespace TrueAquarius.TalkToJira;

/// <summary>
/// Represents the configuration settings for the application.
/// This is loaded on application startup and can be modified by the user.
/// The default values stated here are overwritten by the values in the config file.
/// The config file is typically located [user]/AppData/Roaming/TrueAquarius/TalkToJira/configuration.json.
/// </summary>
internal class Configuration : ConfigManager<Configuration>
{
    public string DeploymentName { get; set; } = "gpt-4o";
    public int HistoryLength { get; set; } = 5;
    public float Temperature { get; set; } = 0.7f;
    public int MaxOutputTokenCount { get; set; } = 1000;
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Please answer the user's questions to the best of your ability.";

    public JiraConfiguration Jira { get; set; } = new JiraConfiguration();
}


internal class JiraConfiguration
{
    public string BaseURL { get; set; }  = "<replace by your url>";
    public string ApiToken { get; set; } = "<replace by your token>";
    public string Username { get; set; } = "<replace by your username>";
    public string Project { get; set; }  = "<replace by your project name>";
}