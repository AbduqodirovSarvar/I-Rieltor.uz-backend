using Application.Features.Company.Commands.Create;
using Application.Features.Company.Commands.Delete;
using Application.Features.Company.Commands.Update;
using Application.Features.Slides.Commands.Create;
using Application.Features.Slides.Commands.Delete;
using Application.Features.Slides.Commands.Update;
using Application.Features.Slides.Queries.GetList;
using Application.Features.Slides.Queries.GetOne;
using Application.Models.Requests.Abstractions;
using Domain.Enums.Actions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController(IMediator mediator) 
        : BaseEntityCrudController<CreateCompanyCommand, 
                                   UpdateCompanyCommand,
                                   DeleteCompanyCommand,
                                   GetOneSlideQuery,
                                   BaseGetListQuery>(mediator)
    {
        [Authorize(Policy = nameof(CompanyActions.CREATE_COMPANY))]
        [HttpPost]
        public override async Task<IActionResult> Post([FromForm] CreateCompanyCommand command)
        {
            return await base.Post(command);
        }

        [Authorize(Policy = nameof(CompanyActions.UPDATE_COMPANY))]
        [HttpPut]
        public override async Task<IActionResult> Put([FromForm] UpdateCompanyCommand command)
        {
            return await base.Put(command);
        }

        [Authorize(Policy = nameof(CompanyActions.DELETE_COMPANY))]
        [HttpDelete]
        public override async Task<IActionResult> Delete(DeleteCompanyCommand command)
        {
            return await base.Delete(command);
        }

        [Authorize(Policy = nameof(CompanyActions.READ_COMPANY))]
        [HttpGet]
        public override async Task<IActionResult> Get([FromQuery] GetOneSlideQuery request)
        {
            return await base.Get(request);
        }

        [Authorize(Policy = nameof(CompanyActions.READ_COMPANY))]
        [HttpGet("list")]
        public override Task<IActionResult> GetAll([FromQuery] BaseGetListQuery request)
        {
            throw new NotImplementedException();
        }
    }
}
