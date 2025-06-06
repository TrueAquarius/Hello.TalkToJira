# Hello.TalkToJira  

This is a simple command line Chatbot to talk to your Jira System.

- <i>How many tickets are in Jira project ABCDEF?</i>
- <i>Based on the ticket infomation which is available, what kind of system is this project building?</i>

Like all my "Hello"-projects, this is a "Hello World" version of a chatbot, meaning that it is meant to be an experiment and not meant to be for production.

## Features  
- Chat with your Jira Instance from your Console  
- Switch between different models (more precisely: switch between different deployments in Azure AI) with the `/model` command.  <>
- Chat history; length of history can be set with `/history` command.  
- Get help with `/help` command  

## Technology Stack
- .Net 8
- nuGet: Microsoft.SemanticKernel (Version 1.55.0)
- nuGet: TrueAquarius.ConfigManager (Version 1.0.0)
- nuGet: Azure.AI.OpenAI (Version 2.2.0-beta.4)
- nuGet: Azure.Core (Version 1.46.1)
- Jira

## Configuration

- In order to connect to Jira, you will need a Personal Access Token (PAT). Once created, the PAT must be stored in the ```Configuration.json``` file. Please follow the instructions in [_Documents/HowToGetJiraToken.md](./_Documents/HowToGetJiraToken.md)


## How to use it
Type any prompt and get an answer from Azure OpenAI.

Change settings with `/`-commands. Type `/help` to get list of commands.

**Enjoy it!!!**

