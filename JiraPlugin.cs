using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel; 

namespace TrueAquarius.TalkToJira
{
    /// <summary>
    /// The JiraPlugin class provides methods to interact with Jira's REST API.
    /// It has method-level annotations which allow it to be used as a plugin in a semantic kernel environment.
    /// </summary>
    internal class JiraPlugin
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _apiToken;

        public JiraPlugin(string baseUrl, string username, string apiToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _apiToken = apiToken;
        }


        public async Task<List<Issue>> ExecuteJQLAsync(string jql)
        {
            List<Issue> issues = new List<Issue>();

            using var httpClient = new HttpClient();

            // Set the HTTP headers  
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            var url = $"{_baseUrl}/rest/api/2/search?jql={Uri.EscapeDataString(jql)}";

            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();

                JiraResponse jiraResponse = JsonSerializer.Deserialize<JiraResponse>(json);

                if (jiraResponse?.Issues != null)
                {
                    issues.AddRange(jiraResponse.Issues);
                }
                return issues;
            }
            else
            {
                Console.WriteLine($"Request failed: {response.StatusCode}");
                return issues;
            }

        }


        [KernelFunction("get_all_jira_tickets")]
        [Description("Retrieves a list of all tickets for a given project")]
        [return: Description("An array of tickets.")]
        public async Task<List<Issue>> GetAllTicketsForProjectAsync([Description("This is the project key.")] string projectKey)
        {
            string jql = $"project={projectKey}";
            return await ExecuteJQLAsync(jql);
        }
    }



    public class JiraResponse
    {
        [JsonPropertyName("issues")]
        public List<Issue> Issues { get; set; } = new();
    }



    public class Issue
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("fields")]
        public Fields Fields { get; set; }
    }



    public class Fields
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }
    }



    public class Status
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
