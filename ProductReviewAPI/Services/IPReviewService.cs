using Microsoft.AspNetCore.Mvc;
using ProductReviewAPI.Models;

namespace ProductReviewAPI.Services
{
    public interface IPReviewService
    {
        Task<ActionResult<string>> GetPredict([FromBody] Reviews comment);
    }
}
