using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Actions
{
    /// <summary>
    /// Slide uchun ruxsat nomlari (CRUD). Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum SlideActions
    {
        CREATE_SLIDE,
        READ_SLIDE,
        UPDATE_SLIDE,
        DELETE_SLIDE,
    }
}
