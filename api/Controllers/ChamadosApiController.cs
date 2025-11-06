using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using ZennixApi.Models;
using ZennixApi.Data;

namespace ZennixApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChamadosApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public ChamadosApiController(AppDbContext context)
        {
            _context = context;

            // 🔹 HttpClient configurado para aceitar certificados HTTPS autoassinados
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _httpClient = new HttpClient(handler);
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirChamadoApi([FromForm] AbrirChamadoRequest request, List<IFormFile>? anexos)
        {
            if (string.IsNullOrWhiteSpace(request.SetorSolicitadoNome))
                return BadRequest("O nome do setor solicitado não pode ser vazio.");

            var usuario = await _context.Usuarios.FindAsync(request.IdUsuario);
            if (usuario == null)
                return BadRequest("Usuário não encontrado.");

            // 🔍 Normaliza o nome do setor para comparação
            string setorNome = request.SetorSolicitadoNome.Trim().ToLower();

            var setor = await _context.Setores
                .FirstOrDefaultAsync(s =>
                    !string.IsNullOrEmpty(s.Descricao) &&
                    s.Descricao.Trim().ToLower() == setorNome
                );

            if (setor == null)
                return BadRequest($"Setor solicitado '{request.SetorSolicitadoNome}' não encontrado.");

            // 🔍 Busca supervisor com prioridade de cargos: 8 > 9 > 10
            int supervisorId = 0;
            int[] prioridades = { 8, 9, 10 };

            foreach (var cargoId in prioridades)
            {
                supervisorId = await _context.Usuarios
                    .Where(u => u.ID_Setor == setor.Id && u.ID_Cargo == cargoId)
                    .OrderBy(u => u.Id)
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (supervisorId != 0)
                    break;
            }

            if (supervisorId == 0)
                supervisorId = usuario.Id;

            // 🧾 Cria o chamado
            var chamado = new Chamado
            {
                Titulo = request.Titulo ?? "(Sem título)",
                Descricao = request.Descricao ?? "",
                DataInicio = DateTime.Now,
                Status = "Aberto",
                ID_Solicitante = usuario.Id,
                ID_CriterioPrioridades = 1,
                ID_Atendente = supervisorId
            };

            _context.Chamados.Add(chamado);
            await _context.SaveChangesAsync();

            int chamadoId = chamado.Id;

            // 📎 Envia anexos para o sistema Web e salva metadados no banco
            if (anexos != null && anexos.Any())
            {
                bool sucesso = await EnviarAnexosParaWebAsync(anexos);
                if (!sucesso)
                    return StatusCode(500, "Falha ao enviar anexos para o sistema Web.");

                foreach (var file in anexos)
                {
                    _context.Anexos.Add(new Anexo
                    {
                        NomeArquivo = file.FileName,
                        CaminhoArquivo = $"/uploads/{file.FileName}", // URL base do Web
                        Formato = Path.GetExtension(file.FileName),
                        ID_Chamado = chamado.Id,
                        ID_Usuario = usuario.Id,
                        Data = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }

            // 🕓 Adiciona histórico do chamado
            _context.HistoricoChamado.Add(new HistoricoChamado
            {
                Data = DateTime.Now,
                AcaoTomada = "Chamado aberto pelo usuário.",
                ID_Usuario = usuario.Id,
                ID_Chamado = chamadoId
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Chamado aberto com sucesso!",
                chamadoId = chamado.Id,
                supervisorId,
                setorId = setor.Id,
                setorNome = setor.Descricao
            });
        }

        // 🔹 Método para enviar anexos ao sistema Web via HTTPS
        private async Task<bool> EnviarAnexosParaWebAsync(List<IFormFile> anexos)
        {
            try
            {
                var urlWeb = "http://localhost:7082/api/anexos/upload"; // Endpoint do Web
                var form = new MultipartFormDataContent();

                foreach (var file in anexos)
                {
                    var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    ms.Position = 0;

                    var fileContent = new ByteArrayContent(ms.ToArray());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                    form.Add(fileContent, "anexos", file.FileName);
                }

                var response = await _httpClient.PostAsync(urlWeb, form);

                if (!response.IsSuccessStatusCode)
                {
                    var respText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Erro no envio de anexo: {respText}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Falha no upload dos anexos: {ex.Message}");
                return false;
            }
        }
    }

    // 🔹 Modelo do request recebido no endpoint
    public class AbrirChamadoRequest
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? SetorSolicitadoNome { get; set; }
        public int IdUsuario { get; set; }
    }
}
