using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitLabToGitHub
{
    internal class Migration
    {
        private readonly GitLabConnector _gitLabConnector;
        private readonly GitHubConnector _gitHubConnector;

        public Migration(GitLabConnector gitLabConnector, GitHubConnector gitHubConnector)
        {
            _gitLabConnector = gitLabConnector;
            _gitHubConnector = gitHubConnector;
        }

        public async Task MigrateOneProject()
        {
            var logMessages = new StringBuilder();
            Console.WriteLine("Migrate a GitLab project to a new GitHub repository.");

            var sourceProject = await _gitLabConnector.GetSourceProject();
            var targetNewRepository = _gitHubConnector.GetNewRepository(sourceProject.Namespace.Name, sourceProject.Name, sourceProject.Description);

            if (!SelectStartMigration(sourceProject.NameWithNamespace, targetNewRepository.Name, targetNewRepository.Private ?? false))
            {
                return;
            }

            Console.Write("Create new GitHub Repository... ");
            var targetRepository = await _gitHubConnector.CreateRepository(targetNewRepository);
            Console.WriteLine("Done.");

            Console.Write("Migrate Git Repository... ");
            var gitPath = Path.Combine(Directory.GetCurrentDirectory(), "GitClones");
            var gitRepoPath = _gitLabConnector.CloneProjectRepository(sourceProject, gitPath);
            _gitHubConnector.PushGitRepo(targetRepository, gitRepoPath);
            Console.WriteLine("Done.");

            Console.Write("Migrate Users... ");
            var users = await _gitLabConnector.GetUsernames(sourceProject);
            Console.Write($"\rMigrate {users.Count} Users... ");
            await _gitHubConnector.CreateCollaborators(targetRepository, users, logMessages);
            Console.WriteLine("Done.");

            if (sourceProject.IssuesEnabled)
            {
                Console.Write("Migrate Milestones... ");
                var milestones = await _gitLabConnector.GetMilestones(sourceProject);
                Console.Write($"\rMigrate {milestones.Count} Milestones... ");
                milestones = await _gitHubConnector.CreateMilestones(targetRepository, milestones);
                Console.WriteLine("Done.");

                Console.Write("Migrate Issues... ");
                var issues = await _gitLabConnector.GetIssues(sourceProject);
                Console.Write($"\rMigrate {issues.Count} Issues... ");
                await _gitHubConnector.CreateIssues(targetRepository, issues, milestones, logMessages);
                Console.WriteLine("Done.");
            }

            Console.WriteLine($"{Environment.NewLine}Log Messages (You have to manual rework):");
            Console.Write(logMessages.ToString());
            Console.WriteLine("Search in open and closed Issues for >Image< and edit to upload images manually.");
            Console.WriteLine();

            Console.WriteLine("Migration finshed.");
            Console.WriteLine($"GitLab: {sourceProject.WebUrl}");
            Console.WriteLine($"GitHub: {targetRepository.HtmlUrl}");
        }

        private bool SelectStartMigration(string sourceProjectName, string targetRepositoryName, bool targetPrivate)
        {
            var privatePublic = targetPrivate ? "private" : "public";
            Console.Write($"Migrate >{sourceProjectName}< from GitLab to new {privatePublic} Project >{targetRepositoryName}< on GitHub? [Y/n]");
            var allowedKeys = new[] {ConsoleKey.Y, ConsoleKey.N, ConsoleKey.Enter};
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));
            Console.WriteLine(string.Empty);

            return userInputKeyInfo.Key != ConsoleKey.N;
        }
    }
}