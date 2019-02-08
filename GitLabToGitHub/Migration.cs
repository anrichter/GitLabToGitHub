using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabApiClient;
using Octokit;
using Group = GitLabApiClient.Models.Groups.Responses.Group;
using Project = GitLabApiClient.Models.Projects.Responses.Project;

namespace GitLabToGitHub
{
    internal class Migration
    {
        private readonly GitLabClient _gitLabClient;
        private readonly GitHubClient _gitHubClient;

        public Migration(GitLabClient gitLabClient, GitHubClient gitHubClient)
        {
            _gitLabClient = gitLabClient;
            _gitHubClient = gitHubClient;
        }

        public async Task MigrateOneProject()
        {
            try
            {
                var availableSourceGroups = await _gitLabClient.Groups.GetAsync();
                var sourceGroup = SelectGitLabGroup(availableSourceGroups);
                var availableProjectsInSourceGroup = await _gitLabClient.Groups.GetProjectsAsync(sourceGroup.Id.ToString());
                var sourceProject = SelectGitLabProject(availableProjectsInSourceGroup);
                var targetRepository = SelectNewGitHubRepository(sourceGroup.Name, sourceProject.Name);

                if (!SelectStartMigration(sourceProject, targetRepository))
                {
                    return;
                }

                Console.WriteLine("Start Migration...");

            }
            catch (GitLabException e)
            {
                Console.WriteLine($"There is a problem to reach your GitLab Server. {e.Message}");
                Console.WriteLine("Please check your GitLab Settings.");
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

        private NewRepository SelectNewGitHubRepository(string sourceGroupName, string sourceProjectName)
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

        private bool SelectStartMigration(Project sourceProject, NewRepository targetRepository)
        {
            var privatePublic = targetRepository.Private == true ? "private" : "public";
            Console.WriteLine($"Migrate >{sourceProject.NameWithNamespace}< from GitLab to new {privatePublic} Project {targetRepository.Name} on GitHub? [Y/n]");
            var allowedKeys = new[] {ConsoleKey.Y, ConsoleKey.N};
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));

            return userInputKeyInfo.Key == ConsoleKey.Y;
        }
    }
}