using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabApiClient;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;

namespace GitLabToGitHub
{
    internal class Migration
    {
        private readonly GitLabClient _gitLabClient;

        public Migration(GitLabClient gitLabClient)
        {
            _gitLabClient = gitLabClient;
        }

        public async Task MigrateOneProject()
        {
            try
            {
                var availableGitLabGroups = await _gitLabClient.Groups.GetAsync();
                var sourceGitLabGroup = SelectGitLabGroup(availableGitLabGroups);
                var availableProjectsInSelectedGroup = await _gitLabClient.Groups.GetProjectsAsync(sourceGitLabGroup.Id.ToString());
                var sourceProject = SelectGitLabProject(availableProjectsInSelectedGroup);

                Console.WriteLine($"Migrate >{sourceProject.NameWithNamespace}< from GitLab to GitHub...");
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
    }
}