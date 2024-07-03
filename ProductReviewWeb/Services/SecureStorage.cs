namespace ProductReviewWeb.Services
{
    public class SecureStorage : ISecureStorage
    {
        private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();

        public Task SetItemAsync(string key, string value)
        {
            _storage[key] = value;
            return Task.CompletedTask;
        }

        public Task<string> GetItemAsync(string key)
        {
            _storage.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task RemoveItemAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
    }

}
