using Application.Features.Slides.Commands.Create;
using Application.Features.Slides.Commands.Delete;
using Application.Features.Slides.Commands.Update;
using Application.Features.Slides.Queries.GetList;
using Application.Features.Slides.Queries.GetOne;
using Application.Features.UserRoles.Commands.Create;
using Application.Features.UserRoles.Commands.Delete;
using Application.Features.UserRoles.Commands.Update;
using Application.Features.UserRoles.Queries.GetList;
using Application.Features.UserRoles.Queries.GetOne;
using Application.Models.Requests.Abstractions;
using Domain.Enums.Actions;
using Domain.Enums.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlideController(IMediator mediator) 
        : BaseEntityCrudController<CreateSlideCommand, 
                                  UpdateSlideCommand, 
                                  DeleteSlideCommand, 
                                  GetOneSlideQuery, 
                                  GetSlideListQuery>(mediator)
    {
        [Authorize(Policy = nameof(SlideActions.CREATE_SLIDE))]
        [HttpPost]
        public override async Task<IActionResult> Post([FromForm] CreateSlideCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(SlideActions.UPDATE_SLIDE))]
        [HttpPut]
        public override async Task<IActionResult> Put([FromForm] UpdateSlideCommand command)
        {
            return await base.Put(command);
        }

        [Authorize(Policy = nameof(SlideActions.DELETE_SLIDE))]
        [HttpDelete]
        public override async Task<IActionResult> Delete(DeleteSlideCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(SlideActions.READ_SLIDE))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneSlideQuery request)
        {
            return await base.Get(request);
        }

        [Authorize(Policy = nameof(SlideActions.READ_SLIDE))]
        [HttpGet("list")]
        public override async Task<IActionResult> GetAll([FromQuery] GetSlideListQuery request)
        {
            return await base.GetAll(request);
        }
    }
}
