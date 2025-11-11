namespace ArtistTool.Domain
{
    public interface IPhotoDatabase
    {
        Task AddCategoryAsync(Category category);
        Task AddPhotographAsync(Photograph photograph);
        Task UpdatePhotographAsync(Photograph photograph);
        Task<Photograph?> GetPhotographWithIdAsync(string id);
        Task<IEnumerable<Category>> ListCategoriesAsync();
        Task<IEnumerable<Tag>> ListTagsAsync();
        Task<IEnumerable<string>> ListPhotoIdsAsync();
        Task InitAsync(CancellationToken cancellationToken);
    }
}