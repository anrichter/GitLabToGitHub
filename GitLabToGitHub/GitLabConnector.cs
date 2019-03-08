using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Milestones.Responses;
using GitLabApiClient.Models.Projects.Responses;
using GitLabToGitHub.Settings;
using LibGit2Sharp;

namespace GitLabToGitHub
{
    internal class GitLabConnector
    {
        private readonly GitLabSettings _gitLabSettings;
        private readonly GitLabClient _gitLabClient;

        private const int FakeGroupIdForAllProjects = 0;

        public GitLabConnector(GitLabSettings gitLabSettings)
        {
            _gitLabSettings = gitLabSettings;
            _gitLabClient = new GitLabClient(gitLabSettings.Url, gitLabSettings.AccessToken);
        }

        public async Task<Project> GetSourceProject()
        {
            try
            {
                var availableSourceGroups = await _gitLabClient.Groups.GetAsync();
                AddFakeGroupToListAllProjects(availableSourceGroups);
                var sourceGroup = SelectGitLabGroup(availableSourceGroups);
                var availableProjectsInSourceGroup = await GetProjectsForGroup(sourceGroup);
                var sourceProject = SelectGitLabProject(availableProjectsInSourceGroup);
                return sourceProject;
            }
            catch (GitLabException e)
            {
                Console.WriteLine($"There is a problem to reach your GitLab Server. {e.Message}");
                Console.WriteLine("Please check your GitLab Settings.");
                Environment.Exit(1);
                throw;
            }

            void AddFakeGroupToListAllProjects(IList<Group> availableSourceGroups)
            {
                var fakeGroup = new Group { Id = FakeGroupIdForAllProjects, Name = "All Projects" };
                availableSourceGroups.Add(fakeGroup);
            }

            Group SelectGitLabGroup(IList<Group> gitLabGroups)
            {
                Console.WriteLine("Select Source GitLab Group:");
                Console.WriteLine("Id\tName");
                foreach (var gitLabGroup in gitLabGroups)
                {
                    Console.WriteLine($"{gitLabGroup.Id}\t{gitLabGroup.Name}");
                }

                Console.WriteLine("Group Id: ");
                var userInput = Console.ReadLine();
                if (!int.TryParse(userInput, out int selectedGroupId))
                {
                    Console.WriteLine("Please insert a valid Group Id.");
                    Environment.Exit(1);
                }

                var selectedGroup = gitLabGroups.FirstOrDefault(g => g.Id == selectedGroupId);
                if (selectedGroup == null)
                {
                    Console.WriteLine("Please insert a valid Group Id.");
                    Environment.Exit(1);
                }

                return selectedGroup;
            }

            async Task<IList<Project>> GetProjectsForGroup(Group group)
            {
                if (group.Id == FakeGroupIdForAllProjects)
                {
                    var projects = await _gitLabClient.Projects.GetAsync();
                    return projects.Where(p => !p.Archived).ToList();
                }
                return await _gitLabClient.Groups.GetProjectsAsync(group.Id.ToString());
            }

            Project SelectGitLabProject(IList<Project> gitLabProjects)
            {
                Console.WriteLine("Select Source GitLab Project:");
                Console.WriteLine("Id\tName");
                foreach (var gitLabProject in gitLabProjects)
                {
                    Console.WriteLine($"{gitLabProject.Id}\t{gitLabProject.Name}");
                }

                Console.WriteLine("Project Id:");
                var userInput = Console.ReadLine();
                if (!int.TryParse(userInput, out int selectedProjectId))
                {
                    Console.WriteLine("Please insert a valid Project Id.");
                    Environment.Exit(1);
                }

                var selectedProject = gitLabProjects.FirstOrDefault(p => p.Id == selectedProjectId);
                if (selectedProject == null)
                {
                    Console.WriteLine("Please insert a valid Project Id.");
                    Environment.Exit(1);
                }

                return selectedProject;
            }
        }

        public string CloneProjectRepository(Project project, string gitRepoPath)
        {
            gitRepoPath = Path.Combine(gitRepoPath, project.Path);
            if (Directory.Exists(gitRepoPath))
            {
                var files = Directory.GetFiles(gitRepoPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(gitRepoPath, true);
            }

            using (var repo = new Repository(Repository.Init(gitRepoPath, true)))
            {
                if (repo.Network.Remotes.Any(r => r.Name == "gitlab"))
                    repo.Network.Remotes.Remove("gitlab");

                var remote = repo.Network.Remotes.Add("gitlab", project.HttpUrlToRepo, "+refs/*:refs/*");
                repo.Config.Set("remote.gitlab.mirror", true);

                var fetchOptions = new FetchOptions
                {
                    CredentialsProvider = (url, fromUrl, types) => new UsernamePasswordCredentials
                    {
                        Username = _gitLabSettings.AccessToken,
                        Password = _gitLabSettings.AccessToken
                    }
                };

                var logMessage = string.Empty;
                Commands.Fetch(repo, "gitlab", new List<string>(), fetchOptions, logMessage);
            }

            return gitRepoPath;
        }

        public async Task<ICollection<string>> GetUsernames(Project project)
        {
            var users = await _gitLabClient.Projects.GetUsersAsync(project.Id);
            return users.Select(u => u.Username).ToList();
        }

        public async Task<ICollection<TransferObjects.Milestone>> GetMilestones(Project project)
        {
            var gitlabMilestones = await _gitLabClient.Projects.GetMilestonesAsync(project.Id, o => o.State = MilestoneState.All);
            return gitlabMilestones.Select(gms => new TransferObjects.Milestone
            {
                SourceId = gms.Id,
                Title = gms.Title,
                Description = gms.Description,
                Closed = gms.State == MilestoneState.Closed
            }).ToList();
        }

        public async Task<ICollection<TransferObjects.Issue>> GetIssues(Project project)
        {
            var issues = new List<TransferObjects.Issue>();

            var gitlabIssues = await _gitLabClient.Issues.GetAsync(project.Id.ToString(), o => o.State = IssueState.All);
            foreach (var gitlabIssue in gitlabIssues)
            {
                var issue = new TransferObjects.Issue
                {
                    Id = gitlabIssue.Id,
                    Title = gitlabIssue.Title,
                    Description = gitlabIssue.Description,
                    AuthorUserName = gitlabIssue.Author.Username,
                    CreatedAt = gitlabIssue.CreatedAt,
                    MilestoneId = gitlabIssue.Milestone?.Id,
                    Closed = gitlabIssue.State == IssueState.Closed,
                };
                gitlabIssue.Labels.ForEach(issue.Labels.Add);
                gitlabIssue.Assignees.ForEach(a => issue.AssigneeUserNames.Add(a.Username));

                var issueNotes = await _gitLabClient.Issues.GetNotesAsync(project.Id, gitlabIssue.Iid);
                foreach (var issueNote in issueNotes)
                {
                    issue.Comments.Add(new TransferObjects.Comment
                    {
                        Id = issueNote.Id,
                        CreatedDate = issueNote.CreatedAt,
                        AuthorName = issueNote.Author.Name,
                        AuthorUsername = issueNote.Author.Username,
                        Body = issueNote.Body,
                        System = issueNote.System
                    });
                }

                issues.Add(issue);
            }

            return issues;
        }
    }
}