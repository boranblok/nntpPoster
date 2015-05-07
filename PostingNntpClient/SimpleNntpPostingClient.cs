using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PostingNntpClient
{
    public class SimpleNntpPostingClient : IDisposable
    {
        private static readonly Encoding iso88591Encoding = Encoding.GetEncoding("iso-8859-1");

        public NewsHostConnectionInfo ConnectionInfo { get; private set; }

        private TcpClient _tcpClient;
        private Stream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;

        public SimpleNntpPostingClient(NewsHostConnectionInfo connectionInfo)
        {
            ConnectionInfo = connectionInfo;
        }

        private Boolean CertValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
        
        private NntpResponse ReadResponse()
        {
            var response = _reader.ReadLine();
            return new NntpResponse(response);
        }

        public void Connect()
        {
            _tcpClient = new TcpClient(ConnectionInfo.Address, ConnectionInfo.Port);
            var stream = _tcpClient.GetStream();
            if(ConnectionInfo.UseSsl)
            {
                var sslStream = new SslStream(stream, true, CertValidationCallback);
                sslStream.AuthenticateAsClient(ConnectionInfo.Address);
                _stream = sslStream;
            }
            else
            {
                _stream = stream;
            }
            _writer = new StreamWriter(_stream, iso88591Encoding);
            _writer.NewLine = "\r\n";  //Added for mono compatibility, nntp requires \r\n as newline char, not just \n as used by mono.
            _writer.AutoFlush = true;
            _reader = new StreamReader(_stream, iso88591Encoding);

            var connectResponse = ReadResponse();
            if (connectResponse.ResponseCode != Rfc977ResponseCodes.ServerReadyPostingAllowed)
                throw new Exception("Could not open a posting connection: " + connectResponse.ResponseMessage);

            Authenticate();
        }

        private void Authenticate()
        {
            _writer.WriteLine("AUTHINFO USER " + ConnectionInfo.Username);
            var response = ReadResponse();
            if(response.ResponseCode == Rfc4643ResponseCodes.PasswordRequired)
            {
                _writer.WriteLine("AUTHINFO PASS " + ConnectionInfo.Password);
                response = ReadResponse();
                if (response.ResponseCode != Rfc4643ResponseCodes.AuthenticationAccepted)
                {
                    throw new Exception("Could not authenticate: " + response.ResponseMessage);
                }
            }
            else
            {
                throw new Exception("Unable to autenticate: " + response.ResponseMessage);
            }
        }

        public String PostYEncMessage(String from, String subject, IEnumerable<String> newsGroups, 
            DateTime postedDateTime, IEnumerable<String> yEncHeaders, Byte[] yEncBody, IEnumerable<String> yEncFooters)
        {
            _writer.WriteLine("POST");
            var response = ReadResponse();
            if (response.ResponseCode != Rfc977ResponseCodes.SendArticleToPost)
            {
                throw new Exception("Could not start posting message: " + response.ResponseMessage);
            }
            var messageId = ExtractMessageID(response.ResponseMessage);
            var postedDateTimeString = postedDateTime.ToString("ddd, dd MMM yyyy HH:mm:ss zzz (UTC)", CultureInfo.InvariantCulture);
            postedDateTimeString = postedDateTimeString.Remove(postedDateTimeString.LastIndexOf(':'), 1);

            _writer.WriteLine("From: {0}", from);
            _writer.WriteLine("Subject: {0}", subject);
            _writer.WriteLine("Date: {0}", postedDateTimeString);
            _writer.WriteLine("Message-ID: {0}", messageId);
            Boolean first = true;
            foreach(var group in newsGroups)
            {
                if(first)
                {
                    _writer.WriteLine("Newsgroups: {0}", group);
                    first = false;
                }
                else
                {
                    _writer.WriteLine("\t{0}", group);
                }
            }

            _writer.WriteLine("");

            foreach (var line in yEncHeaders)
            {
                _writer.WriteLine(line);
            }

            _stream.Write(yEncBody, 0, yEncBody.Length);

            foreach (var line in yEncFooters)
            {
                _writer.WriteLine(line);
            }

            _writer.WriteLine(".");
            response = ReadResponse();

            if (response.ResponseCode != Rfc977ResponseCodes.ArticlePostedOk)
            {
                throw new Exception("Article failed to post: " + response.ResponseMessage);
            }

            return messageId;
        }

        private String ExtractMessageID(String serverResponseCode)
        {
            var indexOfOpenBracket = serverResponseCode.IndexOf('<');
            var indexOfCloseBracket = serverResponseCode.IndexOf('>');
            if (indexOfOpenBracket > 0 && indexOfCloseBracket > 0)
            {
                return serverResponseCode.Substring(indexOfOpenBracket, indexOfCloseBracket - indexOfOpenBracket + 1);
            }
            return "<" + Guid.NewGuid().ToString("N") + "@" + ConnectionInfo.Address + ">";
        }


        public void Dispose()
        {
            try
            {
                if (_reader != null)
                    _reader.Dispose();
            }
            catch
            { }

            try
            {
                if (_writer != null)
                    _writer.Dispose();
            }
            catch { }

            try
            {
                if (_stream != null)
                    _stream.Dispose();
            }
            catch { }
        }
    }
}
