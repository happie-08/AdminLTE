using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AdminLTE.Helpers
{
    public static class ImageHelper
    {
        public static async Task<string> SaveImageAsync(IFormFile file, IWebHostEnvironment env, string folder = "uploads")
        {
            if (file == null || file.Length == 0)
                return "default.png";

            var uploadsPath = Path.Combine(env.WebRootPath, folder);

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName;
        }

        public static void DeleteImage(string fileName, IWebHostEnvironment env, string folder = "uploads")
        {
            if (!string.IsNullOrEmpty(fileName) && fileName != "default.png")
            {
                var uploadsPath = Path.Combine(env.WebRootPath, folder);
                var filePath = Path.Combine(uploadsPath, fileName);

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
    }
}
