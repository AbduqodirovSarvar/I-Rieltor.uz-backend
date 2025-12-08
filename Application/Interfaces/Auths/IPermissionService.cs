using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface IPermissionService
    {
        Task<bool> Check(string entityName, OperationType operation, User? user = null);
        string Get(string entityName, OperationType operation);
    }
}
