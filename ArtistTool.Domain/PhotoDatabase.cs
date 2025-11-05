namespace ArtistTool.Domain
{
    public class PhotoDatabase : IPhotoDatabase
    {
        private readonly List<Photograph> Photos = [];

        private readonly List<Category> Categories = [
            new Category
            {
                Name = "All",
                Description = "All of your photographs."
            }];

        private readonly List<Tag> Tags = [];

        private static Tag Clone(Tag tag) =>
            new()
            {
                TagName = tag.TagName,
                Photographs = [.. tag.Photographs]
            };

        private static Category Clone(Category category) =>
        new()
        {
            Name = category.Name,
            Description = category.Description,
            Photographs = [.. category.Photographs]
        };

        public Task<Photograph?> GetPhotographWithIdAsync(string id)
        {
            var photo = Photos.FirstOrDefault(p => p.Id == id);
            if (photo is null)
            {
                return Task.FromResult<Photograph?>(default);
            }
            return Task.FromResult<Photograph?>(photo.Clone());
        }

        public Task<IEnumerable<Category>> ListCategoriesAsync() =>
            Task.FromResult<IEnumerable<Category>>([..Categories
                .Select(Clone)
                .OrderBy(c => c.Name)]);

        public Task<IEnumerable<Tag>> ListTagsAsync() =>
            Task.FromResult<IEnumerable<Tag>>([..Tags
                .Select(Clone)
                .OrderBy(t => t.TagName)]);

        public Task AddCategoryAsync(Category category)
        {
            var cat = Clone(category);
            var existing = Categories.FirstOrDefault(c => c.Name == cat.Name);
            if (existing is null)
            {
                Categories.Add(cat);
            }
            else
            {
                existing.Description = cat.Description;
                existing.Photographs = [.. cat.Photographs];
            }

            return Task.CompletedTask;
        }

        public Task AddPhotographAsync(Photograph photograph)
        {
            var photo = photograph.Clone();
            photo.Categories = ["All", .. photo.Categories];

            Photos.Add(photo);

            foreach (var category in photo.Categories)
            {
                var categoryInstance = Categories.FirstOrDefault(c => c.Name == category);
                if (categoryInstance is null)
                {
                    categoryInstance = new Category
                    {
                        Name = category,
                        Description = $"Collection of photographs in the '{category}' category."
                    };
                    Categories.Add(categoryInstance);
                }
                if (!categoryInstance.Photographs.Contains(photo.Id))
                {
                    categoryInstance.Photographs.Add(photo.Id);
                }
            }

            foreach (var tag in photo.Tags)
            {
                var tagInstance = Tags.FirstOrDefault(t => t.TagName == tag);
                if (tagInstance is null)
                {
                    tagInstance = new Tag
                    {
                        TagName = tag
                    };
                    Tags.Add(tagInstance);
                }
                if (!tagInstance.Photographs.Contains(photo.Id))
                {
                    tagInstance.Photographs.Add(photo.Id);
                }
            }
            return Task.CompletedTask;
        }

        public Task UpdatePhotographAsync(Photograph photograph)
        {
            var existing = Photos.FirstOrDefault(p => p.Id == photograph.Id);
            if (existing is null)
            {
                throw new InvalidOperationException($"Photograph with id {photograph.Id} not found.");
            }

            // Remove from old categories
            foreach (var category in Categories)
            {
                category.Photographs.RemoveAll(id => id == photograph.Id);
            }

            // Remove from old tags
            foreach (var tag in Tags)
            {
                tag.Photographs.RemoveAll(id => id == photograph.Id);
            }

            // Remove old photo
            Photos.Remove(existing);

            // Add updated photo (reuses AddPhotographAsync logic)
            return AddPhotographAsync(photograph);
        }

        public Task<IEnumerable<string>> ListPhotoIdsAsync() => Task.FromResult<IEnumerable<string>>([.. Photos.Select(p => p.Id)]);
    }
}
