using Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Abstractions
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator _mediator = mediator;
    }
}
