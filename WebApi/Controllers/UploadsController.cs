using Application.Features.UploadedFiles.Queries.GetOne;
using Application.Models.Dtos.Abstractions;
using Application.Models.Requests.Abstractions;
using Domain.Enums.Actions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    /// <summary>
    /// Metadata of uploaded files. The files themselves are served as static files from /uploads,
    /// and they are created, replaced and deleted through the entity that owns them (slide, company).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController(IMediator mediator)
        : BaseEntityCrudController<BaseCreateDto,
                                   BaseUpdateDto,
                                   BaseDeleteDto,
                                   GetOneUploadedFileQuery,
                                   BaseGetListQuery>(mediator)
    {
        [NonAction]
        public override Task<IActionResult> Post([FromForm] BaseCreateDto command)
            => throw new NotSupportedException();

        [NonAction]
        public override Task<IActionResult> Put([FromForm] BaseUpdateDto command)
            => throw new NotSupportedException();

        [NonAction]
        public override Task<IActionResult> Delete(BaseDeleteDto command)
            => throw new NotSupportedException();

        [Authorize(Policy = nameof(UploadedFileActions.READ_UPLOADEDFILE))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneUploadedFileQuery request)
        {
            return await base.Get(request);
        }

        [NonAction]
        public override Task<IActionResult> GetAll([FromQuery] BaseGetListQuery request)
            => throw new NotSupportedException();
    }
}
