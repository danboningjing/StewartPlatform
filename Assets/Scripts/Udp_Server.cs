using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Udp_Server : MonoBehaviour {
    static Socket server;
    Thread t1;
    Thread t2;
	// Use this for initialization
	void Start () {
        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        server.Bind(new IPEndPoint(IPAddress.Parse("192.168.15.101"),8410));
        Debug.Log("UDP启动");
        t1 = new Thread(ReciveMsg);
        t1.Start();
        t2 = new Thread(SendMsg);
        t2.Start();
    }
    static void SendMsg()
    {       
        EndPoint endPoint1 = new IPEndPoint(IPAddress.Parse("192.168.15.201"), 7408);//第一个MBOX
        EndPoint endPoint2 = new IPEndPoint(IPAddress.Parse("192.168.15.211"), 7418);//第二个
        while (true)
        {          
            server.SendTo(MakeUdpMsg.msg1, endPoint1);
            server.SendTo(MakeUdpMsg.msg2, endPoint2);
        }
    }
    static void ReciveMsg()
    {
        while (true)
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[1024];
            int length = server.ReceiveFrom(buffer, ref endPoint);
            string message = Encoding.UTF8.GetString(buffer, 0, length);
            Debug.Log("Recive："+endPoint.ToString() + message);
        }
    }
    void OnApplicationQuit()
    {
        //关闭线程
        if (t1 != null)
        {
            t1.Interrupt();
            t1.Abort();
        }
        if (t2 != null)
        {
            t2.Interrupt();
            t2.Abort();
        }
        //最后关闭socket
        if (server != null)
            server.Close();
       
        print("disconnect");
        
    }
  
 
}
