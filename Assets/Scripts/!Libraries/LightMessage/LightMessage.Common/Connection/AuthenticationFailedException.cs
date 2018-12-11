using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightMessage.Common.Connection
{
    public class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException(string message) : base(message)
        {
        }

        public AuthenticationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AuthenticationFailedException()
        {
        }
    }
}
