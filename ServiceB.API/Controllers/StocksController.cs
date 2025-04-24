using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceB.API.Dtos;

namespace ServiceB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        //Simple Type:intidouble,string,bool,DateTime (Vlidasyon için Route Constraint)
        //Complex type => Class,record,array,list,dict
        [HttpGet(template:"{productId:int}")]
        public IActionResult Get(int productId)
        {
            var response = new GetStockResponseModel(100);
            var responseModel = new ResponseModel<GetStockResponseModel>();
            responseModel.IsSuccess = true;
            responseModel.Data= response;

            try
            {
                return Ok(responseModel.Data);
            }
            catch (Exception)
            {
                responseModel.Error = new ProblemDetails
                {
                    Title = "Ürün Bulunamadı",
                    Detail="Id'si 5 olan ürün bulunamadı"
                };

                return BadRequest(responseModel.Error);
            }
        
        }
    }
}
