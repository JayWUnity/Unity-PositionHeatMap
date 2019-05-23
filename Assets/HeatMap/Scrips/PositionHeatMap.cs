using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Unite
{
    //人员位置热力图
    public class PositionHeatMap : MonoBehaviour
    {
        const int scale = 100;

        //存储地板和热力图的对应关系
        Dictionary<Transform, GameObject> PainterDics = new Dictionary<Transform, GameObject>();

        float offset = 10;//此值需要根据数据量动态调节 可以调整热力图的美观性
        // Use this for initialization
        void Start()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = Vector3.one * 10;

            quad.GetComponent<MeshRenderer>().enabled = false;


            List<Position> posList = new List<Position>();
            for (int i = 0; i < 200; i++)
            {
                posList.Add(new Position(GetVal(5,6), GetVal(5,6), 0));
            }

            for (int i = 0; i < 150; i++)
            {
                posList.Add(new Position(GetVal(4, 6), GetVal(4, 6), 0));
            }

            for (int i = 0; i <600; i++)
            {
                posList.Add(new Position(GetVal(4, 7), GetVal(3, 6f), 0));
            }
           
            DrawPositionHeatmap(quad.transform,posList);
        }

        double GetVal(float min,float max)
        {
            return Random.Range(min, max);
        }

        

        //渲染房间云图  new Vector3(28.2f, 0, 13.8f);
        void DrawPositionHeatmap(Transform floor, List<Position> PositionList)
        {
            GameObject Painter;
            Texture2D tx;
            Vector3 s = Vector3.Scale(floor.GetComponent<MeshFilter>().mesh.bounds.size, floor.lossyScale);

            INT size = new INT((int)s.x * scale, (int)s.y * scale);//这里尽量把大小变成整数 有利于像素打点

            //如果第一次加载 需要实例化云图 并且设置它的层级关系 以及Transform的信息
            //如果更新 或者重新显示 不需要重新创建贴图 存下来利用就好了
            if (!PainterDics.ContainsKey(floor))
            {
                Painter = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tx = new Texture2D(size.x, size.y);
                Material m = new Material(Shader.Find("Standard"));
                m.mainTexture = tx;
                Painter.GetComponent<MeshRenderer>().material = m;
                Painter.transform.SetParent(floor);
                Painter.transform.localPosition = new Vector3(0, 0.05f, 0);//比地面高0.05f
                Painter.transform.localEulerAngles = new Vector3(90, 0, 0);//此处需要确定 模型是不是向上
                Painter.transform.localScale = new Vector3(1, 1, 1);
                PainterDics.Add(floor, Painter);
            }
            else
            {
                if (PainterDics[floor] == null)//如果被摧毁  也需要重新创建 并且把物体加入到对应的值中
                {
                    Painter = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tx = new Texture2D(size.x, size.y);
                    Material m = new Material(Shader.Find("Standard"));
                    m.mainTexture = tx;
                    Painter.GetComponent<MeshRenderer>().material = m;
                    Painter.transform.SetParent(floor.parent);
                    Painter.transform.localPosition = floor.transform.localPosition + new Vector3(0, 0.05f, 0);//比地面高0.05f
                    Painter.transform.localEulerAngles = new Vector3(90, 0, 0);//此处需要确定 模型是不是向上
                    Painter.transform.localScale = new Vector3(s.x, s.z, 1);
                    PainterDics[floor] = Painter;
                }
                else
                {
                    Painter = PainterDics[floor];
                    Painter.SetActive(true);
                    tx = Painter.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
                }
            }
            
            //存储每个像素的即有人在此位置的个数
            float[,] posArray = new float[size.x + 1, size.y + 1];
            foreach (var pos in PositionList)
            {
                PosArrayAdd(posArray, pos,size);
            }

            //计算完以后就可以打点了
            for (int i = 0; i < size.x + 1; i++)
            {
                for (int j = 0; j < size.y + 1; j++)
                {
                    tx.SetPixel(i, j, GetColor(posArray[i, j]));
                }
            }
            tx.Apply();
        }

        /// <summary>
        /// Texture打点函数
        /// </summary>
        /// <param name="posArray"></param>
        /// <param name="pos"></param>
        void PosArrayAdd(float[,] posArray, Position pos,INT size)
        {
            //由于放大scale倍  需要计算给周围点贡献值  距离越近 贡献值越大 本点贡献值加scale/2 最远点加1  我们设定贡献值范围为scale/2 
            //限制贡献范围 加入把贴图放在第一象限且有一个顶点在原点，则所有点不能离开第一象限 xy大于等于0且小于等于最大点坐标
            int minx = Mathf.Clamp(pos.x - scale / 2, 0, size.x + 1);
            int maxx = Mathf.Clamp(pos.x - scale / 2 + scale, 0, size.x + 1);

            int miny = Mathf.Clamp(pos.y - scale / 2, 0, size.y + 1);
            int maxy = Mathf.Clamp(pos.y - scale / 2 + scale, 0, size.y + 1);

            for (int i = minx; i < maxx; i++)
            {
                for (int j = miny; j < maxy; j++)
                {
                    float dis = Vector2.Distance(new Vector2(i, j), new Vector2(pos.x, pos.y));
                    if (dis <= scale / 2)
                    {
                        //距离小于等于scale / 2的  我们要插值计算贡献值 
                        posArray[i, j] += Mathf.Lerp(1, scale / 2, 1 - dis * 2 / scale);
                    }
                }
            }
        }

       
        /// <summary>
        /// 根据人数返回对应的颜色
        /// </summary>    
        /// <param name="count"></param>
        /// <returns></returns>
        Color GetColor(float count)
        {
            count = count * 2 / scale;
            Color color = new Color(1, 1, 1, 0);
            if (count > 0 && count <= offset)
            {
                color = Color.Lerp(Color.gray, Color.blue, count / offset);//new Color(0, 0.5f, 1, 1)
            }
            else if (count > offset && count <= offset * 2)
            {
                color = Color.Lerp(Color.blue, Color.yellow, (count - offset) / offset);
            }

            else if (count > 2 * offset && count <= 3 * offset)
            {
                color = Color.Lerp(Color.yellow, Color.red, (count - offset * 2) / offset);
            }

            else if (count > 3 * offset)
            {
                color = Color.red;
            }
            else
                color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0);

            return color.a == 0 ? color : new Color(color.r, color.g, color.b, 1);
        }




        /// <summary>
        /// 做位置记录的时候已经做了double转int 
        /// 设定每个像素点  有值就+1 最后根据值获取像素颜色
        /// </summary>
        public struct Position
        {
            public int x;
            public int y;
            public int z;
            public Position(double x, double y, double z)
            {
                this.x = ((int)(x * scale));
                this.y = ((int)(y * scale));
                this.z = ((int)(z * scale));
            }

            public override string ToString()
            {
                return (x+":"+y+":"+"z");
            }
        }

        

        public struct INT
        {
            public int x;
            public int y;

            public INT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
    }
}