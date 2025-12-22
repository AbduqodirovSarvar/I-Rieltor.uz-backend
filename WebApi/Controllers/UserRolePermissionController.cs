using Application.Features.UserRolePermissions.Commands.Create;
using Application.Features.UserRolePermissions.Commands.Delete;
using Application.Features.UserRolePermissions.Queries.GetList;
using Application.Features.UserRolePermissions.Queries.GetOne;
using Application.Interfaces.Auths;
using Application.Models.Dtos.UserRolePermissions;
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
    public class UserRolePermissionController(IMediator mediator, IPermissionService permissionService) 
        : BaseEntityCrudController<CreateUserRolePermissionCommand,
                                   UpdateUserRolePermissionDto,
                                   DeleteUserRolePermissionCommand,
                                   GetOneUserRolePermissionQuery,
                                   GetUserRolePermissionListQuery>(mediator)
    {
        private readonly IPermissionService _permissionService = permissionService;

        [Authorize(Policy = nameof(UserRolePermissionActions.CREATE_USERROLEPERMISSION))]
        [HttpPost]
        public override async Task<IActionResult> Post([FromForm] CreateUserRolePermissionCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(UserRolePermissionActions.UPDATE_USERROLEPERMISSION))]
        [HttpPut]
        public override async Task<IActionResult> Put([FromForm] UpdateUserRolePermissionDto command)
        {
            await Task.CompletedTask;
            return BadRequest("Not implemented yet.");
        }

        [Authorize(Policy = nameof(UserRolePermissionActions.DELETE_USERROLEPERMISSION))]
        [HttpDelete]
        public override async Task<IActionResult> Delete(DeleteUserRolePermissionCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(UserRolePermissionActions.READ_USERROLEPERMISSION))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneUserRolePermissionQuery request)
        {
            return await base.Get(request);
        }

        [Authorize(Policy = nameof(UserRolePermissionActions.READ_USERROLEPERMISSION))]
        [HttpGet("list")]
        public override async Task<IActionResult> GetAll([FromQuery] GetUserRolePermissionListQuery request)
        {
            return await base.GetAll(request);
        }

        [Authorize(Policy = nameof(UserRolePermissionActions.READ_USERROLEPERMISSION))]
        [HttpGet("all-permissions")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var result = await _permissionService.GetAllPermission();
            return Ok(result);
        }
    }
}
