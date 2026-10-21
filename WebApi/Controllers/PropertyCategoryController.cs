using Application.Features.PropertyCategories;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyCategoryController(IMediator mediator)
        : BaseEntityCrudController<CreatePropertyCategoryCommand,
                                   UpdatePropertyCategoryCommand,
                                   DeletePropertyCategoryCommand,
                                   GetOnePropertyCategoryQuery,
                                   GetPropertyCategoryListQuery>(mediator)
    {
        [Authorize(Policy = nameof(PropertyCategoryActions.CREATE_PROPERTYCATEGORY))]
        [HttpPost]
        public override Task<IActionResult> Post([FromForm] CreatePropertyCategoryCommand command) => base.Post(command);

        [Authorize(Policy = nameof(PropertyCategoryActions.UPDATE_PROPERTYCATEGORY))]
        [HttpPut]
        public override Task<IActionResult> Put([FromForm] UpdatePropertyCategoryCommand command) => base.Put(command);

        /// <summary>Refused (409) while subcategories or properties use it.</summary>
        [Authorize(Policy = nameof(PropertyCategoryActions.DELETE_PROPERTYCATEGORY))]
        [HttpDelete]
        public override Task<IActionResult> Delete(DeletePropertyCategoryCommand command) => base.Delete(command);

        [Authorize(Policy = nameof(PropertyCategoryActions.READ_PROPERTYCATEGORY))]
        [HttpGet]
        public override Task<IActionResult> Get([FromQuery] GetOnePropertyCategoryQuery request) => base.Get(request);

        [Authorize(Policy = nameof(PropertyCategoryActions.READ_PROPERTYCATEGORY))]
        [HttpGet("list")]
        public override Task<IActionResult> GetAll([FromQuery] GetPropertyCategoryListQuery request) => base.GetAll(request);

        /// <summary>
        /// Every category in display order (with ParentCategoryId, so the frontend can build the tree), for catalog filters.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> GetPublic()
            => Ok(await _mediator.Send(new GetPublicPropertyCategoryListQuery(), HttpContext.RequestAborted));
    }
}
