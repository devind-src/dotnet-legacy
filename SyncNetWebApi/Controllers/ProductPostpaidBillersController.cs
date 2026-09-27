using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyncNetApi.Common;
using SyncNetApi.Dtos.ProductPostpaidBillers;
using SyncNetApi.Services.ProductPostpaidBillers;
using SyncNetApi.Services.Users;

namespace SyncNetApi.Controllers
{
    /// <summary>Detail biller di Product &gt; Fees &gt; Product Fees (fase 1): primary + alternate,
    /// sharing fee, dan LB weight per produk bill payment &amp; purchase.</summary>
    [Route("api/v1/product/pricing-fees/postpaid-billers")]
    public class ProductPostpaidBillersController : PermissionGatedControllerBase
    {
        private readonly IProductPostpaidBillerService _billers;

        public ProductPostpaidBillersController(IProductPostpaidBillerService billers, IUserService users) : base(users)
        {
            _billers = billers;
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<ProductPostpaidBillersDto>> GetByProduct(string productId)
            => Ok(await _billers.GetByProductAsync(productId));

        [HttpPost]
        public async Task<ActionResult<ProductPostpaidBillersDto>> Create(CreateProductPostpaidBillerRequest request)
        {
            if (!await CanAsync(p => p.CanAdd))
                return Forbidden("You do not have permission to add product billers.");

            return await Run(() => _billers.CreateAsync(request, CurrentUser));
        }

        [HttpPut("{productId}/{nodeId:int}")]
        public async Task<ActionResult<ProductPostpaidBillersDto>> Update(string productId, int nodeId, UpdateProductPostpaidBillerRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product billers.");

            return await Run(() => _billers.UpdateAsync(productId, nodeId, request, CurrentUser));
        }

        [HttpPut("{productId}/weights")]
        public async Task<ActionResult<ProductPostpaidBillersDto>> UpdateWeights(string productId, UpdateProductPostpaidBillerWeightsRequest request)
        {
            if (!await CanAsync(p => p.CanEdit))
                return Forbidden("You do not have permission to edit product billers.");

            return await Run(() => _billers.UpdateWeightsAsync(productId, request, CurrentUser));
        }

        [HttpDelete("{productId}/{nodeId:int}")]
        public async Task<ActionResult<ProductPostpaidBillersDto>> Delete(string productId, int nodeId)
        {
            if (!await CanAsync(p => p.CanDelete))
                return Forbidden("You do not have permission to delete product billers.");

            return await Run(() => _billers.DeleteAsync(productId, nodeId, CurrentUser));
        }

        private async Task<ActionResult<ProductPostpaidBillersDto>> Run(System.Func<Task<ProductPostpaidBillersDto>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ProblemDetails { Title = "Not found", Detail = ex.Message, Status = StatusCodes.Status404NotFound });
            }
            catch (ConflictException ex)
            {
                return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid operation", Detail = ex.Message, Status = StatusCodes.Status400BadRequest });
            }
        }
    }
}
