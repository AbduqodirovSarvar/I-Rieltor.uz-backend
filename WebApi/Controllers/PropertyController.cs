using Application.Features.Properties;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Properties;
using Application.Models.ViewModels.Public;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController(IMediator mediator)
        : BaseEntityCrudController<CreatePropertyCommand,
                                   UpdatePropertyCommand,
                                   DeletePropertyCommand,
                                   GetOnePropertyQuery,
                                   GetPropertyListQuery>(mediator)
    {
        [Authorize(Policy = nameof(PropertyActions.CREATE_PROPERTY))]
        [HttpPost]
        [ProducesResponseType<ResponseModel<PropertyViewModel>>(200)]
        public override Task<IActionResult> Post([FromForm] CreatePropertyCommand command) => base.Post(command);

        [Authorize(Policy = nameof(PropertyActions.UPDATE_PROPERTY))]
        [HttpPut]
        [ProducesResponseType<ResponseModel<PropertyViewModel>>(200)]
        public override Task<IActionResult> Put([FromForm] UpdatePropertyCommand command) => base.Put(command);

        /// <summary>Refused (409) once the property was put up for auction; archive it instead.</summary>
        [Authorize(Policy = nameof(PropertyActions.DELETE_PROPERTY))]
        [HttpDelete]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public override Task<IActionResult> Delete(DeletePropertyCommand command) => base.Delete(command);

        /// <summary>With its media.</summary>
        [Authorize(Policy = nameof(PropertyActions.READ_PROPERTY))]
        [HttpGet]
        [ProducesResponseType<ResponseModel<PropertyViewModel>>(200)]
        public override Task<IActionResult> Get([FromQuery] GetOnePropertyQuery request) => base.Get(request);

        [Authorize(Policy = nameof(PropertyActions.READ_PROPERTY))]
        [HttpGet("list")]
        [ProducesResponseType<ResponseModel<List<PropertyViewModel>>>(200)]
        public override Task<IActionResult> GetAll([FromQuery] GetPropertyListQuery request) => base.GetAll(request);

        /// <summary>Adds an image, video or document (multipart form).</summary>
        [Authorize(Policy = nameof(PropertyActions.UPDATE_PROPERTY))]
        [HttpPost("media")]
        [ProducesResponseType<ResponseModel<PropertyMediaViewModel>>(200)]
        public async Task<IActionResult> AddMedia([FromForm] AddPropertyMediaCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Title, order, cover.</summary>
        [Authorize(Policy = nameof(PropertyActions.UPDATE_PROPERTY))]
        [HttpPut("media")]
        [ProducesResponseType<ResponseModel<PropertyMediaViewModel>>(200)]
        public async Task<IActionResult> UpdateMedia([FromForm] UpdatePropertyMediaCommand command)
            => Ok(await _mediator.Send(command));

        [Authorize(Policy = nameof(PropertyActions.UPDATE_PROPERTY))]
        [HttpDelete("media")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> DeleteMedia(DeletePropertyMediaCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// Public catalog with filters, search, sorting and pages; see PublicPropertyFilter.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("public/list")]
        [ProducesResponseType<ResponseModel<List<PublicPropertyListItemViewModel>>>(200)]
        public async Task<IActionResult> GetPublicList([FromQuery] GetPublicPropertyListQuery request)
            => Ok(await _mediator.Send(request, HttpContext.RequestAborted));

        /// <summary>
        /// One property of the catalog with its media and current auction; counts the view.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("public")]
        [ProducesResponseType<ResponseModel<PublicPropertyDetailViewModel>>(200)]
        public async Task<IActionResult> GetPublic([FromQuery] GetPublicPropertyQuery request)
            => Ok(await _mediator.Send(request, HttpContext.RequestAborted));
    }
}
