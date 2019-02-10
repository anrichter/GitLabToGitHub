using System;
using System.Collections.Generic;

namespace GitLabToGitHub.TransferObjects
{
    internal class Issue
    {
        public Issue()
        {
            AssigneeUserNames = new List<string>();
            Labels = new List<string>();
            Comments = new List<Comment>();
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AuthorUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Closed { get; set; }
        public ICollection<string> AssigneeUserNames { get; set; }
        public ICollection<string> Labels { get; set; }
        public int? MilestoneId { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}