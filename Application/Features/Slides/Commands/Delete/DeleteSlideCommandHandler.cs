using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Commands.Delete
{
    public class DeleteSlideCommandHandler(
        ISlideService slideService)
        : ICommandHandler<DeleteSlideCommand, ResponseModel<bool>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<bool>> Handle(DeleteSlideCommand request, CancellationToken cancellationToken)
        {
            return await _slideService.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
