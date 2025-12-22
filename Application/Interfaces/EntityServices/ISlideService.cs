using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Slides;
using Application.Models.ViewModels.Slides;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface ISlideService
        : IGenericEntityService<Slide,
                                CreateSlideDto,
                                UpdateSlideDto,
                                SlideViewModel>
    {
    }
}
