/**
    * SessionExtensions
    *
    * Classe estática de extensões para facilitar o armazenamento e recuperação de objetos complexos
    * na sessão do ASP.NET Core usando JSON.
    *
    * Métodos:
    * - SetObjectAsJson(ISession session, string key, object value)
    *   Serializa um objeto para JSON e salva na sessão usando a chave fornecida.
    *
    * - GetObjectFromJson<T>(ISession session, string key)
    *   Recupera um objeto da sessão pela chave, desserializando o JSON de volta para o tipo T.
    *
    * Dependências:
    * - Microsoft.AspNetCore.Http: para ISession
    * - Newtonsoft.Json: para serialização e desserialização JSON
*/

using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace PIM.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
