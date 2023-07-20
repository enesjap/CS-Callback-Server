using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;


// ASYNC metodlar kullanılırken bitirmeye dikkat etmek gerekir.
// Sebebi async metod çalışırken program async işlemi beklemeyi bırakır 
// sonrasında dönüp diğer işleri halleder bu sebeple ASYNC işlemlerde 
// doğru tamamlanabilme için kontrol sağlamalıyız. 
// IAsyncResult .NET 'in sağladığı bir Interfacedir işlme durumunu kontrol eder.
// I başlangıcı Interface'i temsil eder.Çeşitli sınıfların birbiri ile iletişimini sağlar.
// Callback metodları cpu'ya bir istek gönderir. Özellikleri karşılayan birşey olursa
// cpu fonksiyonu çağrır ve  fonksiyonda gerekli foksiyonu çağırır.
// Byte[] veri tipi genellikle veri depolama ve veri taşıma amaçları ile kullanılır


public class SocketServer
{
    private static TcpListener serverSocket;
    private static TcpClient clientSocket;
    private static ConcurrentBag<TcpClient> listenList = new ConcurrentBag<TcpClient>();
    public static void Main(String[] args)
    {
        StartServer();
        Console.ReadLine();
    }
    public static void StartServer()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = IPAddress.Parse("192.168.10.192");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 44715);
        serverSocket = new TcpListener(localEndPoint);
        serverSocket.Start();
        Console.WriteLine("Asynchonous server socket is listening at: {0}", localEndPoint.Address.ToString());
        WaitForClients();
    }
    private static void OnClientConnected(IAsyncResult asyncResult)
    {
        try
        {
            clientSocket = serverSocket.EndAcceptTcpClient(asyncResult);
            if (clientSocket != null)
            {
                Console.WriteLine("Received connection request from: {0}", clientSocket.Client.RemoteEndPoint.ToString());
                listenList.Add(clientSocket);
                Console.WriteLine("list count : {0}", listenList.Count);
            }
            HandleClientRequest(clientSocket);
        }
        catch (System.Exception)
        {
            throw;
        }
        WaitForClients();
    }
    private static void WaitForClients()
    {
        serverSocket.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnected), null);
    }
    private static void HandleClientRequest(TcpClient clientSocket)
    {
        byte[] buffer = new byte[1024];
        NetworkStream stream = clientSocket.GetStream();
        stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataMessage), new Tuple<NetworkStream, byte[]>(stream, buffer));
    }
    private static void OnDataMessage(IAsyncResult asyncResult)
    {
        try
        {
            Tuple<NetworkStream, byte[]> state = asyncResult.AsyncState as Tuple<NetworkStream, byte[]>;
            NetworkStream stream = state.Item1;
            Byte[] buffer = state.Item2;
            int bytesRead = stream.EndRead(asyncResult);

            if (bytesRead > 0)
            {

                string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Text Recieved: {0}", data);
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataMessage), new Tuple<NetworkStream, byte[]>(stream, buffer));
                stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(OnWriteComplete), new Tuple<NetworkStream, byte[]>(stream, buffer));

            }
            else
            {
                if (listenList.TryTake(out clientSocket))
                {   
                    Console.WriteLine("Client is disconnected");
                    Console.WriteLine("Connected Client Count Now: {0}", listenList.Count);
                }
            }
        }
        catch (Exception recv)
        {
            Console.WriteLine("Client is disconnected");
            if (listenList.TryTake(out clientSocket))
            {
                Console.WriteLine("Connected Client Count Now: {0}", listenList.Count);
            }
            else
            {
                Console.WriteLine("queue empty");
            }
        }
    }
    private static void OnWriteComplete(IAsyncResult asyncResult)
    {
        Tuple<NetworkStream, byte[]> state = asyncResult.AsyncState as Tuple<NetworkStream, byte[]>;
        NetworkStream stream = state.Item1;
        stream.EndWrite(asyncResult);
    }
}
