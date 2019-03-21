using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Washout : MonoBehaviour {
    #region Init eulerAngle
    //翻滚角度
    private float phi = 0f;
    public float Phi
    {
        set
        {
            //保证不超程
            if (value > 10f) value = 10f;
            else if (value < -10f) value = 10f;          
            phi = value;
        }
        get { return phi; }
    }
    //俯仰角度
    private float theta = 0f;
    public float Theta
    {
        set
        {
            //保证不超程
            if (value > 10f) value = 10f;
            else if (value < -10f) value = 10f;
            theta = value;
        }
        get { return theta; }
    }
    //偏航角度
    private float psi = 0f;
    public float Psi
    {
        set
        {
            //保证不超程
            if (value > 10f) value = 10f;
            else if (value < -10f) value = 10f;
            psi = value;
        }
        get { return psi; }
    }
    #endregion
    public const float g = 9.8f;//重力加速度的数值
    public Vector3 Raw_aAA; //输入的原生加速度，动平台坐标系
    private Vector3 aAA;  //经过比例环节后的加速度，动平台坐标系
    private Matrix M_aAA; //经过比例环节后的加速度的列矩阵形式，动平台坐标系
    private Vector3 fAA; //比力，动平台坐标系
    private Matrix M_fAA;//比力的列矩阵形式，动平台坐标系
    private Matrix R;//论文中的L IS,动到惯性坐标系的旋转变换矩阵
    private Vector3 gI= new Vector3(0f,0f,-g);  //重力加速度，惯性坐标系
    private Matrix M_gI; //重力加速度的矩阵形式，惯性坐标系
    private Vector3 aIA; //加速度，输入_高通滤波器（高通加速度通道），惯性坐标系
    private Matrix M_aIA;//加速度的矩阵形式，输入_高通滤波器（高通加速度通道），惯性坐标系
    private Vector3 aIS; //加速度，输出_高通滤波器（高通加速度通道），惯性坐标系
    private const float k = 0.02f * g;   //限制环节，高通加速度通道
    public Vector3 SIS; //位移，高通加速度通道最后的输出

    private Vector3 fSL = new Vector3(0f, 0f, 0f);//比力，是fAA经过低通滤波的输出，也是倾斜协调的输入，z值用不到
    private Vector3 BetaSL = new Vector3(0f, 0f, 0f);//倾斜角度，倾斜协调通道最后的输出，phi,theta,psi的顺序对应xyz轴的旋转弧度

    public Vector3 Raw_wAA; //输入的原生角速度，动平台坐标系
    private Vector3 wAA;//经过比例环节之后的角速度，动平台坐标系
    private Matrix M_wAA;//角速度的列矩阵形式
    private Matrix Ts;  //角速度到欧拉角变换率的变换矩阵
    private Vector3 BetaADot;//欧拉角变换率,输入_高通滤波，高通角速度通道
    private Matrix M_BetaADot;//欧拉角变换率的矩阵形式,输入_高通滤波，高通角速度通道
    private Vector3 BetaSHDot;//欧拉角变换率,输出_高通滤波，高通角速度通道
    private Vector3 BetaSH = new Vector3(0f, 0f, 0f);//倾斜角度,高通角速度通道最后的输出
    private const float sense_EularX = 3f * Mathf.PI / 180f;//侧向角速度感知阈值
    private const float sense_EularY = 3.6f * Mathf.PI / 180f;//纵向角速度感知阈值
    private Vector3 BetaS = new Vector3(0f, 0f, 0f);//输出_最终的欧拉角
    #region 记录链表
    //本项目种所有的滤波器List都只存最新的4个数据，类似于历史记录
    //list aIA，高通加速度通道
    private List<float> list_aIA_X = new List<float> { 0f, 0f, 0f, 0f };//X方向的加速度aIA的最新4个预存值
    private List<float> list_aIA_Y = new List<float> { 0f, 0f, 0f, 0f };//Y方向的加速度aIA的最新4个预存值
    private List<float> list_aIA_Z = new List<float> { 0f, 0f, 0f, 0f };//Z方向的加速度aIA的最新4个预存值
    //list aIS，高通加速度通道
    private List<float> list_aIS_X = new List<float> { 0f, 0f, 0f, 0f };//X方向的加速度aIS的最新4个预存值
    private List<float> list_aIS_Y = new List<float> { 0f, 0f, 0f, 0f };//Y方向的加速度aIS的最新4个预存值
    private List<float> list_aIS_Z = new List<float> { 0f, 0f, 0f, 0f };//Z方向的加速度aIS的最新4个预存值
    //list  fAA_X 和 fAA_Y,倾斜协调通道需要fAA的X和Y方向的记录
    private List<float> list_fAA_X = new List<float> { 0f, 0f, 0f, 0f };//X方向的比力fAA的最新4个预存值，它将会造成Y轴旋转也就是theta的变化
    private List<float> list_fAA_Y = new List<float> { 0f, 0f, 0f, 0f };//Y方向的比力fAA的最新4个预存值，它将会造成X轴旋转也就是phi的变化
    //list fSL_X,倾斜协调通道需要fSL的X方向的记录,也就是说，X和Y方向的滤波器本质上有区别
    private List<float> list_fSL_X = new List<float> { 0f, 0f, 0f, 0f };//X方向的比力fSL的最新4个预存值，它将会造成Y轴旋转也就是theta的变化
    //list BetaADot，高通角速度通道
    private List<float> list_BetaADot_X = new List<float> { 0f, 0f, 0f, 0f };//输入_X轴的欧拉角变换率的最新4个预存值
    private List<float> list_BetaADot_Y = new List<float> { 0f, 0f, 0f, 0f };//输入_Y轴的欧拉角变换率的最新4个预存值
    private List<float> list_BetaADot_Z = new List<float> { 0f, 0f, 0f, 0f };//输入_Z轴的欧拉角变换率的最新4个预存值
    //list BetaSHDot，高通角速度通道
    private List<float> list_BetaSHDot_X = new List<float> { 0f, 0f, 0f, 0f };//输出_X轴的欧拉角变换率的最新4个预存值
    private List<float> list_BetaSHDot_Y = new List<float> { 0f, 0f, 0f, 0f };//输出_Y轴的欧拉角变换率的最新4个预存值
    private List<float> list_BetaSHDot_Z = new List<float> { 0f, 0f, 0f, 0f };//输出_Z轴的欧拉角变换率的最新4个预存值
    //list BetaSH，高通角速度通道
    private List<float> list_BetaSH_X = new List<float> { 0f, 0f, 0f, 0f };//X轴的欧拉角的最新4个预存值，高通角速度通道
    private List<float> list_BetaSH_Y = new List<float> { 0f, 0f, 0f, 0f };//Y轴的欧拉角的最新4个预存值，高通角速度通道
    private List<float> list_BetaSH_Z = new List<float> { 0f, 0f, 0f, 0f };//Z轴的欧拉角的最新4个预存值，高通角速度通道
    #endregion

    private float timer=0f;
    private float tempFloatValue_In=0f;
    private float tempFloatValue_Out = 0f;
    FileInfo inFile;
    FileInfo outFile;
    StreamWriter swin; 
    StreamWriter swout;
    private void Start()
    {
        R = SetR(0f,0f,0f);
        Ts = SetTs(0f,0f,0f);
        M_gI = new Matrix(gI);
        #region 
        inFile = new FileInfo("input" + ".csv");
        outFile = new FileInfo("output" + ".csv");
        swin = inFile.AppendText(); // 在原文件后面追加内容，应用fi.AppendText()
        swout = outFile.AppendText();
        #endregion
    }

    void Update ()
    {
        timer += Time.deltaTime;
        #region 高通加速度通道
        //TODO Raw_aAA需要时刻获取
        R = SetR(Phi, Theta, Psi);
        Prepare_Raw_aAA_To_fAA();
        Prepare_fAA_To_aIA();
        Fliter_aIA_To_aIS();              
        LimitAndOutput_aIS_To_SIS();
        #endregion
        
        #region 倾斜协调通道
        Fliter_fAA_To_fSL();
        TiltAndLimit_fSL_To_BetaSL();
        #endregion

        #region 高通角速度通道
        //TODO Raw_wAA需要时刻获取
        Ts = SetTs(Phi, Theta, Psi);
        Prepare_Raw_wAA_To_BetaADot();
        Fliter_BetaADot_To_BetaSHDot();
        LimitAndOutput_BetaSHDot_To_BetaSH();
        #endregion

        MakeNewEularAngle();
       
        #region 图像输出到csv

        tempFloatValue_In = list_aIA_X[0];
        tempFloatValue_Out = list_aIS_X[3];

        if (timer <= 20f)
        {
            swin.Write(timer);
            swin.Write(",");
            swin.Write(list_aIA_X[0]);           
            swin.Write(",");
            swin.Write(list_aIA_Y[0]);
            swin.Write("\n");

            //swout.Write(timer);
            //swout.Write(",");
            //swout.Write(list_aIS_X[0]);
            ////swout.Write(timer);
            //swout.Write(",");
            //swout.Write(list_aIS_X[1]);
            ////swout.Write(timer);
            //swout.Write(",");
            //swout.Write(list_aIS_X[2]);
            ////swout.Write(timer);
            //swout.Write(",");
            //swout.Write(list_aIS_X[3]);
            //swout.Write("\n");

            swout.Write(timer);
            swout.Write(",");
            swout.Write(fSL.x);
            swout.Write(",");
            swout.Write(fSL.y);
            swout.Write("\n");
        }
        if (timer > 20f)
        {
            swin.Close();
            swin.Dispose();
            swout.Close();
            swout.Dispose();
        }
        #endregion
    }
    #region 高通加速度通道的具体实现
    private Matrix SetR(float phi, float theta, float psi)
    {
        //角度转弧度
        phi *= Mathf.PI / 180f;
        theta *= Mathf.PI / 180f;
        psi *= Mathf.PI / 180f;
        //缩放旋转程度
        //phi /= 5f;
        //theta /= 5f;
        //psi /= 5f;
        //构建矩阵
        Matrix m = new Matrix(3, 3);
        //赋值
        m[0, 0] = Mathf.Cos(theta) * Mathf.Cos(psi);
        m[0, 1] = Mathf.Sin(phi) * Mathf.Sin(theta) * Mathf.Cos(psi) - Mathf.Cos(phi) * Mathf.Sin(psi);
        m[0, 2] = Mathf.Cos(phi) * Mathf.Sin(theta) * Mathf.Cos(psi) + Mathf.Sin(phi) * Mathf.Sin(psi);

        m[1, 0] = Mathf.Cos(theta) * Mathf.Sin(psi);
        m[1, 1] = Mathf.Sin(phi) * Mathf.Sin(theta) * Mathf.Sin(psi) + Mathf.Cos(phi) * Mathf.Cos(psi);
        m[1, 2] = Mathf.Cos(phi) * Mathf.Sin(theta) * Mathf.Sin(psi) - Mathf.Sin(phi) * Mathf.Cos(psi);

        m[2, 0] = -Mathf.Sin(theta);
        m[2, 1] = Mathf.Sin(phi) * Mathf.Cos(theta);
        m[2, 2] = Mathf.Cos(phi) * Mathf.Cos(theta);
        return m;
    } //论文中的L IS，欧拉角旋转矩阵ZYX顺序，右手坐标系，逆时针为正，列向量左乘，R=RxRyRz
    private void Prepare_Raw_aAA_To_fAA()
    {
        aAA = Raw_aAA * 1f;//比例环节 高通加速度通道      
        M_aAA = new Matrix(aAA);
        M_fAA = M_aAA - Matrix.transpose(R) * M_gI;
        fAA = M_fAA.GetVector3();
        Fliter_AddToList_fAA_XY();
    }//输出fAA和矩阵和向量表示，****并且把fAA.x和fAA.y存入记录中
        private void Fliter_AddToList_fAA_XY()
        {
            list_fAA_X.Add(fAA.x); list_fAA_X.RemoveAt(0);
            list_fAA_Y.Add(fAA.y); list_fAA_Y.RemoveAt(0);
        }//把fAA的X和Y方向添加到list尾，在函数Prepare_Raw_aAA_To_fAA中被调用
    private void Prepare_fAA_To_aIA()
    {
        M_aIA = R * M_fAA + M_gI;
        aIA = M_aIA.GetVector3();
    }//输出aIA的向量表示，是高通加速度通道的高通滤波器的输入
    private void Fliter_aIA_To_aIS()
    {
        //aIS.x = 0.502f * aIA.x - 1.507f * list_aIA_X[3] + 1.507f * list_aIA_X[2] - 0.502f * list_aIA_X[1]
        //                       + 1.824f * list_aIS_X[3] - 1.027f * list_aIS_X[2] + 0.117f * list_aIS_X[1];

        aIS.x = 1f * aIA.x / 9f     - 2f * list_aIA_X[3] / 9f     + 1f * list_aIA_X[2] / 9f
                                    - 2f * list_aIS_X[3] / 3f     - 1f * list_aIS_X[2] / 9f;
        //TODO 以下俩个需要换掉
        aIS.y = 1f / 12.25f * aIA.y + 2f / 12.25f * list_aIA_Y[3]       + 1f / 12.25f * list_aIA_Y[2]
                                    + 10.5f / 12.25f * list_aIS_Y[3]    - 2.25f / 12.25f * list_aIS_Y[2];
        aIS.z = 1f / 13.2f  * aIA.z + 3f / 13.2f * list_aIA_Z[3]        + 3f / 13.2f * list_aIA_Z[2]        + 1f / 13.2f * list_aIA_Z[1]
                                    - 10.8f / 13.2f  * list_aIS_Z[3]    + 8.8f / 13.2f   * list_aIS_Z[2]    + 7.2f / 13.2f * list_aIS_Z[1];
        Fliter_AddToList_aIA();
        Fliter_AddToList_aIS();
    }//用aIA和记录的aIS做输入进行高通滤波环节，****并且把aIS存入到记录中  
        private void Fliter_AddToList_aIA()
        {
            list_aIA_X.Add(aIA.x); list_aIA_X.RemoveAt(0);
            list_aIA_Y.Add(aIA.y); list_aIA_Y.RemoveAt(0);
            list_aIA_Z.Add(aIA.z); list_aIA_Z.RemoveAt(0);
        }//把aIA添加到list尾 ，在函数Fliter_aIA_To_aIS中被调用
        private void Fliter_AddToList_aIS()
        {
            list_aIS_X.Add(aIS.x); list_aIS_X.RemoveAt(0);
            list_aIS_Y.Add(aIS.y); list_aIS_Y.RemoveAt(0);
            list_aIS_Z.Add(aIS.z); list_aIS_Z.RemoveAt(0);
        }//把aIS添加到list尾 ，在函数Fliter_aIA_To_aIS中被调用       
    private void LimitAndOutput_aIS_To_SIS()
    {
        if (aIS.magnitude <= k) return;//限制环节，舍弃过低的加速度
        //此时aIS已经进入到list中了,list_aIS_X[3]==aIS.x，yz同理
        SIS.x = 0.25f * list_aIS_X[3] + 0.5f * list_aIS_X[2] + 0.25f * list_aIS_X[1];
        SIS.y = 0.25f * list_aIS_Y[3] + 0.5f * list_aIS_Y[2] + 0.25f * list_aIS_Y[1];
        SIS.z = 0.25f * list_aIS_Z[3] + 0.5f * list_aIS_Z[1] + 0.25f * list_aIS_Z[1];      
    }//输出位移。通过进行限制环节和2次滤波，把加速度转化为位移；   
    #endregion
    #region 倾斜协调通道的具体实现
    private void Fliter_fAA_To_fSL()
    {
        //此时的fAA的X和Y方向已经在记录中,比如fAA.y==list_fAA_Y[3]，而FSL.x的值在函数结束后进入记录FSL.y不需要记录
        //x方向的力，纵摇，绕y轴旋转
        fSL.x = 2.25f / 6.25f * list_fAA_X[3] + 4.5f / 6.25f * list_fAA_X[2] + 2.25f / 6.25f * list_fAA_X[1]
              - 2.5f / 6.25f * list_fSL_X[3] - 0.25f / 6.25f * list_fSL_X[2];
        //y方向的力，横摇，绕x轴旋转
        fSL.y = 0.25f * list_fAA_Y[3] + 0.5f * list_fAA_Y[2] + 0.25f * list_fAA_Y[1];
 
        Fliter_AddToList_fSL_X();
    }//用fAA.x,记录的fAA_Y,记录的fSL_X 做输入进行低通滤波环节，****并且把fSL.x存入到记录中
        private void Fliter_AddToList_fSL_X()
        {
            list_fSL_X.Add(fSL.x); list_fSL_X.RemoveAt(0);
        }//把fSL的X方向添加到list尾，在函数Fliter_fAA_To_fSL中被调用
    private void TiltAndLimit_fSL_To_BetaSL()
    {
        BetaSL.x = -fSL.y / g;
        BetaSL.y = fSL.x / g;
        //TODO 弄清楚这个是怎么进行角速度限制的（不然就不进行限制了）
        //TODO 尝试观察间隔时间是否是定值，就可以简化计算
        //float max_BetaSL_X = Time.deltaTime * sense_EularX;
        //float max_BetaSL_Y = Time.deltaTime * sense_EularY;
        //Mathf.Clamp(BetaSL.x, -max_BetaSL_X, max_BetaSL_X);
        //Mathf.Clamp(BetaSL.y, -max_BetaSL_Y, max_BetaSL_Y);
    }
    #endregion
    #region 高通角速度通道的具体实现
    private Matrix SetTs(float phi, float theta, float psi)
    {
        //角度转弧度
        phi *= Mathf.PI / 180f;
        theta *= Mathf.PI / 180f;
        psi *= Mathf.PI / 180f;
        //构建矩阵
        Matrix m = new Matrix(3, 3);
        //赋值
        m[0, 0] = 1f;
        m[0, 1] = Mathf.Sin(phi) * Mathf.Tan(theta) ;
        m[0, 2] = Mathf.Cos(phi) * Mathf.Tan(theta);

        m[1, 0] = 0f;
        m[1, 1] = Mathf.Cos(phi) ;
        m[1, 2] = -Mathf.Sin(phi) ;

        m[2, 0] = 0f;
        m[2, 1] = Mathf.Sin(phi) / Mathf.Cos(theta);
        m[2, 2] = Mathf.Cos(phi) / Mathf.Cos(theta);
        return m;
    }//设置Ts的值
    private void Prepare_Raw_wAA_To_BetaADot()
    {
        wAA = Raw_wAA * 1f;
        M_wAA = new Matrix(wAA);
        M_BetaADot = Ts * M_wAA;
        BetaADot = M_BetaADot.GetVector3();
    }//输出BetaADot的向量表示，是高通角速度通道的高通滤波器的输入
    private void Fliter_BetaADot_To_BetaSHDot()
    {
        BetaSHDot.x = 0.5f * BetaADot.x + 1.5f * list_BetaADot_X[3] + 1.5f * list_BetaADot_X[2] + 0.5f * list_BetaADot_X[1]
                                        + list_BetaSHDot_X[3]       - list_BetaSHDot_X[2]       + list_BetaSHDot_X[1];
        BetaSHDot.y = 0.5f * BetaADot.y + 1.5f * list_BetaADot_Y[3] + 1.5f * list_BetaADot_Y[2] + 0.5f * list_BetaADot_Y[1]
                                        + list_BetaSHDot_Y[3] - list_BetaSHDot_Y[2] + list_BetaSHDot_Y[1];
        BetaSHDot.z = 0.5f * BetaADot.z   + 1.5f * list_BetaADot_Z[3] + 1.5f * list_BetaADot_Z[2] + 0.5f * list_BetaADot_Z[1] 
                                        + list_BetaSHDot_Z[3] - list_BetaSHDot_Z[2] + list_BetaSHDot_Z[1];
        Fliter_AddToList_BetaADot();
        Fliter_AddToList_BetaSHDot();
    }//用BetaADot及其记录和记录的BetaSHDot做输入进行高通滤波环节，****并且把BetaADot和BetaSHDot存入到记录中  
        private void Fliter_AddToList_BetaADot()
        {
            list_BetaADot_X.Add(BetaADot.x); list_BetaADot_X.RemoveAt(0);
            list_BetaADot_Y.Add(BetaADot.y); list_BetaADot_Y.RemoveAt(0);
            list_BetaADot_Z.Add(BetaADot.z); list_BetaADot_Z.RemoveAt(0);
        }//把BetaADot添加到list尾 ，在函数Fliter_BetaADot_To_BetaSHDot中被调用
        private void Fliter_AddToList_BetaSHDot()
        {           
            list_BetaSHDot_X.Add(BetaSHDot.x); list_BetaSHDot_X.RemoveAt(0);
            list_BetaSHDot_Y.Add(BetaSHDot.y); list_BetaSHDot_Y.RemoveAt(0);
            list_BetaSHDot_Z.Add(BetaSHDot.z); list_BetaSHDot_Z.RemoveAt(0);
        }//把BetaSHDot添加到list尾 ，在函数Fliter_BetaADot_To_BetaSHDot中被调用
    private void LimitAndOutput_BetaSHDot_To_BetaSH()
    {
        //限制环节，低于阈值的角速度就直接为0，平台在这个轴向上就可以不用动了
        if (Mathf.Abs(BetaSHDot.x) <= sense_EularX) BetaSHDot.x = 0f;
        if (Mathf.Abs(BetaSHDot.y) <= sense_EularY) BetaSHDot.y = 0f;
        BetaSH.x = 2f * BetaSHDot.x + list_BetaSH_X[3];
        BetaSH.y = 2f * BetaSHDot.y + list_BetaSH_Y[3];
        BetaSH.z = 2f * BetaSHDot.z + list_BetaSH_Z[3];
        LimitAndOutput_AddToList_BetaSH();
    }//限制过低的角速度，输出BetaSH，并把BetaSH存入记录中
        private void LimitAndOutput_AddToList_BetaSH()
        {
            list_BetaSH_X.Add(BetaSH.x); list_BetaSH_X.RemoveAt(0);
            list_BetaSH_Y.Add(BetaSH.y); list_BetaSH_Y.RemoveAt(0);
            list_BetaSH_Z.Add(BetaSH.z); list_BetaSH_Z.RemoveAt(0);
        }//把BetaSH添加到list尾 ，在函数LimitAndOutput_AddToList_BetaSH中被调用
# endregion
    private void MakeNewEularAngle()
    {
        BetaS = BetaSL + BetaSH;
        Phi = BetaS.x * 180f / Mathf.PI;
        Theta = BetaS.y * 180f / Mathf.PI;
        psi = BetaS.z * 180f / Mathf.PI;
    }//为平台姿态的欧拉角重新赋值
}

