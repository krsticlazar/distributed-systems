using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.ServiceReference1;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form, IService1Callback
    {
        private Service1Client proxy;

        public Form1()
        {
            InitializeComponent();

            proxy = new ServiceReference1.Service1Client(new InstanceContext(this));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            proxy.PrimerOneWayAsync();
        }

        public void OnPrimerCallback()
        {
            MessageBox.Show("Callback pozvan!");
        }

        public int HowLongToDelay()
        {
            int d;

            if (!int.TryParse(textBox1.Text, out d))
                d = 1;

            return d;
        }
    }
}
