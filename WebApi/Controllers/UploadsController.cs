using Application.Features.UploadedFiles.Queries.GetOne;
using Application.Models.Dtos.Abstractions;
using Application.Models.Requests.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UploadsController(IMediator mediator) 
        : BaseEntityCrudController<BaseCreateDto,
                                   BaseUpdateDto,
                                   BaseDeleteDto,
                                   GetOneUploadedFileQuery,
                                   BaseGetListQuery>(mediator)
    {
        [HttpPost]
        public override Task<IActionResult> Post([FromForm] BaseCreateDto command)
        {
            throw new NotImplementedException();
        }

        [HttpPut]
        public override Task<IActionResult> Put([FromForm] BaseUpdateDto command)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        public override Task<IActionResult> Delete(BaseDeleteDto command)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneUploadedFileQuery request)
        {
            if (request == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(request);
            return Ok(result);
        }

        [HttpGet("list")]
        public override Task<IActionResult> GetAll([FromQuery] BaseGetListQuery request)
        {
            throw new NotImplementedException();
        }
    }
}
