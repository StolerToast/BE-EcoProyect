namespace smartbin.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AgentNotFoundException : Exception
    {
        private string _message;

        public override string Message => _message;

        public AgentNotFoundException(int id)
        {
            _message = "Could not find agent with id " + id.ToString();
        }
    }
}
