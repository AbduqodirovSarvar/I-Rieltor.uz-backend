using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Commands.Update
{
    public class UpdateSlideCommandHandler(
        ISlideService slideService)
        : ICommandHandler<UpdateSlideCommand, ResponseModel<SlideViewModel>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<SlideViewModel>> Handle(UpdateSlideCommand request, CancellationToken cancellationToken)
        {
            return await _slideService.UpdateAsync(request, cancellationToken);
        }
    }
}
