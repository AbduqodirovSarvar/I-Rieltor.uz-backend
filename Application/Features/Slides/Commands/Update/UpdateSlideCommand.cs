using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Slides;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Application.Features.Slides.Commands.Update
{
    public record UpdateSlideCommand
        : UpdateSlideDto,
        ICommand<ResponseModel<SlideViewModel>>
    {
    }
}
