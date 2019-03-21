using System;
using UnityEngine;

public class MakeUdpMsg : MonoBehaviour {
    public string[] hex = new string[6];//每个轴的16进制表示的脉冲数
    public Inverse inverse;
    static public byte[] msg1 = new byte[38];
    static public byte[] msg2 = new byte[38];//20+12+6
    byte[] tempByte1 = new byte[4];
    byte[] tempByte2 = new byte[4];
    //debug
    private string outPutMsg1;
    private string outPutMsg2;
    void Start()
    {
        msg1 = HexStringToByte("55aa000013010000ffffffff000000010000000000000000000000000000000012345678abcd");//向第一个MBOX发送的udp报文
        msg2 = HexStringToByte("55aa000013010000ffffffff000000010000000000000000000000000000000012345678abcd");//向第二个MBOX发送的udp报文
        ////////////////////////****************************************112233441122334411223344
    }
    void Update () {
        hex = inverse.hex; //[20]-[31]  20-23 24-27 28-31 
        for (int i = 0; i < 3; i++)
        {
            //16进制字符串转字节数组，123轴一份，456轴一份，分别存入msg1和msg2
            tempByte1 = HexStringToByte(hex[i]);// tempByte.length等于4
            tempByte2 = HexStringToByte(hex[i + 3]);//分成两份
            for (int j = 0; j < 4; j++)
            {
                msg1[20 + i * 4 + j] = tempByte1[j];//放入对应位置              
            }
            for (int j = 0; j < 4; j++)
            {
                msg2[20 + i * 4 + j] = tempByte2[j];//放入对应位置
            }
        }
        outPutMsg1 = ByteToHexStr(msg1);
        outPutMsg2 = ByteToHexStr(msg2);
        //Debug.Log(outPutMsg1);
        //Debug.Log(outPutMsg2);
        //Debug.Log("55AA000013010000FFFFFFFF0000000199999999112233441122334411223344");
       
    }
    private byte[] HexStringToByte(string msg)
    {
        while (msg.Length<8)
        {
            msg = "0" + msg;
        }
        string strTemp = "";
        byte[] b = new byte[msg.Length / 2];
        for (int i = 0; i < msg.Length / 2; i++)
        {
            strTemp = msg.Substring(i * 2, 2);
            b[i] = Convert.ToByte(strTemp, 16);
        }
        //按照指定编码将字符串变为字节数组
        return b;
    }
    private string ByteToHexStr(byte[] bytes)
    {
        string returnStr = "";
        if (bytes != null)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                returnStr += bytes[i].ToString("X2");
            }
        }
        return returnStr;
    }
}
