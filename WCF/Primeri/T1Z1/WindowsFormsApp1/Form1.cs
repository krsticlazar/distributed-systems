using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.ServiceReferenceTest;
using System.ServiceModel;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public ServiceVremeClient proxy;
        int counter = 0;

        public ServiceVremeClient Proxy
        {
            get
            {
                return proxy;
            }
            set
            {
                if (value == null)
                    throw new Exception("");
                proxy = value;
            }
        }

        public Form1()
        {
            InitializeComponent();

            proxy = new ServiceVremeClient();
            //        new InstanceContext(this));
            Console.WriteLine(proxy.GetDate());
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnGetData_Click(object sender, EventArgs e)
        {
            //txtR.Text = proxy.GetData(int.Parse(txt1.Text));
            txtR.BackColor = Color.Yellow;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //var result = proxy.GetDataUsingDataContract(new CompositeType()
            //{
            //    BoolValue = (counter%2 == 0),
            //    StringValue = txt2.Text,
            //    PublicField = 1
            //});
            //counter++;
            //txtR.BackColor = Color.LimeGreen;
            //txtR.Text = result.StringValue;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtR.Text = proxy.GetDate().ToString() + "\t\t\t" + proxy.GetBrojPoziva().ToString();
        }
    }
}
