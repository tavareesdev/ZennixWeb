/**
    * ChatController
    *
    * Controlador responsável pelo gerenciamento do chat de suporte técnico empresarial e abertura automática de chamados.
    * 
    * Funcionalidades:
    * - Receber mensagens do usuário e classificar se estão relacionadas a suporte técnico ou não.
    * - Interagir com o modelo GPT para gerar respostas técnicas baseadas em documentação do sistema.
    * - Verificar fluxo de confirmação S/N para saber se a solução funcionou.
    * - Permitir abertura automática de chamados caso a solução não seja suficiente.
    * - Manter estado de interação por usuário (tentativas, respostas e fluxo de escolha de setor).
    *
    * Dependências:
    * - IHttpClientFactory para requisições HTTP à API do OpenAI.
    * - IConfiguration para acessar a chave de API do OpenAI.
    * - ApplicationDbContext para manipulação de dados de chamados, usuários, setores e histórico.
    * - IWebHostEnvironment para localizar arquivos de documentação do sistema.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PIM.Models;
using PIM.Helpers;

namespace PIM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private static readonly List<object> Historico = new()
        {
            new { role = "system", content = "Você é um assistente técnico que responde apenas sobre o sistema Windows 11 Pro em ThinkPad E14 Gen 2 e suporte técnico empresarial. Tente resolver o problema do usuário de forma simples. Caso não consiga, o chamado será aberto automaticamente." }
        };

        private static readonly Dictionary<int, int> TentativasUsuarios = new();
        private static readonly Dictionary<int, bool> AguardandoSetor = new();
        private static readonly Dictionary<int, string> MensagemOriginal = new();
        private static readonly Dictionary<int, string> RespostaGPT = new();
        private static readonly Dictionary<int, bool> AguardandoConfirmacao = new();

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
            _env = env;
        }

        /**
            * EnviarMensagem
            *
            * Recebe uma mensagem do usuário, valida o contexto e retorna uma resposta adequada.
            *
            * Tipo de retorno: IActionResult
            * - Retorna Ok(ResultadoChat) com a resposta do GPT ou mensagens de erro/alerta.
            *
            * Funcionamento detalhado:
            * 1. Verifica se a API Key do OpenAI está configurada.
            * 2. Inicializa o contador de tentativas do usuário, se necessário.
            * 3. Verifica se o usuário está em modo de confirmação S/N; trata respostas "S" e "N".
            * 4. Verifica se o usuário está escolhendo um setor para abertura de chamado.
            * 5. Valida a mensagem com GPT para identificar se é válida para suporte técnico.
            * 6. Se válida, consulta arquivo de documentação do sistema e gera resposta com GPT.
            * 7. Armazena mensagem original e resposta do GPT, ativando modo de confirmação.
            * 
            * Parâmetros:
            * - ChatRequest req: objeto contendo Mensagem e UsuarioId.
            *
            * Dependências:
            * - OpenAI API para geração de respostas automáticas.
        */

        [HttpPost("mensagem")]
        public async Task<IActionResult> EnviarMensagem([FromBody] ChatRequest req)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("API Key não configurada.");

            if (!TentativasUsuarios.ContainsKey(req.UsuarioId))
                TentativasUsuarios[req.UsuarioId] = 0;

            string msgUsuario = req.Mensagem.Trim();

            // 1 - Se usuário deve responder apenas S/N
            if (AguardandoConfirmacao.ContainsKey(req.UsuarioId) && AguardandoConfirmacao[req.UsuarioId])
            {
                string resposta = msgUsuario.ToUpper();
                if (resposta == "S")
                {
                    AguardandoConfirmacao[req.UsuarioId] = false;
                    return Ok(new ResultadoChat
                    {
                        resposta = "Que ótimo! Fico feliz em ajudar, qualquer coisa é só chamar!",
                        abrirChamado = false
                    });
                }
                else if (resposta == "N")
                {
                    AguardandoConfirmacao[req.UsuarioId] = false;
                    return await SolicitarEscolhaSetor(req.UsuarioId);
                }
                else
                {
                    return Ok(new ResultadoChat
                    {
                        resposta = "Por favor, digite apenas 'S' para sim, e 'N' para não.",
                        abrirChamado = false
                    });
                }
            }

            // 2 - Se usuário está escolhendo setor (NÃO deve passar pela validação do GPT aqui)
            if (AguardandoSetor.ContainsKey(req.UsuarioId) && AguardandoSetor[req.UsuarioId])
            {
                if (int.TryParse(msgUsuario, out int setorId))
                {
                    return await AbrirChamadoComSetor(req, setorId, "O usuário informou que a solução não funcionou.");
                }
                else
                {
                    return Ok(new ResultadoChat
                    {
                        resposta = "Você deve digitar apenas o **ID do setor** para abrir o chamado.",
                        abrirChamado = false
                    });
                }
            }

            // 3 - Verificação com GPT se a mensagem é válida para suporte
            bool valido = await EhMensagemValidaComGPT(msgUsuario);
            if (!valido)
            {
                return Ok(new ResultadoChat
                {
                    resposta = "Este canal é exclusivo para questões empresariais e suporte técnico.",
                    abrirChamado = false
                });
            }

            // 4 - fluxo normal com GPT
            var filePath = Path.Combine(_env.WebRootPath, "uploads", "sistema", "documentacao_sistema.txt");
            if (!System.IO.File.Exists(filePath))
                return StatusCode(500, $"Arquivo de documentação não encontrado: {filePath}");

            var docTexto = await System.IO.File.ReadAllTextAsync(filePath);
            var prompt = $@"
                Você é um assistente técnico.  
                Se a pergunta for sobre o sistema da empresa, utilize APENAS a documentação abaixo para responder:

                DOCUMENTAÇÃO DO SISTEMA:
                {docTexto}

                Se a pergunta NÃO estiver relacionada ao sistema da empresa, responda somente sobre Windows 11 Pro em notebooks ThinkPad E14 Gen 2 ou sobre suporte técnico empresarial.  
                Tente sempre resolver o problema do usuário de forma simples.  

                Caso não seja possível resolver a questão, avise o usuário que um chamado será aberto automaticamente.

                Pergunta do usuário: {msgUsuario}
            ";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var payloadNormal = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
                    new { role = "system", content = "Você é um assistente técnico especializado." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            var contentNormal = new StringContent(JsonSerializer.Serialize(payloadNormal), Encoding.UTF8, "application/json");
            var respostaChatNormal = await client.PostAsync("https://api.openai.com/v1/chat/completions", contentNormal);
            var resultadoNormal = await respostaChatNormal.Content.ReadAsStringAsync();

            if (!respostaChatNormal.IsSuccessStatusCode)
                return StatusCode((int)respostaChatNormal.StatusCode, resultadoNormal);

            using var docNormal = JsonDocument.Parse(resultadoNormal);
            var respostaNormal = docNormal.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            TentativasUsuarios[req.UsuarioId]++;

            // grava mensagem original e a resposta do GPT para o chamado
            if (!MensagemOriginal.ContainsKey(req.UsuarioId))
                MensagemOriginal[req.UsuarioId] = msgUsuario;

            RespostaGPT[req.UsuarioId] = respostaNormal;

            // ativa modo aguardando confirmacao S/N
            AguardandoConfirmacao[req.UsuarioId] = true;

            return Ok(new ResultadoChat
            {
                resposta = respostaNormal + "\n\nA solução funcionou? Responda apenas com 'S' para sim ou 'N' para não.",
                abrirChamado = false
            });
        }

        /**
            * EhMensagemValidaComGPT
            *
            * Consulta o GPT para classificar se a mensagem do usuário é SUPORTE ou FORA_DO_ESCOPO.
            *
            * Tipo de retorno: bool
            * - true se a classificação for SUPORTE, false caso contrário ou erro na chamada.
            *
            * Funcionamento detalhado:
            * - Cria payload com prompt específico para classificação.
            * - Envia requisição POST à API OpenAI.
            * - Interpreta a resposta do GPT e retorna true se for SUPORTE.
            *
            * Parâmetros:
            * - string mensagem: texto da mensagem do usuário.
            *
            * Dependências:
            * - OpenAI API para classificação de mensagens.
        */
 
        private async Task<bool> EhMensagemValidaComGPT(string mensagem)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = $@"
                Classifique a seguinte mensagem em apenas UMA das categorias:
                - SUPORTE: quando for sobre problemas técnicos, sistemas, redes, Windows, software, login, acesso, empresa.
                - FORA_DO_ESCOPO: quando for sobre saúde, problemas pessoais, relacionamento, assuntos não técnicos ou não empresariais.

                Mensagem: ""{mensagem}""
                Responda apenas com SUPORTE ou FORA_DO_ESCOPO.
            ";

            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new { role = "system", content = "Você é um classificador de mensagens. Responda apenas SUPORTE ou FORA_DO_ESCOPO." },
                    new { role = "user", content = prompt }
                },
                temperature = 0,
                max_tokens = 5
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return false; // fallback: bloquear por segurança

            using var doc = JsonDocument.Parse(result);
            var classificacao = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return classificacao.Trim().ToUpper().Contains("SUPORTE");
        }

        /**
            * SolicitarEscolhaSetor
            *
            * Solicita ao usuário a escolha de um setor para abertura de chamado quando a solução automatizada falha.
            *
            * Tipo de retorno: Task<IActionResult>
            * - Retorna Ok(ResultadoChat) contendo a lista de setores disponíveis e instruções.
            *
            * Funcionamento detalhado:
            * - Consulta setores com supervisores disponíveis.
            * - Seta flag de AguardandoSetor para o usuário.
            * - Retorna mensagem formatada com IDs e descrições dos setores.
            *
            * Parâmetros:
            * - int usuarioId: identificador do usuário que está interagindo.
            *
            * Dependências:
            * - ApplicationDbContext para consultar setores e usuários.
        */

        private async Task<IActionResult> SolicitarEscolhaSetor(int usuarioId)
        {
            var setoresDisponiveis = await _context.Setores
                .Where(s => _context.Usuarios.Any(u => u.ID_Setor == s.Id && u.ID_Cargo == 8))
                .ToListAsync();

            if (!setoresDisponiveis.Any())
            {
                return Ok(new ResultadoChat
                {
                    resposta = "Não há setores com supervisores disponíveis no momento. Não é possível abrir chamado.",
                    abrirChamado = false
                });
            }

            AguardandoSetor[usuarioId] = true;

            var listaSetores = string.Join("\n", setoresDisponiveis.Select(s => $"{s.Id} - {s.Descricao}"));
            return Ok(new ResultadoChat
            {
                resposta = $"O problema não foi resolvido. Escolha o setor digitando o **ID** correspondente:\n\n{listaSetores}",
                abrirChamado = false
            });
        }

        /**
            * AbrirChamadoComSetor
            *
            * Abre um chamado automaticamente com base no setor escolhido pelo usuário.
            *
            * Tipo de retorno: Task<IActionResult>
            * - Retorna Ok(ResultadoChat) informando que o chamado foi criado e dados resumidos do chamado.
            *
            * Funcionamento detalhado:
            * - Recupera usuário logado e identifica supervisor do setor escolhido.
            * - Gera título do chamado usando GPT.
            * - Cria registro de chamado e histórico no banco de dados.
            * - Limpa estados temporários do usuário (mensagem original, resposta GPT, flags).
            *
            * Parâmetros:
            * - ChatRequest req: mensagem original do usuário.
            * - int setorId: ID do setor escolhido para abertura do chamado.
            * - string motivo: descrição do motivo pelo qual o chamado está sendo aberto.
            *
            * Dependências:
            * - ApplicationDbContext para salvar chamado e histórico.
            * - OpenAI API para gerar título do chamado.
        */
 
        private async Task<IActionResult> AbrirChamadoComSetor(ChatRequest req, int setorId, string motivo)
        {
            var usuarioLogado = HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
            var usuarioLogadoId = usuarioLogado.Id;

            var atendenteId = await _context.Usuarios
                .Where(u => u.ID_Setor == setorId && u.ID_Cargo == 8)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (atendenteId == 0)
            {
                return Ok(new ResultadoChat
                {
                    resposta = "Não existe supervisor disponível para o setor escolhido. Tente outro setor.",
                    abrirChamado = false
                });
            }

            string mensagemUsuario = MensagemOriginal.ContainsKey(req.UsuarioId) ? MensagemOriginal[req.UsuarioId] : req.Mensagem;
            string respostaProposta = RespostaGPT.ContainsKey(req.UsuarioId) ? RespostaGPT[req.UsuarioId] : "";

            var tituloChamado = await GerarTituloChamadoGPT(mensagemUsuario, respostaProposta);

            var chamado = new Chamado
            {
                Titulo = tituloChamado,
                Descricao = $"Problema relatado pelo usuário: {mensagemUsuario}\nSolução proposta pelo assistente: {respostaProposta}",
                DataInicio = DateTime.Now,
                Status = "Aberto",
                ID_Solicitante = usuarioLogadoId,
                ID_CriterioPrioridades = 2,
                ID_Atendente = atendenteId,
                PrioridadeId = 3
            };

            _context.Chamados.Add(chamado);
            await _context.SaveChangesAsync();

            int chamadoId = chamado.Id;

            var historicoChamado = new HistoricoChamado
            {
                ID_Chamado = chamadoId,
                ID_Usuario = 20,
                AcaoTomada = "Chamado aberto pela IA.",
                Data = DateTime.Now
            };

            _context.HistoricoChamado.Add(historicoChamado);
            await _context.SaveChangesAsync();

            // limpa estados
            AguardandoSetor[req.UsuarioId] = false;
            MensagemOriginal.Remove(req.UsuarioId);
            RespostaGPT.Remove(req.UsuarioId);

            return Ok(new ResultadoChat
            {
                resposta = "Um chamado foi aberto automaticamente com base no setor escolhido.",
                abrirChamado = true,
                dadosChamado = new ChamadoResumo
                {
                    titulo = chamado.Titulo,
                    prioridade = "media",
                    descricao = chamado.Descricao
                }
            });
        }

        /**
            * GerarTituloChamadoGPT
            *
            * Gera um título curto e descritivo para um chamado técnico usando GPT.
            *
            * Tipo de retorno: Task<string>
            * - Retorna título gerado pelo GPT ou, em caso de erro, uma versão truncada da mensagem do usuário.
            *
            * Funcionamento detalhado:
            * - Cria payload de requisição com prompt específico para gerar título.
            * - Envia requisição à API OpenAI.
            * - Recebe resposta e extrai conteúdo do título.
            * - Limita o tamanho do título a 50 caracteres.
            *
            * Parâmetros:
            * - string mensagemUsuario: descrição do problema relatado pelo usuário.
            * - string respostaProposta: solução proposta pelo assistente.
            *
            * Dependências:
            * - OpenAI API para geração de título.
        */
 
        private async Task<string> GerarTituloChamadoGPT(string mensagemUsuario, string respostaProposta)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = $"Crie um título curto e descritivo para um chamado técnico com base neste problema e solução:\n" +
                         $"Problema do usuário: \"{mensagemUsuario}\"\n" +
                         $"Solução proposta: \"{respostaProposta}\"";

            var payload = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
                    new { role = "system", content = "Você é um assistente técnico que cria títulos curtos e claros para chamados." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 50
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return mensagemUsuario.Length > 50 ? mensagemUsuario.Substring(0, 50) + "..." : mensagemUsuario;

            using var doc = JsonDocument.Parse(result);
            var titulo = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return titulo.Length > 50 ? titulo.Substring(0, 50) + "..." : titulo;
        }
    }

    /**
        * ChatRequest
        *
        * Classe modelo utilizada para envio de mensagens via API.
        *
        * Propriedades:
        * - string Mensagem: texto enviado pelo usuário.
        * - int UsuarioId: identificador do usuário que enviou a mensagem.
    */
    public class ChatRequest
    {
        public string Mensagem { get; set; }
        public int UsuarioId { get; set; }
    }

    /**
        * ChamadoResumo
        *
        * Classe modelo que contém informações resumidas de um chamado.
        *
        * Propriedades:
        * - string titulo: título do chamado.
        * - string prioridade: prioridade do chamado (ex.: baixa, média, alta).
        * - string descricao: descrição detalhada do chamado.
    */

    public class ChamadoResumo
    {
        public string titulo { get; set; }
        public string prioridade { get; set; }
        public string descricao { get; set; }
    }

    /**
        * ResultadoChat
        *
        * Classe modelo para retorno das respostas do chat.
        *
        * Propriedades:
        * - string resposta: texto da resposta que será exibido ao usuário.
        * - bool abrirChamado: indica se um chamado foi aberto automaticamente.
        * - ChamadoResumo? dadosChamado: informações resumidas do chamado, se criado.
    */
 
    public class ResultadoChat
    {
        public string resposta { get; set; }
        public bool abrirChamado { get; set; }
        public ChamadoResumo? dadosChamado { get; set; }
    }
}
