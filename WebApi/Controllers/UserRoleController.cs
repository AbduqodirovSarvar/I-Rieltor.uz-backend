using Application.Features.UserRoles.Commands.Create;
using Application.Features.UserRoles.Commands.Delete;
using Application.Features.UserRoles.Commands.Update;
using Application.Features.UserRoles.Queries.GetList;
using Application.Features.UserRoles.Queries.GetOne;
using Domain.Enums.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRoleController(IMediator mediator)
        : BaseEntityCrudController<CreateUserRoleCommand,
                                   UpdateUserRoleCommand,
                                   DeleteUserRoleCommand,
                                   GetOneUserRoleQuery,
                                   GetUserRoleListQuery>(mediator)
    {
        [Authorize(Policy = nameof(UserRoleCrudPermissionEnum.CREATE_USERROLE))]
        [HttpPost]
        public override async Task<IActionResult> Post([FromForm] CreateUserRoleCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(UserRoleCrudPermissionEnum.UPDATE_USERROLE))]
        [HttpPut]
        public override async Task<IActionResult> Put([FromForm] UpdateUserRoleCommand command)
        {
            return await base.Put(command);
        }

        [Authorize(Policy = nameof(UserRoleCrudPermissionEnum.DELETE_USERROLE))]
        [HttpDelete]
        public override async Task<IActionResult> Delete(DeleteUserRoleCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(UserRoleCrudPermissionEnum.READ_USERROLE))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneUserRoleQuery request)
        {
            return await base.Get(request);
        }

        [Authorize(Policy = nameof(UserRoleCrudPermissionEnum.READ_USERROLE))]
        [HttpGet("list")]
        public override async Task<IActionResult> GetAll([FromQuery] GetUserRoleListQuery request)
        {
            return await base.GetAll(request);
        }
    }
}
