using System;
using System.IO;
using System.Linq;
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
            var sourceProject = await _gitLabConnector.GetSourceProject();
            var targetNewRepository = _gitHubConnector.GetNewRepository(sourceProject.Namespace.Name, sourceProject.Name);

            if (!SelectStartMigration(sourceProject.NameWithNamespace, targetNewRepository.Name, targetNewRepository.Private ?? false))
            {
                return;
            }

            var targetRepository = await _gitHubConnector.CreateRepository(targetNewRepository);

            var gitPath = Path.Combine(Directory.GetCurrentDirectory(), "GitClones");
            var gitRepoPath = _gitLabConnector.CloneProjectRepository(sourceProject, gitPath);
            _gitHubConnector.PushGitRepo(targetRepository, gitRepoPath);

            Console.Write("Migrate Milestones... ");
            var milestones = await _gitLabConnector.GetMilestones(sourceProject);
            Console.Write($"{milestones.Count} Milestones ");
            milestones = await _gitHubConnector.CreateMilestones(targetRepository, milestones);
            Console.WriteLine("migrated.");

            Console.Write("Migrate Issues... ");
            var issues = await _gitLabConnector.GetIssues(sourceProject);
            Console.Write($"{issues.Count} Issues ");
            await _gitHubConnector.CreateIssues(targetRepository, issues, milestones);
            Console.WriteLine("migrated");

            Console.WriteLine("Migration finshed.");
            Console.WriteLine($"GitLab: {sourceProject.WebUrl}");
            Console.WriteLine($"GitHub: {targetRepository.HtmlUrl}");
        }

        private bool SelectStartMigration(string sourceProjectName, string targetRepositoryName, bool targetPrivate)
        {
            var privatePublic = targetPrivate ? "private" : "public";
            Console.Write($"Migrate >{sourceProjectName}< from GitLab to new {privatePublic} Project >{targetRepositoryName}< on GitHub? [Y/n]");
            var allowedKeys = new[] {ConsoleKey.Y, ConsoleKey.N};
            ConsoleKeyInfo userInputKeyInfo;
            do
            {
                userInputKeyInfo = Console.ReadKey(false);
            } while (!allowedKeys.Contains(userInputKeyInfo.Key));
            Console.WriteLine(string.Empty);

            return userInputKeyInfo.Key == ConsoleKey.Y;
        }
    }
}