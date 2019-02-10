using System;
using System.Collections.Generic;
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
            Console.Write($"Git: Push >{gitRepoPath}< to repository >{gitHubRepository.FullName}<... ");

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

        public async Task<ICollection<TransferObjects.Milestone>> CreateMilestones(Repository repository, ICollection<TransferObjects.Milestone> milestones)
        {
            var sortedMilestones = milestones.OrderBy(m => m.SourceId);
            foreach (var milestone in sortedMilestones)
            {
                var newMilestone = new NewMilestone(milestone.Title)
                {
                    Description = milestone.Description,
                    State = milestone.Closed ? ItemState.Closed : ItemState.Open
                };
                var createdMilestone = await _gitHubClient.Issue.Milestone.Create(repository.Id, newMilestone);
                milestone.TargetId = createdMilestone.Number;
            }

            return milestones;
        }

        public async Task CreateIssues(Repository repository, ICollection<TransferObjects.Issue> issues, ICollection<TransferObjects.Milestone> milestones)
        {
            var sortedIssues = issues.OrderBy(i => i.Id);
            foreach (var issue in sortedIssues)
            {
                var newIssue = new NewIssue(issue.Title);
                newIssue.Body = ComposeBody(issue);
                foreach (var userName in issue.AssigneeUserNames)
                {
                    newIssue.Assignees.Add(userName);
                }
                foreach (var label in issue.Labels)
                {
                    newIssue.Labels.Add(label);
                }
                if (issue.MilestoneId.HasValue)
                {
                    newIssue.Milestone = milestones.Single(ms => ms.SourceId == issue.MilestoneId.Value)?.TargetId;
                }

                var createdIssue = await _gitHubClient.Issue.Create(repository.Id, newIssue);

                if (issue.Closed)
                {
                    var issueUpdate = createdIssue.ToUpdate();
                    issueUpdate.State = ItemState.Closed;
                    await _gitHubClient.Issue.Update(repository.Id, createdIssue.Number, issueUpdate);
                }
            }

            string ComposeBody(TransferObjects.Issue issue)
            {
                var body = issue.Description;
                body += $"{Environment.NewLine}{Environment.NewLine}";
                body += $"**Imported from GitLab**{Environment.NewLine}";
                body += $"Created from {issue.AuthorUserName} on {issue.CreatedAt:u}{Environment.NewLine}";
                body += $"*Comments:*{Environment.NewLine}";
                foreach (var comment in issue.Comments.OrderBy(c => c.Id))
                {
                    body += $"{Environment.NewLine}*{comment.AuthorUsername} on {comment.CreatedDate:u}*:{Environment.NewLine}{comment.Body}{Environment.NewLine}";
                }

                return body;
            }
        }
    }
}