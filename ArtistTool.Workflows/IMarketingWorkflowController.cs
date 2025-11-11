namespace ArtistTool.Workflows
{
    public interface IMarketingWorkflowController
    {
        Task<MarketingWorkflowContext> GetOrStartMarketingWorkflowAsync(string photoId);
    }
}
