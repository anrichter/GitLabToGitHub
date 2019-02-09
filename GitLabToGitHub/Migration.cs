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
        }

        private bool SelectStartMigration(string sourceProjectName, string targetRepositoryName, bool targetPrivate)
        {
            var privatePublic = targetPrivate ? "private" : "public";
            Console.WriteLine($"Migrate >{sourceProjectName}< from GitLab to new {privatePublic} Project >{targetRepositoryName}< on GitHub? [Y/n]");
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