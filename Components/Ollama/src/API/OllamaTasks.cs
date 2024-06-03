using System;

namespace SAAC.Ollama
{
    public class ConversationContext : IDisposable
    {
        public long[] Context { get; set; }

        public ConversationContext(long[] context) 
        {
            Context = context;
        }

        public void Dispose()
        {
            Context = null;
        }
    }

    public class ConversationContextWithResponse : ConversationContext
    {
        public string Response { get; set; }

        public ConversationContextWithResponse(string response, long[] context)
            : base(context)
        {
            Response = response;
        }
    }
}
