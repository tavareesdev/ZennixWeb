using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZennixWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnexosController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAnexos([FromForm] List<IFormFile> anexos)
        {
            if (anexos == null || anexos.Count == 0)
                return BadRequest("Nenhum arquivo enviado.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var file in anexos)
            {
                var uniqueName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var path = Path.Combine(uploadsFolder, uniqueName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            return Ok(new { message = "Arquivos enviados com sucesso!" });
        }
    }
}
