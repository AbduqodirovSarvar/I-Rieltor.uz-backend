using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Commands.Create
{
    public class CreateSlideCommandHandler(ISlideService slideService)
        : ICommandHandler<CreateSlideCommand, ResponseModel<SlideViewModel>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<SlideViewModel>> Handle(CreateSlideCommand request, CancellationToken cancellationToken)
        {
            return await _slideService.CreateAsync(request, cancellationToken);
        }
    }
}
