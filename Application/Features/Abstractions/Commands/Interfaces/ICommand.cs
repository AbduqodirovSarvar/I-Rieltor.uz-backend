using Application.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Abstractions.Commands.Interfaces
{
    public interface ICommand<out TResponse> 
        : IRequest<TResponse>
    {
    }
}
