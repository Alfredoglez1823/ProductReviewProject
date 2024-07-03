using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductReviewAPI.Services;
using ProductReviewAPI.Models;

namespace ProductReviewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PReviewController : ControllerBase, IPReviewService
    {

        [HttpPost("ReviewPredict")]
        public async Task<ActionResult<string>> GetPredict([FromBody] Reviews comment)
        {
            string? result = await UseModel(comment.comment);
            if (result != null)
                return Ok(result);
            else
                return BadRequest("The prediction failed");
        }


        private async Task<string> UseModel(string comment)
        {
            //Load sample data
            var sampleData = new ProductReviewModel.ModelInput()
            {
                Summary = comment,
            };

            //Load model and predict output
            var result = ProductReviewModel.Predict(sampleData);


            if (result.PredictedLabel != null)
                return result.PredictedLabel;
            else
                return null;
        }
    }

}
