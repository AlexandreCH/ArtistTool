
namespace ArtistTool.Services
{
    public interface IMessageHub
    {
        void Publish<T>(T message);
        Action Subscribe<T>(Action<T> handler);
    }
}