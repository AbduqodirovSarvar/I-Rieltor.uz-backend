using Application.Features.Slides.Commands.Create;
using Application.Features.Slides.Commands.Delete;
using Application.Features.Slides.Commands.Update;
using Application.Features.Slides.Queries.GetList;
using Application.Features.Slides.Queries.GetOne;
using Application.Features.Slides.Queries.GetPublicList;
using Domain.Enums.Actions;
using Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Public;
using Application.Models.ViewModels.Slides;

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
        [ProducesResponseType<ResponseModel<SlideViewModel>>(200)]
        public override async Task<IActionResult> Post([FromForm] CreateSlideCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(SlideActions.UPDATE_SLIDE))]
        [HttpPut]
        [ProducesResponseType<ResponseModel<SlideViewModel>>(200)]
        public override async Task<IActionResult> Put([FromForm] UpdateSlideCommand command)
        {
            return await base.Put(command);
        }

        [Authorize(Policy = nameof(SlideActions.DELETE_SLIDE))]
        [HttpDelete]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public override async Task<IActionResult> Delete(DeleteSlideCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(SlideActions.READ_SLIDE))]
        [HttpGet]
        [ProducesResponseType<ResponseModel<SlideViewModel>>(200)]
        public override async Task<IActionResult> Get([FromQuery] GetOneSlideQuery request)
        {
            return await base.Get(request);
        }

        [Authorize(Policy = nameof(SlideActions.READ_SLIDE))]
        [HttpGet("list")]
        [ProducesResponseType<ResponseModel<List<SlideViewModel>>>(200)]
        public override async Task<IActionResult> GetAll([FromQuery] GetSlideListQuery request)
        {
            return await base.GetAll(request);
        }

        /// <summary>
        /// Home page carousel: every slide with its image URL, in display order.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("public")]
        [ProducesResponseType<ResponseModel<List<PublicSlideViewModel>>>(200)]
        public async Task<IActionResult> GetPublic()
        {
            var result = await _mediator.Send(new GetPublicSlideListQuery(), HttpContext.RequestAborted);
            return Ok(result);
        }
    }
}
