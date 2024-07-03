namespace ProductReviewWeb.Services
{
    public interface ISecureStorage
    {
        Task SetItemAsync(string key, string value);
        Task<string> GetItemAsync(string key);
        Task RemoveItemAsync(string key);
    }
}
