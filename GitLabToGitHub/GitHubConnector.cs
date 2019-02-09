using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using Octokit;
using Credentials = Octokit.Credentials;
using Repository = Octokit.Repository;

namespace GitLabToGitHub
{
    internal class GitHubConnector
    {
        private readonly GitHubSettings _gitHubSettings;
        private readonly GitHubClient _gitHubClient;

        public GitHubConnector(GitHubSettings gitHubSettings)
        {
            _gitHubSettings = gitHubSettings;
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

            Console.Write($"Should Repository {repositoryName} be Private or Public [R/u]?");
            var allowedKeys = new[] { ConsoleKey.R, ConsoleKey.U, ConsoleKey.Enter };
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));
            newRepository.Private = userInputKeyInfo.Key != ConsoleKey.U;
            Console.WriteLine(string.Empty);

            return newRepository;
        }

        public async Task<Repository> CreateRepository(NewRepository newRepository)
        {
            Console.Write("Create GitHub Repository... ");
            var repository = await _gitHubClient.Repository.Create(newRepository);
            Console.WriteLine("Done.");
            return repository;
        }

        public void PushGitRepo(Repository gitHubRepository, string gitRepoPath)
        {
            Console.Write($"Push Git Repository to GitHub Repository >{gitHubRepository.FullName}<... ");

            using (var repo = new LibGit2Sharp.Repository(gitRepoPath))
            {
                var remote = repo.Network.Remotes.Add("github", gitHubRepository.CloneUrl, "+refs/*:refs/*");
                repo.Config.Set("remote.github.mirror", true);

                var pushOptions = new PushOptions
                {
                    CredentialsProvider = (url, fromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = _gitHubSettings.AccessToken,
                            Password = string.Empty
                        }
                };

                var refs = repo.Refs.Select(r => r.CanonicalName);
                repo.Network.Push(remote, refs, pushOptions);
            }
            Console.WriteLine("Done.");
        }
    }
}