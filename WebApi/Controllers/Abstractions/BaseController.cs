using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Abstractions
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
    }
}
