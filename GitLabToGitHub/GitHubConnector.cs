using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabToGitHub.Settings;
using LibGit2Sharp;
using Octokit;
using Credentials = Octokit.Credentials;
using Repository = Octokit.Repository;

namespace GitLabToGitHub
{
    internal class GitHubConnector
    {
        private readonly GitHubSettings _gitHubSettings;
        private readonly UserMapper _userMapper;
        private readonly GitHubClient _gitHubClient;

        public GitHubConnector(GitHubSettings gitHubSettings, UserMapper userMapper)
        {
            _gitHubSettings = gitHubSettings;
            _userMapper = userMapper;
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

            Console.Write($"Should Repository {repositoryName} be Private? [Y/n]");
            var allowedKeys = new[] { ConsoleKey.Y, ConsoleKey.N, ConsoleKey.Enter };
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));
            newRepository.Private = userInputKeyInfo.Key != ConsoleKey.N;
            Console.WriteLine(string.Empty);

            return newRepository;
        }

        public async Task<Repository> CreateRepository(NewRepository newRepository)
        {
            return await _gitHubClient.Repository.Create(newRepository);
        }

        public void PushGitRepo(Repository gitHubRepository, string gitRepoPath)
        {
            using (var repo = new LibGit2Sharp.Repository(gitRepoPath))
            {
                if (repo.Network.Remotes.Any(r => r.Name == "github"))
                    repo.Network.Remotes.Remove("github");

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
        }

        public async Task CreateCollaborators(Repository repository, ICollection<string> usernames)
        {
            var existingCollaborators = await _gitHubClient.Repository.Collaborator.GetAll(repository.Id);
            var existingCollaboratorUsernames = existingCollaborators.Select(c => c.Login).ToList();

            var neededCollaboratorUsernames = usernames.Select(user => _userMapper.MapToGitHubUserName(user)).ToList();
            var newCollaboratorUsernames = neededCollaboratorUsernames.Except(existingCollaboratorUsernames);
            
            foreach (var newCollaboratorUsername in newCollaboratorUsernames)
            {
                await _gitHubClient.Repository.Collaborator.Add(repository.Id, newCollaboratorUsername);
            }
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
                var issueAssignees = issue.AssigneeUserNames.Select(name => _userMapper.MapToGitHubUserName(name)).ToList();

                var newIssue = new NewIssue(issue.Title);
                newIssue.Body = ComposeBody(issue, issueAssignees);

                if (issueAssignees.Contains(repository.Owner.Login))
                {
                    newIssue.Assignees.Add(repository.Owner.Login);
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

            string ComposeBody(TransferObjects.Issue issue, List<string> issueAssignees)
            {
                var body = issue.Description;
                body += $"{Environment.NewLine}{Environment.NewLine}";
                body += $"**Imported from GitLab**{Environment.NewLine}";
                body += $"Created from {_userMapper.MapToGitHubUserName(issue.AuthorUserName)} on {issue.CreatedAt:u}{Environment.NewLine}";
                if (issueAssignees.Any())
                {
                    body += $"Assignees: {string.Join(", ", issueAssignees)}{Environment.NewLine}";
                }
                body += $"*Comments:*{Environment.NewLine}";
                foreach (var comment in issue.Comments.OrderBy(c => c.Id))
                {
                    body += $"{Environment.NewLine}*{_userMapper.MapToGitHubUserName(comment.AuthorUsername)} on {comment.CreatedDate:u}*:{Environment.NewLine}{comment.Body}{Environment.NewLine}";
                }

                return body;
            }
        }
    }
}