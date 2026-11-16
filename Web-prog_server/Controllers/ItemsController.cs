using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Web_prog_server.Middleware;
using Web_prog_server.Services.Items;

namespace Web_prog_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemsController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ItemResponse>>> GetItems(
            CancellationToken cancellationToken)
        {
            var items = await _itemService.GetAllAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemResponse>> GetItem(
            int id, CancellationToken cancellationToken)
        {
            var result = await _itemService.GetByIdAsync(id, cancellationToken);
            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(WithRequestId(result.Error!));
        }

        [HttpPost]
        [EnableRateLimiting("item-write")]
        public async Task<ActionResult<ItemResponse>> CreateItem(
            CreateItemRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            var result = await _itemService.CreateIdempotentAsync(
                request, idempotencyKey, cancellationToken);
            if (!result.IsSuccess)
            {
                var error = WithRequestId(result.Error!);
                return error.Code == "IDEMPOTENCY_KEY_REUSED"
                    ? Conflict(error)
                    : BadRequest(error);
            }

            return CreatedAtAction(nameof(GetItem), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ItemResponse>> UpdateItem(
            int id, UpdateItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _itemService.UpdateAsync(id, request, cancellationToken);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return result.Error!.Code == "ITEM_NOT_FOUND"
                ? NotFound(WithRequestId(result.Error))
                : BadRequest(WithRequestId(result.Error));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken cancellationToken)
        {
            var result = await _itemService.DeleteAsync(id, cancellationToken);
            return result.IsSuccess
                ? NoContent()
                : NotFound(WithRequestId(result.Error!));
        }

        private ErrorResponse WithRequestId(ErrorResponse error) =>
            error with { RequestId = HttpContext.RequestId() };
    }
}
