using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.CardProducts;
using SyncNetApi.Services.CardProducts;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/card-products")]
    public class CardProductsController : PermissionGatedControllerBase
    {
        private readonly ICardProductService _products;

        public CardProductsController(ICardProductService products, IUserService users) : base(users)
        {
            _products = products;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CardProductDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _products.GetRecordsAsync(filter));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CardProductDto>> GetById(int id)
        {
            var item = await _products.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CardProductDto>> Create(CreateCardProductRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add products.");

            try
            {
                var created = await _products.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid product", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CardProductDto>> Update(int id, UpdateCardProductRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit products.");

            try
            {
                return Ok(await _products.UpdateAsync(id, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid product", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete products.");

            try
            {
                await _products.DeleteAsync(id, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
