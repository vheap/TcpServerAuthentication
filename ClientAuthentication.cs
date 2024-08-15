using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServerAuthentication
{
    public class ClientAuthentication
    {
        public static void ProcessClient(Client client)
        {
            if (client == null) { return; }
            client.AuthenticateConnection();
        }
    }
}
