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
        public static bool SaveXmlToFile<T>(T form, string fileName) where T : Form
        {
            try
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
                    var att = (SaveFieldsConfigAttribute)field.GetCustomAttributes(true).ToList().
                    Find(p => p.GetType() == typeof(SaveFieldsConfigAttribute));//寻找SaveFieldsConfigAttribute标记的字段
                    if (att != null)
                    {
                        XElement element = new XElement("item", new XAttribute("Key", field.Name));
                        object fieldsObj = field.GetValue(form);
                        if (att.PropertyName != null)
                        {
                            element.Add(fieldsObj.GetType().GetProperty(att.PropertyName).GetValue(fieldsObj));//获取字段值并添加值上一级节点
                        }
                        else
                        {
                            element.Add(field.GetValue(form));//获取字段值并添加值上一级节点
                        }
                        paramElement.Add(element);//添加子节点
                    }
                });
                xdoc.Add(new XComment($"DateTime：{DateTime.Now.ToString()}"));//添加备注
                xdoc.Save(fileName);//保存
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 读取Form窗体值类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="form">窗体实例</param>
        /// <param name="fileName">xml文件路径</param>
        /// <returns></returns>
        public static bool LoadXmlFromFile<T>(T form, string fileName) where T : Form
        {
            try
            {
                BindingFlags filter = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//过滤器
                Type tp = form.GetType();
                // 加载用户上次输入的配置
                XDocument xdoc = XDocument.Load(fileName);
                XElement xRoot = xdoc.Element("UIInputValueList");//获取参数父节点
                foreach (XElement xele in xRoot.Elements())
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
                    else
                    {
                        val = xele.Value;
                    }

                    if (att.PropertyName != null)
                    {
                        fieldObj.GetType().GetProperty(att.PropertyName, filter).SetValue(fieldObj, val);//字段或属性重新赋值
                    }
                    else
                    {
                        field.SetValue(form, val);//字段或属性重新赋值
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }