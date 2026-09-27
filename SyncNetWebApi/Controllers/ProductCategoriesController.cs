using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductCategories;
using SyncNetApi.Services.ProductCategories;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    [Route("api/v1/product/master/categories")]
    public class ProductCategoriesController : PermissionGatedControllerBase
    {
        private readonly IProductCategoryService _categories;

        public ProductCategoriesController(IProductCategoryService categories, IUserService users) : base(users)
        {
            _categories = categories;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductCategoryDto>>> GetAll([FromQuery] string? filter)
            => Ok(await _categories.GetRecordsAsync(filter));

        [HttpGet("{category}")]
        public async Task<ActionResult<ProductCategoryDto>> GetById(string category)
        {
            var item = await _categories.GetByIdAsync(category);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<ProductCategoryDto>> Create(CreateProductCategoryRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product categories.");

            try
            {
                var created = await _categories.CreateAsync(request, CurrentUser);
                return CreatedAtAction(nameof(GetById), new { category = created.Category }, created);
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Product category already exists", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
        }

        [HttpPut("{category}")]
        public async Task<ActionResult<ProductCategoryDto>> Update(string category, UpdateProductCategoryRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product categories.");

            try
            {
                return Ok(await _categories.UpdateAsync(category, request, CurrentUser));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product category not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }

        [HttpDelete("{category}")]
        public async Task<IActionResult> Delete(string category)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product categories.");

            try
            {
                await _categories.DeleteAsync(category, CurrentUser);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Product category not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
        }
    }
}
