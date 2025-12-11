using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Queries.ConfirmationCode
{
    public record GetConfirmationCodeQuery
        : IQuery<ResponseModel<bool>>
    {
    }
}
