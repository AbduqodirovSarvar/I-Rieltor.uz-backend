using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;
using System;
using System.Collections.Generic;


using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Services.Repositories
{
    public class UploadedFileRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<UploadedFile>(context, redisService, unitOfWork), IUploadedFileRepository
    {
    }
}
