namespace ArtistTool.Domain.Agents
{
    public class ReportSection(int level, string name)
    {
        private readonly Lock _mutex = new();
        public int Level => level;
        public string Name => name; 
        public HtmlSnippetResponse? Snippet { get; set; }
        public List<ReportSection> Children = [];
        public void AddChild(ReportSection section)
        {
            _mutex.Enter();
            try
            {
                Children.Add(section);
            }
            finally
            {
                _mutex.Exit();
            }
        }
    }
}
