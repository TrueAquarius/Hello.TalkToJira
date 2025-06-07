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
    public int HistoryLength { get; set; } = 10;
    public float Temperature { get; set; } = 0.3f;
    public int MaxOutputTokenCount { get; set; } = 1000;
    public string SystemPrompt { get; set; } = 
        @"You are an assistant who helps users of Jira with questions they have regarding Jira Tickets.
          Do not answer questions which are not related to tickets; say `Sorry, I cannot answer that question. I can only answer questions regarding Jira Tickets.'";

    public JiraConfiguration Jira { get; set; } = new JiraConfiguration();
}


internal class JiraConfiguration
{
    public string BaseURL { get; set; }  = "";
    public string ApiToken { get; set; } = "";
    public string Username { get; set; } = "";
}