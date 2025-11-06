/**
    * Anexo:
    * - Representa um arquivo anexado a um chamado.
    * - Propriedades:
    *   - ID: Identificador único do anexo.
    *   - NomeArquivo: Nome do arquivo enviado.
    *   - CaminhoArquivo: Caminho físico ou virtual do arquivo no servidor.
    *   - Formato: Extensão ou tipo do arquivo (ex: jpg, pdf).
    *   - ID_Chamado: Identificador do chamado ao qual o anexo pertence.
    *   - ID_Usuario: Identificador do usuário que enviou o anexo.
    *   - Data: Data de envio do anexo.
    * - Relacionamentos:
    *   - Chamado: Navegação para o chamado relacionado.
    *   - Usuario: Navegação para o usuário que realizou o upload.
*/

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIM.Models
{
    public class Anexo
    {
        public int ID { get; set; }

        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public string Formato { get; set; }

        public int ID_Chamado { get; set; }
        public int ID_Usuario { get; set; }

        public DateTime Data { get; set; }

        // Corrigido: define manualmente as FKs
        [ForeignKey("ID_Chamado")]
        public Chamado Chamado { get; set; }

        [ForeignKey("ID_Usuario")]
        public Usuario Usuario { get; set; }
    }
}
