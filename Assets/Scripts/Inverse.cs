using UnityEngine;

public class Inverse : MonoBehaviour {
    //public Transform car;//小车
    public Washout washout;
    private Vector3 SIS = new Vector3(0f,0f,0f);

    #region Declare XYZ and Matrix
    private const float R_a = 0.8083f;//单位：m
    private const float R_b = 0.8834f;//单位：m
    private const float Beta_a = 11.72f * Mathf.PI / 180f; //单位：弧度=0.2045526
    private const float Beta_b = 18.23f * Mathf.PI / 180f; //单位：弧度=0.3181735

    private float[] Beta_A = new float[6];//每个A点在惯性系中各自的弧度
    private Vector3[] A = new Vector3[6];//每个A点在惯性系中的XYZ位置
    private float[] Beta_B = new float[6];//每个B点在上平台中各自的弧度
    private Vector3[] B = new Vector3[6];//每个B点在上平台中的XYZ位置
    public Vector3 S = new Vector3(0f, 0f, 0.77f);//直接设置上平台中心点初始位置 0.77m

    private Matrix R = new Matrix(3, 3);//旋转变换矩阵
    private Matrix[] Pos_A = new Matrix[6];//每个A点在惯性系中坐标矩阵
    private Matrix[] Pos_B = new Matrix[6];//每个B点在上平台中坐标矩阵
    private Matrix Pos_S; //上平台中心点在惯性系中坐标矩阵(在SetXYZ_ABSL()方法中进行初始化，构造为列矩阵)
    private Matrix[] L = new Matrix[6];//每个电动缸的向量(列矩阵)
    #endregion

    #region Declare length of the six  poles
    private float[] length = new float[6];//六个电动缸的长度。单位：m
    private float originLength = 0.925f;//电动缸初始长度：行程20cm，92.5+20cm
    private int[] deltaLength = new int[6];//六个电动缸的伸长量。单位：mm
    private int[] pulse = new int[6];//六个电动缸的脉冲。伸长量/5*10000
    public string[] hex = new string[6];
    #endregion

    private void Start()
    {
        /******初始化L=S+RB-A中，等号右侧用到的所有矩阵。以及等号左面六个电动缸的向量矩阵******/
        //初始化A和B各点的坐标位置，最终结果为Vec3的向量，vector3.xyz代表右手坐标系中的xyz
        InitXYZ_AB();
        //初始化旋转矩阵，以后随物体姿态的变化，这个矩阵每帧都要改变
        R = SetR(0f, 0f, 0f);
        //把A和B各点，S点的位置存入矩阵中，共6+6+1=13个位置矩阵。并且初始化六个电动缸L的向量矩阵。
        SetXYZ_ABSL();
    }

    void Update()
    {

        ////Unity坐标系转右手坐标系，x为前，y为左，z为上，逆时针为正；
        //Phi = -car.eulerAngles.z;//翻滚，右手坐标系中x轴的角度
        //Theta = car.eulerAngles.x;//俯仰，右手坐标系中y轴的角度
        //Psi = -car.eulerAngles.y;//偏航，右手坐标系中z轴的角度
        //不断改变右手坐标系中欧拉角(ZYX顺序)旋转矩阵
       
        R = SetR(washout.Phi, washout.Theta, washout.Psi);
        SetS();//改动上平台XYZ的坐标
        //实时计算L
        for (int i = 0; i < 6; i++)
        {
            //求L的向量
            L[i] = Pos_S + (R * Pos_B[i]) - Pos_A[i];
            //取模长
            length[i] = Mathf.Sqrt(L[i][0, 0] * L[i][0, 0] + L[i][1, 0] * L[i][1, 0] + L[i][2, 0] * L[i][2, 0]);
            //伸长量，单位mm
            deltaLength[i] = (int)((length[i] - originLength) * 1000f);
        }
        if (LengthIsAllowed(deltaLength))
        {
            for (int i = 0; i < 6; i++)
            {
                pulse[i] = deltaLength[i] * 10000 / 5;
                hex[i] = pulse[i].ToString("X");
            }
        }

        //输出各个欧拉角和电动缸伸长量
        //Debug.Log("滚翻角phi:" + Phi + "  " + "俯仰角theta:" + Theta + "  " + "偏航角psi:" + Psi);
        /*  Debug.Log("1号：" + hex[0] + "  " + "2号：" + hex[1] + "  " + "3号：" + hex[2] + "  " +
            "4号：" + hex[3] + "  " + "5号：" + hex[4] + "  " + "6号：" + hex[5]);*/
        Debug.Log("1号：" + deltaLength[0] + "  " + "2号：" + deltaLength[1] + "  " + "3号：" + deltaLength[2] + "  " +
         "4号：" + deltaLength[3] + "  " + "5号：" + deltaLength[4] + "  " + "6号：" + deltaLength[5]);
    }

    //初始化A和B各点坐标
    private void InitXYZ_AB()
    {
        /******************************************************************************************/
        //计算每个B点在动坐标系的角度
        Beta_B[0] = Mathf.PI / 3f - Beta_b / 2f;       //1号轴
        Beta_B[1] = Mathf.PI / 3f + Beta_b / 2f;       //2
        Beta_B[2] = Mathf.PI - Beta_b / 2f;           //3号轴
        Beta_B[3] = Mathf.PI + Beta_b / 2f;           //4
        Beta_B[4] = 5f / 3f * Mathf.PI - Beta_b / 2f;   //5号轴 //注意不能写成 5 / 3 * Mathf.PI - Beta_b / 2; 因为5/3等于1
        Beta_B[5] = 5f / 3f * Mathf.PI + Beta_b / 2f;   //6
        //计算每个B点在动坐标系的位置       
        for (int i = 0; i < 6; i++)
        {
            B[i].x = R_b * Mathf.Cos(Beta_B[i]);
            B[i].y = R_b * Mathf.Sin(Beta_B[i]);
            B[i].z = 0f;
            //Debug.Log(B[i].x + " " + B[i].y + " " + B[i].z );
        }
        /******************************************************************************************/
        //计算每个A点在动坐标系的角度
        Beta_A[0] = Beta_a / 2f;                      //1号轴
        Beta_A[1] = 2f / 3f * Mathf.PI - Beta_a / 2f;  //2
        Beta_A[2] = 2f / 3f * Mathf.PI + Beta_a / 2f;   //3号轴
        Beta_A[3] = 4f / 3f * Mathf.PI - Beta_a / 2f;   //4
        Beta_A[4] = 4f / 3f * Mathf.PI + Beta_a / 2f;   //5号轴
        Beta_A[5] = 2f * Mathf.PI - Beta_a / 2f;       //6
        //计算每个A点在动坐标系的位置
        for (int i = 0; i < 6; i++)
        {
            A[i].x = R_b * Mathf.Cos(Beta_A[i]);
            A[i].y = R_b * Mathf.Sin(Beta_A[i]);
            A[i].z = 0f;
            //Debug.Log(A[i].x + " " + A[i].y + " " + A[i].z );
        }
        /******************************************************************************************/
    }

    //欧拉角旋转矩阵ZYX顺序，右手坐标系，逆时针为正，列向量左乘，R=RxRyRz
    private Matrix SetR(float phi, float theta, float psi)
    {
        //角度转弧度
        phi *= Mathf.PI / 180f;
        theta *= Mathf.PI / 180f;
        psi *= Mathf.PI / 180f;
       
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
    }
    //动态改变上平台中心点的坐标,也就是产生位移变化
    private void SetS()
    {
        //上平台中心点在惯性坐标系中的坐标矩阵
        float[] tempVec3S = new float[3] { washout.SIS.x, washout.SIS.y, S.z+washout.SIS.z };
        Pos_S = new Matrix(tempVec3S);
    }
    //位置坐标转矩阵
    private void SetXYZ_ABSL()
    {
        //定义A和B各自的六个三维矩阵
        for (int i = 0; i < 6; i++)
        {
            //A:6个位置矩阵
            float[] tempVec3A = new float[3] { A[i].x, A[i].y, A[i].z };//Vector3 转 float[3]
            Pos_A[i] = new Matrix(tempVec3A);//调用传入数组的构造函数，构造结果是列矩阵          
            //B:6个位置矩阵
            float[] tempVec3B = new float[3] { B[i].x, B[i].y, B[i].z };//Vector3 转 float[3]
            Pos_B[i] = new Matrix(tempVec3B);//调用传入数组的构造函数，构造结果是列矩阵
            //L:6个电动缸的向量
            L[i] = new Matrix(3, 1);
        }
        //定义上平台中心点在惯性坐标系中的坐标矩阵
        float[] tempVec3S = new float[3] { S.x, S.y, S.z };
        Pos_S = new Matrix(tempVec3S);
        /*********至此13个位置矩阵，和6个电动缸向量*********/
    }

    //范围限制
    private bool LengthIsAllowed(int[] length)
    {
        foreach(int temp in length)
        {
            if (temp < 0 || temp > 190)
            {
                return false;
            }
        }
        return true;
    }
}
