﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GitLabToGitHub
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.production.json", optional: true)
                .Build();

            var gitLabSettings = new GitLabSettings();
            config.GetSection("GitLab").Bind(gitLabSettings);
            var gitLabConnector = new GitLabConnector(gitLabSettings);

            var gitHubSettings = new GitHubSettings();
            config.GetSection("GitHub").Bind(gitHubSettings);
            var gitHubConnector = new GitHubConnector(gitHubSettings);

            var migration = new Migration(gitLabConnector, gitHubConnector);
            await migration.MigrateOneProject();
        }
    }
}
