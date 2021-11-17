using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;

/// <summary>
/// Form值类型控件标记字段是否可以被存储
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SaveFieldsConfigAttribute : Attribute
{
    /// <summary>
    /// 属性名称 
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="PropertyName">属性名称</param>
    public SaveFieldsConfigAttribute(string propertyName)
    {
        PropertyName = propertyName;
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
                    XElement element = new XElement("item", new XAttribute("Key", field.Name));
                    object fieldsObj = field.GetValue(form);
                    if (att.PropertyName != null)
                    {
                        var saveVal = fieldsObj.GetType().GetProperty(att.PropertyName).GetValue(fieldsObj);
                        if (saveVal.GetType().GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                        {
                            element.Add(string.Join(",", (saveVal as System.Collections.ICollection).Cast<object>().ToList().Select(p => p.ToString())));
                        }
                        else
                        {
                            element.Add(saveVal);//获取字段值并添加值上一级节点
                        }
                    }
                    else
                    {
                        element.Add(field.GetValue(form));//获取字段值并添加值上一级节点
                    }
                    paramElement.Add(element);//添加子节点
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("保存配置(Error)：" + form.Name+","+field.Name + "," + ex.Message);
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
                var field = tp.GetField(xele.FirstAttribute.Value, filter);//通过标记查找字段或属性
                if (field == null) continue;
                var fieldObj = field.GetValue(form);//通过实例获取字段或属性
                var att = (SaveFieldsConfigAttribute)field.GetCustomAttribute(typeof(SaveFieldsConfigAttribute));
                if (att == null) continue;

                Type prop = null;
                object val = null;
                if (att.PropertyName != null)
                {
                    prop = fieldObj.GetType().GetProperty(att.PropertyName, filter).PropertyType;
                }
                else
                {
                    prop = field.FieldType;
                }

                if (prop == typeof(int))
                {
                    val = Convert.ToInt32(xele.Value);
                }
                else if (prop == typeof(double))
                {
                    val = Convert.ToDouble(xele.Value);
                }
                else if (prop == typeof(decimal))
                {
                    val = Convert.ToDecimal(xele.Value);
                }
                else if (prop == typeof(float))
                {
                    val = Convert.ToSingle(xele.Value);
                }
                else if (prop == typeof(byte))
                {
                    val = Convert.ToByte(xele.Value);
                }
                else if (prop == typeof(bool))
                {
                    val = Convert.ToBoolean(xele.Value);
                }
                else if (prop.GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                {
                    val = (System.Collections.ICollection)(xele.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList());
                }
                else
                {
                    val = xele.Value;
                }

                if (att.PropertyName != null)
                {
                    if (fieldObj.GetType().GetProperty(att.PropertyName, filter).SetMethod != null & !prop.GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                    {
                        fieldObj.GetType().GetProperty(att.PropertyName, filter).SetValue(fieldObj, val);//字段或属性重新赋值
                    }
                    else
                    {
                        if (prop.GetInterfaces().Contains(typeof(System.Collections.ICollection)))
                        {
                            var obj = fieldObj.GetType().GetProperty(att.PropertyName, filter).GetValue(fieldObj);
                            (fieldObj.GetType().GetProperty(att.PropertyName, filter).GetValue(fieldObj) as
                              System.Collections.IList).GetType().GetMethod("Clear").Invoke(obj, null);//先清空集合
                            (val as IList<string>).Cast<object>().ToList().ForEach(item =>//使用集合的Add方法添加
                            {
                                (fieldObj.GetType().GetProperty(att.PropertyName, filter).GetValue(fieldObj) as
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
}
