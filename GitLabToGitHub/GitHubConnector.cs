using Octokit;

namespace GitLabToGitHub
{
    internal class GitHubConnector
    {
        private readonly GitHubClient _gitHubClient;

        public GitHubConnector(GitHubSettings gitHubSettings)
        {
            _gitHubClient = new GitHubClient(new ProductHeaderValue("GitLabToGitHub"))
            {
                Credentials = new Credentials(gitHubSettings.AccessToken)
            };
        }
    }
}