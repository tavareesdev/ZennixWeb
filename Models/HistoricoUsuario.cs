/**
    * HistoricoUsuario:
    * - Representa o histórico de ações realizadas em um usuário específico dentro do sistema.
    *
    * Propriedades:
    * - ID: Identificador único do registro de histórico (PK).
    * - Data: Data e hora em que a ação foi realizada.
    * - AcaoTomada: Descrição da ação realizada (ex.: "Alterou o campo Nome de 'João' para 'José'").
    * - QuemFez: Nome ou identificação do usuário que executou a ação.
    * - ID_Usuario: Identificador do usuário alvo da ação (aquele que teve alterações feitas).
    * - ID_Modificante: Identificador do usuário que realizou a modificação (pode ser nulo se for ação do sistema).
    *
    * Observações:
    * - Permite auditar e rastrear todas as mudanças feitas nos dados de usuários no sistema.
*/

using System;

namespace PIM.Models
{
    public class HistoricoUsuario
    {
        public int ID { get; set; }  // PK

        public DateTime Data { get; set; }

        public string AcaoTomada { get; set; }  // Descrição da ação

        public string QuemFez { get; set; }  // Nome ou identificação de quem fez a ação

        public int ID_Usuario { get; set; }  // Usuário alvo da ação (o usuário que teve alterações feitas)

        public int? ID_Modificante { get; set; }  // Usuário que fez a modificação (pode ser null)
    }
}
