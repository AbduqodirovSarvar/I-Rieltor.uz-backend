using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Actions
{
    /// <summary>
    /// Company uchun ruxsat nomlari (CRUD). Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum CompanyActions
    {
        CREATE_COMPANY,
        READ_COMPANY,
        UPDATE_COMPANY,
        DELETE_COMPANY,
    }
}
