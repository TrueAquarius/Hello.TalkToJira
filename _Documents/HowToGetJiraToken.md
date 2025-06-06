# How to get a Jira Personal Access Token

In order to connect to Jira, you will need a Personal Access Token (PAT). Once created, the PAT must be stored in the ```Configuration.json``` file

If you're on Jira Server 8.14+ or Jira Data Center, you can create a Personal Access Token (PAT) yourself.

## Steps to Create a PAT:

- Go to User Profile > Personal Access Tokens.

- Click Create token.

- Provide a label and expiry date.

- Copy the token and save it securely.

## Store the PAT in Configuration.json

The configuration file is typically located here:

```powershell
c:\Users\[Username]\AppData\Roaming\TrueAquarius\TalkToJira\Configuration.json
```
The content of the file looks something like this:

```poweshell
{
  "DeploymentName": "gpt-4o",
  "HistoryLength": 10,
  "Temperature": 0.1,
  "MaxOutputTokenCount": 2000,
  "SystemPrompt": "You are a helpful assistant. Please answer the user\u0027s questions to the best of your ability.",
  "Jira": {
    "BaseURL": "https://abc.example.net/jira",
    "ApiToken": "jkadfhjahsdfjhjadfhkjdfhkjieiehfnc",
    "Username": "name@example.com",
    "Project": "ABCDEFG"
  }
}
```
Paste your PAT to the field ```ApiToken``` in section ```Jira```.
