using Application.Models.Dtos.UploadedFiles;
using Application.Models.ViewModels.UploadedFiles;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class UploadedFileMappingProfile : Profile
    {
        public UploadedFileMappingProfile() 
        {
            CreateMap<UploadedFile, UploadedFileViewModel>()
                .ForMember(d => d.Url, o => o.MapFrom<FileUrlResolver, string>(s => s.Path));
            CreateMap<UpdateUploadedFileDto, UploadedFile>().ReverseMap();
        }
    }
}
