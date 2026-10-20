using Application.Features.Users.Commands.AssignRole;
using Application.Features.Users.Commands.Create;
using Application.Features.Users.Commands.Delete;
using Application.Features.Users.Commands.Update;
using Application.Features.Users.Queries.GetList;
using Application.Features.Users.Queries.GetOne;
using Domain.Enums.Actions;
using Domain.Enums.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IMediator mediator) 
        : BaseEntityCrudController<CreateUserCommand,
                                   UpdateUserCommand,
                                   DeleteUserCommand,
                                   GetOneUserQuery,
                                   GetUserListQuery>(mediator)
    {
        [Authorize(Policy = nameof(UserActions.CREATE_USER))]
        [HttpPost]
        public override async Task<IActionResult> Post([FromForm] CreateUserCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(UserActions.UPDATE_USER))]
        [HttpPut]
        public override async Task<IActionResult> Put([FromForm] UpdateUserCommand command)
        {
            return await base.Put(command);
        }

        [Authorize(Policy = nameof(UserActions.DELETE_USER))]
        [HttpDelete]
        public override async Task<IActionResult> Delete(DeleteUserCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(UserActions.READ_USER))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneUserQuery request)
        {
            return await base.Get(request);
        }

        /// <summary>
        /// Gives a user a role; refused for a role with more rights than the caller's, and for the caller's own role.
        /// </summary>
        [Authorize(Policy = nameof(UserRoleActions.ASSIGN_USERROLE))]
        [HttpPut("role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignUserRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [Authorize(Policy = nameof(UserActions.READ_USER))]
        [HttpGet("list")]
        public override async Task<IActionResult> GetAll([FromQuery] GetUserListQuery request)
        {
            return await base.GetAll(request);
        }
    }
}
