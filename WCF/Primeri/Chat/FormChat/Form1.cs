using FormChat.ServiceReferenceChat;
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
using Message = FormChat.ServiceReferenceChat.Message;

namespace FormChat
{
    public partial class Form1 : Form, IChatCallback
    {
        private ChatClient proxy;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            if(proxy != null)
            {
                proxy.Close();
                proxy = null;
            }

            proxy = new ChatClient(new InstanceContext(this));
            proxy.register(txtName.Text);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            proxy.Send(new Message()
            {
                Text = txt.Text,
                To = txtTo.Text,
            });
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            var messages = proxy.MsgHistory(dtpFrom.Value, dtpTo.Value);

            foreach (var message in messages)
            {
                txtHistory.Text += $@"
{message.Timestamp}
From: {message.From}
To:{message.To}
Message:{message.Text}";
            }
        }

        public void notify(ServiceReferenceChat.Message m)
        {
            ChangeText($@"
{m.Timestamp}
From: {m.From}
To:{m.To}
Message:{m.Text}", txtHistory);
//            MessageBox.Show($@"
//{m.Timestamp}
//From: {m.From}
//To:{m.To}
//Message:{m.Text}");
        }

        public void ChangeText(string text, TextBox t)
        {
            if(t.InvokeRequired)
            {
                t.Invoke((MethodInvoker)(()=>{ ChangeText(text, t); }));
            }
            else
            {
                t.Text = text;
            }
        }
    }
}
