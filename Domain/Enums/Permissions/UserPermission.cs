using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Permissions
{
    public enum UserPermission
    {
        NONE,
        GET_USER,
        CREATE_USER,
        UPDATE_USER,
        DELETE_USER,
    }
}
