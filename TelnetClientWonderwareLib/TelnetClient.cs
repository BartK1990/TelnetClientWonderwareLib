using System.Text;

namespace TelnetClientWonderwareLib
{
    internal enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    internal enum Options
    {
        SGA = 3
    }

    public class TelnetClient
    {
        private readonly TcpConnection _tcpConnection;
        private readonly int _messageTimeoutMs;
        private readonly int _loginTimeoutMs;

        public TelnetClient(string ipAddress, int ipPort, int messageTimeoutMs, int loginTimeoutMs)
        {
            _messageTimeoutMs = messageTimeoutMs;
            _loginTimeoutMs = loginTimeoutMs;
            _tcpConnection = new TcpConnection(ipAddress, ipPort);
        }

        public string Login_SendCommand_Disconnect(string username, string password, string command)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Login(username, password));
            WriteLine(command);
            sb.AppendLine(Read());
            _tcpConnection.Disconnect();
            return sb.ToString();
        }

        public string Login(string username, string password)
        {
            _tcpConnection.Connect();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"R: {Read()}");
            if (!sb.ToString().TrimEnd().EndsWith(":"))
            {
                sb.AppendLine("Failed to connect : no login prompt");
                return sb.ToString();
            }
            WriteLine(username);
            sb.AppendLine($"R: {Read()}");
            if (!sb.ToString().TrimEnd().EndsWith(":"))
            {
                sb.AppendLine("Failed to connect : no password prompt");
                return sb.ToString();
            }
            WriteLine(password);
            System.Threading.Thread.Sleep(_loginTimeoutMs);
            sb.AppendLine($"R: {Read()}");
            return sb.ToString();
        }

        public string Read()
        {
            if (!_tcpConnection.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                System.Threading.Thread.Sleep(_messageTimeoutMs);
                ParseTelnet(sb);
            } while (_tcpConnection.Available > 0);
            return sb.ToString();
        }

        public string WriteLine(string cmd)
        {
            string s = $"{cmd}\r";
            Write(s);
            return s;
        }

        private void Write(string cmd)
        {
            if (!_tcpConnection.Connected) return;
            ASCIIEncoding ascii = new ASCIIEncoding();
            //byte[] buf = ascii.GetBytes(cmd.Replace("\0xFF","\0xFF\0xFF"));
            byte[] buf = ascii.GetBytes(cmd);
            _tcpConnection.WriteData(buf);
        }

        private void ParseTelnet(StringBuilder sb)
        {
            while (_tcpConnection.Available > 0)
            {
                int input = _tcpConnection.ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputVerb = _tcpConnection.ReadByte();
                        if (inputVerb == -1) break;
                        switch (inputVerb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputVerb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppress go ahead)
                                int inputOption = _tcpConnection.ReadByte();
                                if (inputOption == -1) break;
                                _tcpConnection.WriteByte((byte)Verbs.IAC);
                                if (inputOption == (int)Options.SGA)
                                {
                                    _tcpConnection.WriteByte(inputVerb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                }
                                else
                                {
                                    _tcpConnection.WriteByte(inputVerb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                }
                                _tcpConnection.WriteByte((byte)inputOption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }
    }
}
