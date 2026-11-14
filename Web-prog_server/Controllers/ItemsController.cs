using Microsoft.AspNetCore.Mvc;
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
                : NotFound(result.Error);
        }

        [HttpPost]
        public async Task<ActionResult<ItemResponse>> CreateItem(
            CreateItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _itemService.CreateAsync(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
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
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken cancellationToken)
        {
            var result = await _itemService.DeleteAsync(id, cancellationToken);
            return result.IsSuccess
                ? NoContent()
                : NotFound(result.Error);
        }
    }
}
