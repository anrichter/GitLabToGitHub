using System;
using System.Linq;
using System.Text.RegularExpressions;
using Octokit;

namespace GitLabToGitHub
{
    internal class GitHubConnector
    {
        private readonly GitHubClient _gitHubClient;

        public GitHubConnector(GitHubSettings gitHubSettings)
        {
            _gitHubClient = new GitHubClient(new ProductHeaderValue("GitLabToGitHub"))
            {
                Credentials = new Credentials(gitHubSettings.AccessToken)
            };
        }

        public NewRepository GetNewRepository(string sourceGroupName, string sourceProjectName)
        {
            sourceGroupName = Regex.Replace(sourceGroupName, @"\s+", string.Empty);
            sourceProjectName = Regex.Replace(sourceProjectName, @"\s+", string.Empty);
            var repositoryName = $"{sourceGroupName}_{sourceProjectName}";

            Console.WriteLine($"New GitHub Repository name [{repositoryName}]: ");
            var userInput = Console.ReadLine();
            repositoryName = string.IsNullOrEmpty(userInput) ? repositoryName : userInput;
            var newRepository = new NewRepository(repositoryName);

            Console.WriteLine($"Should Repository {repositoryName} be Private or Public [R/u]?");
            var allowedKeys = new[] { ConsoleKey.R, ConsoleKey.U, ConsoleKey.Enter };
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));
            newRepository.Private = userInputKeyInfo.Key != ConsoleKey.U;

            return newRepository;
        }
    }
}