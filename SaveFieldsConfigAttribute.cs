using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

/// <summary>
/// Form值类型控件标记字段是否可以被存储
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SaveFieldsConfigAttribute : Attribute
{
    /// <summary>
    /// 属性名称 
    /// </summary>
    public string[] PropertyNames { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="PropertyName">属性名称</param>
    public SaveFieldsConfigAttribute(params string[] propertyNames)
    {
        PropertyNames = propertyNames;
    }


    /// <summary>
    /// 保存Form窗体值类型数据至文件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="form">窗体实例</param>
    /// <param name="fileName">xml文件路径</param>
    /// <returns></returns>
    public static void SaveXmlToFile<T>(T form, string fileName) where T : Form
    {
        BindingFlags filter = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//过滤器
        Type tp = form.GetType();//获取Form类型
        var fields = tp.GetFields(filter);//获取共用和私有字段
        XDocument xdoc = new XDocument();//创建xml文档
        xdoc.Add(new XComment($"窗体界面({form.Text})的参数记录"));
        var paramElement = new XElement("UIInputValueList");
        xdoc.Add(paramElement);//添加父节点
        List<XElement> paramList = new List<XElement>();
        fields.ToList().ForEach((field) =>
        {
            try
            {
                var att = (SaveFieldsConfigAttribute)field.GetCustomAttributes(true).ToList().
                    Find(p => p.GetType() == typeof(SaveFieldsConfigAttribute));//寻找SaveFieldsConfigAttribute标记的字段
                if (att != null)
                {
                    object RootObj = field.GetValue(form);

                    if (att.PropertyNames != null && att.PropertyNames.Length != 0)
                    {
                        for (int n = 0; n < att.PropertyNames.Length; n++)
                        {
                            string propertyName = att.PropertyNames[n];
                            XElement element = new XElement("item", new XAttribute("Key", field.Name),
                                new XAttribute("PropertyName", propertyName));
                            var saveVal = RootObj.GetType().GetProperty(propertyName).GetValue(RootObj);
                            if (saveVal.GetType().GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                            {
                                element.Add(string.Join(",", (saveVal as System.Collections.ICollection).Cast<object>().ToList().Select(p => p.ToString())));
                            }
                            else
                            {
                                Type saveValType = saveVal.GetType();
                                element.Add(SerizerObject(saveValType, saveVal));//将对象转化为自定义字符串
                            }
                            paramElement.Add(element);//添加子节点
                        }
                    }
                    else
                    {
                        XElement element = new XElement("item", new XAttribute("Key", field.Name), new XAttribute("PropertyName", ""));
                        element.Add(SerizerObject(field.FieldType, field.GetValue(form)));//获取字段值并添加值上一级节点
                        paramElement.Add(element);//添加子节点
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("保存配置(Error)：" + form.Name + "," + field.Name + "," + ex.Message);
            }
        });
        xdoc.Add(new XComment($"DateTime：{DateTime.Now.ToString()}"));//添加备注
        xdoc.Save(fileName);//保存
    }




    /// <summary>
    /// 读取Form窗体值类型数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="form">窗体实例</param>
    /// <param name="fileName">xml文件路径</param>
    /// <returns></returns>
    public static void LoadXmlFromFile<T>(T form, string fileName) where T : Form
    {
        BindingFlags filter = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//过滤器
        Type tp = form.GetType();

        if (!System.IO.File.Exists(fileName)) return;//如果文件不存在
        // 加载用户上次输入的配置
        XDocument xdoc = XDocument.Load(fileName);
        XElement xRoot = xdoc.Element("UIInputValueList");//获取参数父节点
        foreach (XElement xele in xRoot.Elements())
        {
            try
            {
                string keyName = xele.Attributes().ToList().Find(p => p.Name == "Key").Value;//通过字段名字
                var field = tp.GetField(keyName, filter);//通过标记查找字段或属性
                if (field == null) continue;
                var fieldObj = field.GetValue(form);//通过实例获取字段或属性

                Type prop = null;
                object val = null;

                string propertyName = xele.Attributes().ToList().Find(p => p.Name == "PropertyName").Value;
                if (!string.IsNullOrEmpty(propertyName))
                {
                    prop = fieldObj.GetType().GetProperty(propertyName, filter).PropertyType;
                }
                else
                {
                    prop = field.FieldType;
                }

                val= DeserializationObject(prop, xele.Value);//将字符串反序列化成对象

                if (!string.IsNullOrEmpty(propertyName))
                {
                    if (fieldObj.GetType().GetProperty(propertyName, filter).SetMethod != null & !prop.GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                    {
                        fieldObj.GetType().GetProperty(propertyName, filter).SetValue(fieldObj, val);//字段或属性重新赋值
                    }
                    else
                    {
                        if (prop.GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                        {
                            var obj = fieldObj.GetType().GetProperty(propertyName, filter).GetValue(fieldObj);
                            (fieldObj.GetType().GetProperty(propertyName, filter).GetValue(fieldObj) as
                              System.Collections.IList).GetType().GetMethod("Clear").Invoke(obj, null);//先清空集合
                            (val as IList<string>).Cast<object>().ToList().ForEach(item =>//使用集合的Add方法添加
                            {
                                (fieldObj.GetType().GetProperty(propertyName, filter).GetValue(fieldObj) as
                                     System.Collections.IList).GetType().GetMethod("Add").Invoke(obj,
                                    new object[] { item });//再重新添加新的至集合
                            });
                        }
                    }
                }
                else
                {
                    field.SetValue(form, val);//字段或属性重新赋值
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("加载配置(Error)：" + form.Name + "," + xele.FirstAttribute.Value + "," + ex.Message);
            }
        }
    }

    /// <summary>
    /// 将实例对象转为自定义字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string SerizerObject(Type type, object saveVal)
    {
        Type saveValType = type;
        string content = "";
        if (saveValType == typeof(System.Drawing.Point))// X,Y
        {
            var tempVal = (System.Drawing.Point)saveVal;
            content = string.Format("{0},{1}", tempVal.X, tempVal.Y);//获取字段值并添加值上一级节点
        }
        if (saveValType == typeof(System.Drawing.PointF))// X,Y
        {
            var tempVal = (System.Drawing.PointF)saveVal;
            content = string.Format("{0},{1}", tempVal.X, tempVal.Y);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(System.Drawing.SizeF))//Width,Height
        {
            var tempVal = (System.Drawing.SizeF)saveVal;
            content = string.Format("{0},{1}", tempVal.Width, tempVal.Height);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(System.Drawing.Size))//Width,Height
        {
            var tempVal = (System.Drawing.Size)saveVal;
            content = string.Format("{0},{1}", tempVal.Width, tempVal.Height);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(System.Drawing.Color))//A,R,G,B
        {
            var tempVal = (System.Drawing.Color)saveVal;
            content = string.Format("{0},{1},{2},{3}", tempVal.A, tempVal.R, tempVal.G, tempVal.B);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(System.Drawing.Font))//FontName,FontSize
        {
            var tempVal = (System.Drawing.Font)saveVal;
            content = string.Format("{0},{1}", tempVal.Name, tempVal.Size);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(System.Drawing.Bitmap) || saveValType == typeof(System.Drawing.Image))//Base64String
        {
            var tempVal = (System.Drawing.Bitmap)saveVal;
            content = BitmapToBase64String(tempVal);//获取字段值并添加值上一级节点
        }
        else if (saveValType == typeof(Padding))//Left,Top,Right,Bottom
        {
            var tempVal = (Padding)saveVal;
            content = string.Format("{0},{1},{2},{3}", tempVal.Left, tempVal.Top,tempVal.Right,tempVal.Bottom);//获取字段值并添加值上一级节点
        }
        else//可直接转为字符串存储 包括枚举类型
        {
            content = saveVal.ToString();//获取字段值并添加值上一级节点
        }
        return content;
    }


    /// <summary>
    /// 将自定义字符串转为实例对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="content"></param>
    /// <returns></returns>
    public static object DeserializationObject(Type type, string content)
    {
        Type ValType = type;

        object Obj = null;
        if (ValType == typeof(int))//解析int 类型
        {
            Obj = Convert.ToInt32(content);
        }
        else if (ValType == typeof(double))//解析double 类型
        {
            Obj = Convert.ToDouble(content);
        }
        else if (ValType == typeof(decimal))//解析decimal 类型
        {
            Obj = Convert.ToDecimal(content);
        }
        else if (ValType == typeof(float))//解析float 类型
        {
            Obj = Convert.ToSingle(content);
        }
        else if (ValType == typeof(byte))//解析byte 类型
        {
            Obj = Convert.ToByte(content);
        }
        else if (ValType == typeof(bool))//解析bool 类型
        {
            Obj = Convert.ToBoolean(content);
        }
        else if (ValType == typeof(System.Drawing.Point))//解析point 类型
        {
            string[] values = content.Split(',');
            Obj = new System.Drawing.Point(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
        }
        else if (ValType == typeof(System.Drawing.PointF))//解析PointF 类型
        {
            string[] values = content.Split(',');
            Obj = new System.Drawing.PointF(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]));
        }
        else if (ValType == typeof(System.Drawing.Color))//解析Color 类型
        {
            string[] values = content.Split(',');
            Obj = System.Drawing.Color.FromArgb(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]),
                Convert.ToInt32(values[2]), Convert.ToInt32(values[3]));
        }
        else if (ValType == typeof(System.Drawing.Size))//解析Size 类型
        {
            string[] values = content.Split(',');
            Obj = new System.Drawing.Size(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
        }
        else if (ValType == typeof(System.Drawing.SizeF))//解析SizeF 类型
        {
            string[] values = content.Split(',');
            Obj = new System.Drawing.SizeF(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]));
        }
        else if (ValType == typeof(System.Drawing.Font))//解析Font 类型
        {
            string[] values = content.Split(',');
            Obj = new System.Drawing.Font(values[0], Convert.ToSingle(values[1]));
        }
        else if (ValType.IsEnum)//解析枚举 类型
        {
            Array EnumArray = ValType.GetEnumValues();
            foreach (var item in EnumArray)
            {
                if (item.ToString() == content)
                {
                    Obj = item;
                    break;
                }
            }
        }
        else if (ValType == typeof(System.Drawing.Image) || ValType == typeof(System.Drawing.Bitmap))//解析图片 类型
        {
            Obj = Base64StringToBitmap(content);
        }
        else if (ValType == typeof(Padding))//Left,Top,Right,Bottom
        {
            string[] values = content.Split(',');
            Obj = new Padding(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]),
                Convert.ToInt32(values[2]), Convert.ToInt32(values[3]));//获取字段值并添加值上一级节点
        }
        else if (ValType.GetInterfaces().Contains(typeof(System.Collections.ICollection)))//解析集合 类型
        {
            Obj = (System.Collections.ICollection)(content.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList());
        }
        else//解析字符串类型及其他
        {
            Obj = content;
        }
        return Obj;
    }


    /// <summary>
    /// 将Bitmap类型转为base64字符串
    /// </summary>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static string BitmapToBase64String(System.Drawing.Bitmap bmp)
    {
        //字面是对当前图片进行了二进制转换
        MemoryStream ms = new MemoryStream();
        bmp.Save(ms, bmp.RawFormat);
        byte[] arr = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(arr, 0, (int)ms.Length);
        ms.Close();
        //这里将arr强转Base64
        return Convert.ToBase64String(arr);
    }


    /// <summary>
    /// base64字符串转为将Bitmap类型
    /// </summary>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static System.Drawing.Bitmap Base64StringToBitmap(string base64String)
    {
        //将base64强转图片
        base64String = base64String.Replace("data:image/png;base64,", "").Replace("data:image/jgp;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");//将base64头部信息替换
        byte[] bytes = Convert.FromBase64String(base64String);
        MemoryStream ms = new MemoryStream(bytes);
        System.Drawing.Image mImage = System.Drawing.Image.FromStream(ms);
        return new System.Drawing.Bitmap(ms);
    }

}
