using System;
using System.IO;
using System.Threading.Tasks;
using GitLabApiClient;
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

            var gitLabClient = new GitLabClient(gitLabSettings.Url, gitLabSettings.AccessToken);
            var migration = new Migration(gitLabClient);
            await migration.MigrateOneProject();
        }
    }
}
