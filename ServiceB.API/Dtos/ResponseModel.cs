using Microsoft.AspNetCore.Mvc;

namespace ServiceB.API.Dtos
{
    public class ResponseModel<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public ProblemDetails Error { get; set; }
    }
}
