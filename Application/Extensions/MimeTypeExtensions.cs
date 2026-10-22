using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extensions
{
    public static class MimeTypeExtensions
    {
        public static string ToMimeString(this FileMimeType mimeType)
        {
            return mimeType switch
            {
                FileMimeType.ImagePng => "image/png",
                FileMimeType.ImageJpeg => "image/jpeg",
                FileMimeType.ImageGif => "image/gif",
                FileMimeType.ApplicationPdf => "application/pdf",
                FileMimeType.ApplicationDocx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileMimeType.ApplicationXlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileMimeType.VideoMp4 => "video/mp4",
                FileMimeType.AudioMp3 => "audio/mpeg",
                FileMimeType.Unknown or _ => "application/octet-stream"
            };
        }

        public static FileMimeType ToFileMimeType(this string mimeTypeString)
        {
            return mimeTypeString?.ToLowerInvariant() switch
            {
                "image/png" => FileMimeType.ImagePng,
                "image/jpeg" or "image/jpg" => FileMimeType.ImageJpeg,
                "image/gif" => FileMimeType.ImageGif,
                "application/pdf" => FileMimeType.ApplicationPdf,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => FileMimeType.ApplicationDocx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => FileMimeType.ApplicationXlsx,
                "video/mp4" => FileMimeType.VideoMp4,
                "audio/mpeg" or "audio/mp3" => FileMimeType.AudioMp3,
                _ => FileMimeType.Unknown
            };
        }
    }
}
