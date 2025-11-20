namespace ArtistTool.Workflows
{
    /// <summary>
    /// Represents the type of AI client used for agent interactions
    /// </summary>
    public enum ChatClientType
    {
        /// <summary>
        /// Text-based conversational chat client
        /// </summary>
        Conversational,
        
        /// <summary>
        /// Vision-enabled chat client that can process images
        /// </summary>
        Vision,
        
        /// <summary>
        /// Image generation client
        /// </summary>
        Image
    }
}
