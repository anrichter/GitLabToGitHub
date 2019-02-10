using System;

namespace GitLabToGitHub.TransferObjects
{
    internal class Comment
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string AuthorUsername { get; set; }
        public string Body { get; set; }
    }
}