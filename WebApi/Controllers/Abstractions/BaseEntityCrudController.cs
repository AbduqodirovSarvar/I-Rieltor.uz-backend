using Application.Models.Dtos.Abstractions;
using Application.Models.Requests.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Abstractions
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public abstract class BaseEntityCrudController<TCreateCommand, 
                                          TUpdateCommand,
                                          TDeleteCommand,
                                          TGetQuery,
                                          TGetListQuery>(IMediator mediator) 
        : BaseController(mediator)
        where TCreateCommand : BaseCreateDto
        where TUpdateCommand : BaseUpdateDto
        where TDeleteCommand : BaseDeleteDto
        where TGetQuery : BaseGetOneQuery
        where TGetListQuery : BaseGetListQuery
    {
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromForm] TCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut]
        public virtual async Task<IActionResult> Put([FromForm] TUpdateCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete]
        public virtual async Task<IActionResult> Delete(TDeleteCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        // Reads take the request token: a client that hung up stops the query. Writes do not (they always finish,
        // so a dropped connection never leaves half an operation).
        public virtual async Task<IActionResult> Get([FromQuery] TGetQuery request)
        {
            var result = await _mediator.Send(request, HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet("list")]
        public virtual async Task<IActionResult> GetAll([FromQuery] TGetListQuery request)
        {
            var result = await _mediator.Send(request, HttpContext.RequestAborted);
            return Ok(result);
        }
    }
}
