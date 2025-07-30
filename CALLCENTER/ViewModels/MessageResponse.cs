namespace smartbin.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using smartbin.Enumerators; // Asegura el using correcto para MessageType

    public class MessageResponse : JsonResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public MessageType Type { get; set; }

        public static MessageResponse GetResponse(int code, string message, MessageType type)
        {
            return new MessageResponse
            {
                Code = code,
                Message = message,
                Type = type,
                Status = code == 0 ? 1 : 0
            };
        }
    }
}
