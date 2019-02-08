using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;
using LibGit2Sharp;

namespace GitLabToGitHub
{
    internal class GitLabConnector
    {
        private readonly GitLabSettings _gitLabSettings;
        private readonly GitLabClient _gitLabClient;

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
                var sourceGroup = SelectGitLabGroup(availableSourceGroups);
                var availableProjectsInSourceGroup = await _gitLabClient.Groups.GetProjectsAsync(sourceGroup.Id.ToString());
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
        }

        private Group SelectGitLabGroup(IList<Group> gitLabGroups)
        {
            Console.WriteLine("Select Source GitLab Group:");
            Console.WriteLine("ID\tName");
            foreach (var gitLabGroup in gitLabGroups)
            {
                Console.WriteLine($"{gitLabGroup.Id}\t{gitLabGroup.Name}");
            }

            Console.WriteLine("Select a Group: ");
            var userInput = Console.ReadLine();
            if (!int.TryParse(userInput, out int selectedGroupId))
            {
                Console.WriteLine("Please insert an correct Id");
                return SelectGitLabGroup(gitLabGroups);
            }

            var selectedGroup = gitLabGroups.FirstOrDefault(g => g.Id == selectedGroupId);
            if (selectedGroup == null)
            {
                Console.WriteLine("Please insert a valid Group Id");
                return SelectGitLabGroup(gitLabGroups);
            }

            return selectedGroup;
        }

        private Project SelectGitLabProject(IList<Project> gitLabProjects)
        {
            Console.WriteLine("Select Source GitLab Project:");
            Console.WriteLine("ID\tName");
            foreach (var gitLabProject in gitLabProjects)
            {
                Console.WriteLine($"{gitLabProject.Id}\t{gitLabProject.Name}");
            }

            Console.WriteLine("Select a Project:");
            var userInput = Console.ReadLine();
            if (!int.TryParse(userInput, out int selectedProjectId))
            {
                Console.WriteLine("Please insert an correct Id");
                return SelectGitLabProject(gitLabProjects);
            }

            var selectedProject = gitLabProjects.FirstOrDefault(p => p.Id == selectedProjectId);
            if (selectedProject == null)
            {
                Console.WriteLine("Please insert a valid Project Id");
                return SelectGitLabProject(gitLabProjects);
            }

            return selectedProject;
        }

        public string CloneProjectRepository(Project sourceProject, string gitRepoPath)
        {
            gitRepoPath = Path.Combine(gitRepoPath, sourceProject.Path);
            Console.Write($"Clone Git Repository to >{gitRepoPath}<... ");

            using (var repo = new Repository(Repository.Init(gitRepoPath, true)))
            {
                var remote = repo.Network.Remotes.Add("gitlab", sourceProject.HttpUrlToRepo, "+refs/*:refs/*");
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

            Console.WriteLine("Done.");
            return gitRepoPath;
        }
    }
}