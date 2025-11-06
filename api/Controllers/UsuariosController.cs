using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ZennixApi.Data;
using ZennixApi.Models;

namespace ZennixApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(AppDbContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // GET api/usuarios
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new { u.Id, u.Nome, u.Email })
                .ToListAsync();

            return Ok(usuarios);
        }

        // Função auxiliar para gerar o hash MD5
        private string ToMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        // POST api/usuarios/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("===== LOGIN REQUEST RECEBIDO =====");
            _logger.LogInformation($"Email: {request.Email}");
            _logger.LogInformation($"Senha (original): {request.Senha}");

            var senhaHash = ToMD5(request.Senha);
            _logger.LogInformation($"Senha (MD5): {senhaHash}");

            try
            {
                var usuario = await _context.UsuarioLoginResults
                    .FromSqlInterpolated($@"
                        SELECT u.ID, u.Nome, u.Email, u.Senha, u.ID_Setor, u.Status
                        FROM Usuarios u
                        WHERE u.Email = {request.Email} AND u.Senha = {senhaHash}")
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (usuario != null)
                {
                    Console.WriteLine($"Usuário encontrado: {usuario.Nome}");

                    var setor = await _context.Setores
                        .Where(s => s.Id == usuario.ID_Setor)
                        .Select(s => s.Descricao)
                        .FirstOrDefaultAsync();

                    var setoresDisponiveis = await _context.Setores
                        .FromSqlRaw("select * from Setores s where Id in (select distinct ID_SETOR from Usuarios where ID_Cargo in(8,9,10))")
                        .ToListAsync();

                    return Ok(new
                    {
                        usuario.Id,
                        usuario.Nome,
                        usuario.Email,
                        usuario.ID_Setor,
                        Setor = setor,
                        usuario.Status,
                        SetoresDisponiveis = setoresDisponiveis
                    });
                }

                Console.WriteLine("Nenhum usuário encontrado!");
                return Unauthorized(new { mensagem = "E-mail ou senha inválidos" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ ERRO NO LOGIN:");
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { mensagem = "Erro interno no servidor" });
            }
        }
    }
}
