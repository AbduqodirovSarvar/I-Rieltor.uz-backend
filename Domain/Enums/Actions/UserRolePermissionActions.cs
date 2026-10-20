using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Actions
{
    /// <summary>
    /// UserRolePermission uchun ruxsat nomlari (CRUD). Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum UserRolePermissionActions
    {
        CREATE_USERROLEPERMISSION,
        READ_USERROLEPERMISSION,
        UPDATE_USERROLEPERMISSION,
        DELETE_USERROLEPERMISSION,
    }
}
