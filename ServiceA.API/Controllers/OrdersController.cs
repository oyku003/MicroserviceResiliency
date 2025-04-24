using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceA.API.Dtos;
using ServiceA.API.Services;

namespace ServiceA.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(StockService stockService) : ControllerBase
    {
        [ProducesResponseType<GetStockResponseModel>(statusCode:200)]
        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            return Ok(await stockService.GetStockCount(productId: 2));
        }
    }
}
