  /// <summary>
    /// Formֵ���Ϳؼ�����ֶ��Ƿ���Ա��洢
    /// </summary>

    [AttributeUsage(AttributeTargets.Field)]
    public class SaveFieldsConfigAttribute : Attribute
    {
        /// <summary>
        /// �������� 
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="PropertyName">��������</param>
        public SaveFieldsConfigAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }


        /// <summary>
        /// ����Form����ֵ�����������ļ�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="form">����ʵ��</param>
        /// <param name="fileName">xml�ļ�·��</param>
        /// <returns></returns>
        public static bool SaveXmlToFile<T>(T form, string fileName) where T : Form
        {
            try
            {
                BindingFlags filter = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//������
                Type tp = form.GetType();//��ȡForm����
                var fields = tp.GetFields(filter);//��ȡ���ú�˽���ֶ�
                XDocument xdoc = new XDocument();//����xml�ĵ�
                xdoc.Add(new XComment($"�������({form.Text})�Ĳ�����¼"));
                var paramElement = new XElement("UIInputValueList");
                xdoc.Add(paramElement);//��Ӹ��ڵ�
                List<XElement> paramList = new List<XElement>();
                fields.ToList().ForEach((field) =>
                {
                    var att = (SaveFieldsConfigAttribute)field.GetCustomAttributes(true).ToList().
                    Find(p => p.GetType() == typeof(SaveFieldsConfigAttribute));//Ѱ��SaveFieldsConfigAttribute��ǵ��ֶ�
                    if (att != null)
                    {
                        XElement element = new XElement("item", new XAttribute("Key", field.Name));
                        object fieldsObj = field.GetValue(form);
                        if (att.PropertyName != null)
                        {
                            element.Add(fieldsObj.GetType().GetProperty(att.PropertyName).GetValue(fieldsObj));//��ȡ�ֶ�ֵ�����ֵ��һ���ڵ�
                        }
                        else
                        {
                            element.Add(field.GetValue(form));//��ȡ�ֶ�ֵ�����ֵ��һ���ڵ�
                        }
                        paramElement.Add(element);//����ӽڵ�
                    }
                });
                xdoc.Add(new XComment($"DateTime��{DateTime.Now.ToString()}"));//��ӱ�ע
                xdoc.Save(fileName);//����
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// ��ȡForm����ֵ��������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="form">����ʵ��</param>
        /// <param name="fileName">xml�ļ�·��</param>
        /// <returns></returns>
        public static bool LoadXmlFromFile<T>(T form, string fileName) where T : Form
        {
            try
            {
                BindingFlags filter = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;//������
                Type tp = form.GetType();
                // �����û��ϴ����������
                XDocument xdoc = XDocument.Load(fileName);
                XElement xRoot = xdoc.Element("UIInputValueList");//��ȡ�������ڵ�
                foreach (XElement xele in xRoot.Elements())
                {
                    var field = tp.GetField(xele.FirstAttribute.Value, filter);//ͨ����ǲ����ֶλ�����
                    if (field == null) continue;
                    var fieldObj = field.GetValue(form);//ͨ��ʵ����ȡ�ֶλ�����
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
                        fieldObj.GetType().GetProperty(att.PropertyName, filter).SetValue(fieldObj, val);//�ֶλ��������¸�ֵ
                    }
                    else
                    {
                        field.SetValue(form, val);//�ֶλ��������¸�ֵ
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