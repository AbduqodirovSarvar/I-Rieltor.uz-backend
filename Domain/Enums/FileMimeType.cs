using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    /// <summary>
    /// Yuklangan fayl turi (MIME).
    /// </summary>
    public enum FileMimeType
    {
        /// <summary>
        /// Noma'lum tur (application/octet-stream).
        /// </summary>
        Unknown,
        /// <summary>
        /// PNG rasm (image/png).
        /// </summary>
        ImagePng,
        /// <summary>
        /// JPEG rasm (image/jpeg).
        /// </summary>
        ImageJpeg,
        /// <summary>
        /// GIF rasm (image/gif).
        /// </summary>
        ImageGif,
        /// <summary>
        /// PDF hujjat (application/pdf).
        /// </summary>
        ApplicationPdf,
        /// <summary>
        /// Word hujjat (.docx).
        /// </summary>
        ApplicationDocx,
        /// <summary>
        /// Excel jadval (.xlsx).
        /// </summary>
        ApplicationXlsx,
        /// <summary>
        /// MP4 video (video/mp4).
        /// </summary>
        VideoMp4,
        /// <summary>
        /// MP3 audio (audio/mpeg).
        /// </summary>
        AudioMp3
    }
}
