using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Users.Commands.AssignRole
{
    /// <summary>
    /// Gives a user a role. Needs ASSIGN_USERROLE and passes <c>IUserAccessGuard</c>:
    /// nobody can hand out a role with more rights than their own, or change their own role.
    /// </summary>
    public record AssignUserRoleCommand : ICommand<ResponseModel<UserViewModel>>
    {
        [Required]
        public Guid UserId { get; init; }

        [Required]
        public Guid UserRoleId { get; init; }
    }

    public class AssignUserRoleCommandHandler(IUserService userService)
        : ICommandHandler<AssignUserRoleCommand, ResponseModel<UserViewModel>>
    {
        private readonly IUserService _userService = userService;
        public async Task<ResponseModel<UserViewModel>> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
        {
            return await _userService.AssignRoleAsync(request.UserId, request.UserRoleId, cancellationToken);
        }
    }
}
