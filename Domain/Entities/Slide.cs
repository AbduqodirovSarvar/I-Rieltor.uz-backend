using Domain.Behaviours;
using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Sayt bosh sahifasidagi slayder (karusel) elementi.
    /// </summary>
    public class Slide : AudiTableEntity
    {
        /// <summary>
        /// Slayddagi asosiy sarlavha.
        /// </summary>
        [Searchable]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Sarlavha ostidagi kichik sarlavha.
        /// </summary>
        [Searchable]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// Slayddagi asosiy matn.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Slayd fon rasmining Id'si (FK). Har bir slaydning o'z rasmi bor (1:1).
        /// </summary>
        public Guid UploadedFileId { get; set; }

        /// <summary>
        /// Slayd fon rasmi (navigatsiya).
        /// </summary>
        public UploadedFile? UploadedFile { get; set; } = null;

        /// <summary>
        /// Slayderdagi tartib raqami: qiymati kichik slayd oldin ko'rsatiladi.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Slayd ekranda turadigan vaqt, millisekundlarda (standart 3000, ya'ni 3 soniya).
        /// </summary>
        public int DurationMs { get; set; } = 3000;
    }
}
