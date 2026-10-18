using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WcfVremeClient.ServiceReferenceVreme;

namespace WcfVremeClient
{
    public partial class Form1 : Form
    {
        VremeClient proxy;

        public Form1()
        {
            InitializeComponent();

            proxy = new VremeClient();

            proxy.GetVremeCompleted += Proxy_GetVremeCompleted;
        }

        private void btnVreme_Click(object sender, EventArgs e)
        {
            textBox1.Text += proxy.GetVreme().ToString() + Environment.NewLine;
        }

        private void btnVremeA_Click(object sender, EventArgs e)
        {
            textBox1.Text += proxy.GetDataUsingDataContract(new VremeDodatno()
            {
                PrvoPokretanje = DateTime.Now
            }).StringValue;
            //proxy.GetVremeAsync();
        }

        private void Proxy_GetVremeCompleted(object sender, GetVremeCompletedEventArgs e)
        {
            textBox1.Text += "A: " + e.Result.ToString() + Environment.NewLine;
        }
    }
}
