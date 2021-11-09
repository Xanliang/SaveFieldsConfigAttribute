# SaveFieldsConfigAttribute
Use Attribute Save Form's  Control Property To Xml
For Windows Form Project  Use Visual Studio 2019

(1) using in Form1.Designer.cs 

        [SaveFieldsConfig("Text")]
        private System.Windows.Forms.TextBox textBox1;
        [SaveFieldsConfig("Text")]
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        [SaveFieldsConfig("Checked")]
        private System.Windows.Forms.CheckBox checkBox1;
        [SaveFieldsConfig("Checked")]
        private System.Windows.Forms.RadioButton radioButton1;
        
(2)  Save and Load XmlFile

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveFieldsConfigAttribute.SaveXmlToFile(this, this.Name + ".xml");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SaveFieldsConfigAttribute.LoadXmlFromFile(this, this.Name + ".xml");
        }

![image](https://user-images.githubusercontent.com/16300960/140856113-dbafb03f-9dbb-46c4-971e-bde44aa2829b.png)
