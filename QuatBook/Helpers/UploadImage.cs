namespace QuatBook.Helpers
{
    public class UploadImage
    {
        public static string UploadHinh(IFormFile Hinh, string folder)
        {
            try
            {
                // Create directory if it doesn't exist
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", folder);
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate unique filename to avoid conflicts
                string uniqueFileName = Path.GetFileNameWithoutExtension(Hinh.FileName)
                    + "_" + Guid.NewGuid().ToString().Substring(0, 8)
                    + Path.GetExtension(Hinh.FileName);

                var fullPath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    Hinh.CopyTo(stream);
                }

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                // Log the exception message
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return string.Empty;
            }
        }

        public static byte[] ConvertToByteArray(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            using (var memoryStream = new MemoryStream())
            {
                imageFile.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static async Task<byte[]> ConvertToByteArrayAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
