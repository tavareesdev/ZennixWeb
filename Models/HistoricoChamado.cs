/**
    * HistoricoChamado:
    * - Representa o histórico de ações realizadas em um chamado específico.
    *
    * Propriedades:
    * - Id: Identificador único do registro de histórico.
    * - ID_Chamado: Chave estrangeira referenciando o chamado associado.
    * - Chamado: Navegação para o objeto Chamado relacionado.
    * - ID_Usuario: Chave estrangeira referenciando o usuário que realizou a ação.
    * - Usuario: Navegação para o objeto Usuario que executou a ação.
    * - AcaoTomada: Descrição da ação realizada (ex.: "Alterou Status para Concluído").
    * - Data: Data e hora em que a ação foi realizada.
    *
    * Observações:
    * - Permite rastrear todas as modificações e interações feitas em chamados dentro do sistema.
*/

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIM.Models
{
    public class HistoricoChamado
    {
        public int Id { get; set; }
        
        public int ID_Chamado { get; set; }
        [ForeignKey(nameof(ID_Chamado))]
        public Chamado Chamado { get; set; }

        public int ID_Usuario { get; set; }
        [ForeignKey(nameof(ID_Usuario))]
        public Usuario Usuario { get; set; }

        public string AcaoTomada { get; set; }
        public DateTime Data { get; set; }
    }
}
