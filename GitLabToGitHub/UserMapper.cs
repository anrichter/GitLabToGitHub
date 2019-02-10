using System.Collections.Generic;

namespace GitLabToGitHub
{
    internal class UserMapper
    {
        public Dictionary<string, string> UserMappings { get; set; }

        public string MapToGitHubUserName(string gitlabUserName)
        {
            return UserMappings.ContainsKey(gitlabUserName) ? UserMappings[gitlabUserName] : gitlabUserName;
        }
    }
}