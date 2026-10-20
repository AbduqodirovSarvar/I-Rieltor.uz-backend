using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.Actions
{
    /// <summary>
    /// UploadedFile uchun ruxsat nomlari (CRUD). Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum UploadedFileActions
    {
        CREATE_UPLOADEDFILE,
        READ_UPLOADEDFILE,
        UPDATE_UPLOADEDFILE,
        DELETE_UPLOADEDFILE,
    }
}
