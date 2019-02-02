using System;
using System.Threading.Tasks;
using GitLabApiClient;
using GitLabApiClient.Models.Projects.Requests;

namespace GitLabToGitHub
{
    internal class Migration
    {
        private readonly GitLabClient _gitLabClient;

        public Migration(GitLabClient gitLabClient)
        {
            _gitLabClient = gitLabClient;
        }

        public async Task Start()
        {
            var gitlabProjects = await _gitLabClient.Projects.GetAsync(o => o.Order = ProjectsOrder.LastActivityAt);

            foreach (var gitLabProject in gitlabProjects)
            {
                Console.WriteLine($"{gitLabProject.Namespace.Name}\t{gitLabProject.Name}");
            }
        }
    }
}