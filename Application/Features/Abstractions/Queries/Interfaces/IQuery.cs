using Application.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Abstractions.Queries.Interfaces
{
    public interface IQuery<out TResponse> 
        : IRequest<TResponse>
    {
    }
}
