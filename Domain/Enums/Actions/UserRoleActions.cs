using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Actions
{
    /// <summary>
    /// UserRole uchun ruxsat nomlari (CRUD). Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum UserRoleActions
    {
        CREATE_USERROLE,
        READ_USERROLE,
        UPDATE_USERROLE,
        DELETE_USERROLE,
        /// <summary>Foydalanuvchiga rol berish (PUT api/User/role).</summary>
        ASSIGN_USERROLE,
    }
}
