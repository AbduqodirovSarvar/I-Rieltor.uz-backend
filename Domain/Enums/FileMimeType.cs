using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum FileMimeType
    {
        Unknown,           // application/octet-stream
        ImagePng,          // image/png
        ImageJpeg,         // image/jpeg
        ImageGif,          // image/gif
        ApplicationPdf,    // application/pdf
        ApplicationDocx,   // application/vnd.openxmlformats-officedocument.wordprocessingml.document
        ApplicationXlsx,   // application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
        VideoMp4,          // video/mp4
        AudioMp3           // audio/mpeg
    }
}
