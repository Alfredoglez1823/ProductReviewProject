using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductReviewAPI.Models;
using ProductReviewAPI.Services;
using static ProductReviewAPI.Controllers.PReviewController;

namespace ProductReviewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCommentsController : ControllerBase
    {
        private readonly IProductCommentService _productCommentService;
        private readonly IPReviewService _pReviewService;
        private readonly ILogger<ProductCommentsController> _logger;

        public ProductCommentsController(IProductCommentService productCommentService, ILogger<ProductCommentsController> logger, IPReviewService pReviewService)
        {
            _productCommentService = productCommentService;
            _pReviewService = pReviewService;
            _logger = logger;
        }

        // GET: api/ProductComments
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<IEnumerable<ProductComment>>> GetAll()
        {
            var comments = await _productCommentService.GetAllAsync();
            return Ok(comments);
        }

        // GET: api/ProductComments/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult<ProductComment>> GetById(int id)
        {
            var comment = await _productCommentService.GetByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            return Ok(comment);
        }

        // POST: api/ProductComments
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductComment>> Post([FromBody] ProductComment productComment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var actionResult = await _pReviewService.GetPredict(new Reviews { comment = productComment.Comment.ToString() });
                // Verifica si la acción fue exitosa
                if (actionResult.Result is OkObjectResult okResult && okResult.Value is string prediction)
                {
                    productComment.Prediction = prediction;
                }
                else
                {
                    // Manejar el caso en que la predicción falló (BadRequest, etc.)
                    _logger.LogError("Error al obtener la predicción: La respuesta del servicio no fue exitosa.");
                    return StatusCode(500, "Error al procesar la solicitud.");
                }
            }
            catch (Exception ex)
            {
                // Manejar la excepción, tal vez loguear el error y devolver un error 500
                _logger.LogError($"Error al obtener la predicción: {ex.Message}");
                return StatusCode(500, "Error al procesar la solicitud.");
            }

            var createdComment = await _productCommentService.AddAsync(productComment);
            _logger.LogInformation($"ProductComment created: {createdComment.Id}");
            return Ok($"{productComment.Product} created");
        }

        // DELETE: api/ProductComments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if(id == 1)
                return BadRequest("you cannot delete product 1");

            var comment = await _productCommentService.GetByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            await _productCommentService.DeleteAsync(id);
            _logger.LogInformation($"ProductComment deleted: {id}");
            return Ok("removed product");
        }

        
    }
}
