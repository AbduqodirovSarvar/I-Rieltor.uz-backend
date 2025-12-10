using Application.Models.Dtos.Users;
using Application.Models.Requests.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IMediator mediator) 
        : BaseEntityCrudController<CreateUserDto,
                                   UpdateUserDto,
                                   DeleteUserDto,
                                   BaseGetOneQuery,
                                   BaseGetListQuery>(mediator)
    {
    }
}
