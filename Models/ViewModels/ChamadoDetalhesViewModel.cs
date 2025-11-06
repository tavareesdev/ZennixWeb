/**
    * ViewModels relacionados a Chamados, Comentários, Histórico e Anexos.
    *
    * ComentarioViewModel:
    * - Representa um comentário feito em um chamado.
    * - Propriedades:
    *   - Id: Identificador do comentário.
    *   - NomeUsuario: Nome do usuário que realizou o comentário.
    *   - IdUsuario: Identificador do usuário que comentou.
    *   - Texto: Conteúdo do comentário.
    *   - Data: Data e hora do comentário.
    *
    * HistoricoViewModel:
    * - Representa um registro histórico de ações realizadas em um chamado.
    * - Propriedades:
    *   - NomeUsuario: Nome do usuário que realizou a ação.
    *   - AcaoTomada: Descrição da ação realizada.
    *   - Data: Data e hora da ação.
    *   - IdUsuario: Identificador do usuário que realizou a ação.
    *
    * AnexoViewModel:
    * - Representa um arquivo anexado a um chamado.
    * - Propriedades:
    *   - Id: Identificador do anexo.
    *   - IdUsuario: Identificador do usuário que anexou o arquivo.
    *   - NomeArquivo: Nome do arquivo.
    *   - CaminhoArquivo: Caminho físico ou virtual do arquivo.
    *   - NomeUsuario: Nome do usuário que anexou.
    *   - DataEnvio: Data e hora do envio do anexo.
    *
    * ChamadoDetalhesViewModel:
    * - Representa todos os detalhes de um chamado, incluindo comentários, histórico, anexos e informações relacionadas.
    * - Propriedades:
    *   - Id, Titulo, Descricao, DataInicio, DataFim, Status
    *   - Solicitante, Responsavel, Setor, SetorSoli
    *   - PrioridadeDescricao, NivelPrioridadeDescricao, Situacao
    *   - SetorId, ResponsavelId, PrioridadeId, NivelPrioridadeId, SolicitanteId
    *   - Criterios, Prioridades: listas para popular dropdowns na view
    *   - Comentarios: lista de ComentarioViewModel
    *   - Historico: lista de HistoricoViewModel
    *   - Anexos: lista de AnexoViewModel
*/

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PIM.Models.ViewModels
{
    public class ComentarioViewModel
    {
        public int Id { get; set; }
        public string NomeUsuario { get; set; }
        public int IdUsuario { get; set; }
        public string Texto { get; set; }
        public DateTime Data { get; set; }
    }

    public class HistoricoViewModel
    {
        public string NomeUsuario { get; set; }
        public string AcaoTomada { get; set; }
        public DateTime Data { get; set; }
        public int IdUsuario { get; set; }
    }

    public class AnexoViewModel
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public string NomeUsuario { get; set; }
        public DateTime DataEnvio { get; set; }
    }

    public class ChamadoDetalhesViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string Status { get; set; }
        public string Solicitante { get; set; }
        public string Responsavel { get; set; }
        public string Setor { get; set; }
        public string SetorSoli { get; set; }
        public string PrioridadeDescricao { get; set; }
        public string NivelPrioridadeDescricao { get; set; }
        public string Situacao { get; set; }
        public int SetorId { get; set; }
        public int? ResponsavelId { get; set; } // Adicione isso no ViewModel
        public int? PrioridadeId { get; set; }  // <- Para armazenar o ID selecionado
        public int? NivelPrioridadeId { get; set; }  // <- Para armazenar o ID selecionado
        public int SolicitanteId { get; set; }

        public List<SelectListItem> Criterios { get; set; }
        public List<SelectListItem> Prioridades { get; set; }
        public List<ComentarioViewModel> Comentarios { get; set; }
        public List<HistoricoViewModel> Historico { get; set; }
        public List<AnexoViewModel> Anexos { get; set; }
    }
}
