namespace GitLabToGitHub.TransferObjects
{
    internal class Milestone
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Closed { get; set; }
    }
}