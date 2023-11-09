using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;

using System.Threading;
using System.IO;

namespace cs408project_client
{
    public partial class Form1 : Form
    {


        List<string> listboxitems = new List<string>();

        bool terminating = false;
        bool disconnected = false;
        bool connected = false;
        string username;


        int postID = 0;

        Socket clientSocket;
        public Form1()
        {


            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }


        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }


        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }


        private bool isanIP(string str)
        {

            int total = 0;

            for (int i = 0; i < str.Length; i++)
            {

                if(str[i] == '.')
                {


                    total++;
                }
            }
            if(total == 3)
            {

                foreach (char c in str)
                {
                    if ((c > '0' || c < '9') && c == '.')
                        return true;
                }
            }
            return false;

        }



        private void button1_Click(object sender, EventArgs e)
        {


            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox1.Text;
            username = textBox3.Text;


            int portNum;
            if (textBox1.Text == "")
            {
                logs.AppendText("Please enter an IP address!\n");
                button2.Enabled = false;
                return;
            }

            else if (!isanIP(textBox1.Text))
            {

                logs.AppendText("Please Check your IP Adress!\n");
                button1.Enabled = true;
                textBox1.Enabled = true;

            }


            else if (textBox2.Text == "")
            {

                logs.AppendText("Please enter your PortNumber!\n");

                textBox4.Enabled = false;
                button2.Enabled = false;
                textBox1.Enabled = false;

                return;
            }

            else if (!IsDigitsOnly(textBox2.Text)) {



                logs.AppendText("Please Check your PortNumber!\n");
                button1.Enabled = true;
                textBox2.Enabled = true;

            }


            else if (textBox3.Text == "")
            {

                logs.AppendText("Please enter your username!\n");
                textBox4.Enabled = false;
                button2.Enabled = false;
                textBox2.Enabled = false;
                textBox1.Enabled = false;
                return;
            }



            if (Int32.TryParse(textBox2.Text, out portNum))
            {
                try
                {
                    clientSocket.Connect(IP, portNum);
                    button1.Enabled = false;
                    connected = true;


                    checkConnection(username);

                }
                catch
                {
                    logs.AppendText("Couldn't connect to the server!\n");
                    textBox2.Enabled = true;
                    button1.Enabled = true;
                }

            }
        }

        private void checkConnection(string user_name)
        {



            try
            {
                Byte[] buffer = Encoding.Default.GetBytes(user_name);    //send username to server
                clientSocket.Send(buffer);

                Byte[] buffer_2 = new Byte[5];                          //receive feedback from the server


                clientSocket.Receive(buffer_2);                         //true or false



                string result = Encoding.Default.GetString(buffer_2);

                if (result == "TRUEE")    //if true
                {
                    button1.Enabled = false;
                    listBox1.Enabled = true;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                    textBox4.Enabled = true;
                    textBox1.Enabled = false;

                    username = user_name;

                    connected = true;
                    disconnected = false;
                    logs.AppendText("Connected to the server!\n");

                    textBox6.Enabled = true;
                    textBox5.Enabled = true;

                    button6.Enabled = true;
                    button8.Enabled = true;
                    button7.Enabled = true;
                    button5.Enabled = true;

                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;

                    Thread receiveThread = new Thread(Receive);         //we are connected to the server, we can start receiving messages
                    receiveThread.Start();



                }
                else    //if false
                {
                    logs.AppendText("The server did not accept the user name!\n");
                    button1.Enabled = true;
                    textBox1.Enabled = false;
                    textBox2.Enabled = false;
                    button4.Enabled = false;
                    button2.Enabled = false;
                    clientSocket.Close();
                    connected = false;


                }

            }



            catch
            {
                logs.AppendText("The server did not reply back about connection!\n");

            }

        }


        private void Receive()
        {
            while (connected)    //continuously check for messages sent from server
            {
                try
                {
                    Byte[] buffer2 = new Byte[2048];
                    clientSocket.Receive(buffer2);

                    string incomingMessage = Encoding.Default.GetString(buffer2);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));



                    if (incomingMessage.Substring(0, 7) == "LISTBOX")
                    {



                        incomingMessage = incomingMessage.Substring(8);

                        string[] rest = incomingMessage.Split('/');

                        rest = rest.Where(x => !string.IsNullOrEmpty(x)).ToArray();


                        foreach (var element in rest)
                        {
                            if (!listBox1.Items.Contains(element))
                            {

                                listBox1.Items.Add(element);

                            }
                        }
                    }


                    else if (incomingMessage.Substring(0, 7) == "ADDBACK")
                    {

                        incomingMessage = incomingMessage.Substring(7);


                        string[] rest = incomingMessage.Split('/');

                        listBox1.Items.Add(rest[1]);


                        logs.AppendText(rest[1] + rest[2]);


                    }


                    else if (incomingMessage.Substring(0, 9) == "Username:")
                    {


                        string[] showposts = incomingMessage.Split('/');




                        foreach (string element in showposts)
                        {


                            logs.AppendText(element);
                            logs.AppendText("\n");


                        }
                        logs.AppendText("\n");

                    }


                    else if (incomingMessage.Substring(0, 9) == "SENDERROR")
                    {


                        incomingMessage = incomingMessage.Substring(9);
                        logs.AppendText(incomingMessage);
                    }


                    else if (incomingMessage.Substring(0, 10) == "REMOVEBACK")
                    {


                        incomingMessage = incomingMessage.Substring(10);


                        string[] rest = incomingMessage.Split('/');

                        listBox1.Items.Remove(rest[1]);


                        logs.AppendText(rest[1] + rest[2] + "\n");



                    }


                }
                catch
                {
                    if (!terminating && !disconnected)  //lost connection, but disconnect button has not been clicked
                    {
                        logs.AppendText("The server has disconnected\n");

                        listBox1.Items.Clear();


                        button1.Enabled = true;
                        textBox1.Enabled = true;
                        textBox2.Enabled = true;
                        textBox3.Enabled = true;

                        clientSocket.Close();           //close connection to the socket
                        connected = false;
                    }
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {

            listBox1.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            connected = false;
            terminating = true;
            disconnected = true;
            clientSocket.Close();
            logs.AppendText("Successfully disconnected!\n");
            button1.Enabled = true;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            textBox4.Enabled = false;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
            textBox5.Enabled = false;
            textBox6.Enabled = false;
            listBox1.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {

            string username = textBox3.Text;
            string post = textBox4.Text;
            string date = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");

            logs.AppendText("You have successfully append a post! \n");
            logs.AppendText(username + ":" + post + "\n");

            string allmessage = "POST" + date + " " + "/" + username + "/" + postID + "/" + post + "/";


            Byte[] buffer3 = Encoding.Default.GetBytes(allmessage);
            clientSocket.Send(buffer3);


        }

        private void button4_Click(object sender, EventArgs e)
        {


            string message = "GETPOSTS" + username;
            logs.AppendText("Showing All Posts from clients \n");
            Byte[] buffer4 = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer4);
            

        }

        private void button5_Click(object sender, EventArgs e)   //remove friend button
        {

            object selecteditem = listBox1.SelectedItem;
            object itemafterdelete = selecteditem;
            listBox1.Items.Remove(selecteditem);
            listboxitems.Remove(selecteditem.ToString());
            string allstring = "SERVERFRIEND2/" + username + "/" + selecteditem;


            Byte[] buffer5 = Encoding.Default.GetBytes(allstring);
            clientSocket.Send(buffer5);

            logs.AppendText("You have deleted " + selecteditem.ToString() + " from your friend list");


            string toserver = "DELETEERROR" + "/" + username + "/" + " removed " + "/" + selecteditem.ToString() + "/" + " from friends \n";

            Byte[] buffer6 = Encoding.Default.GetBytes(toserver);
            clientSocket.Send(buffer6);






        }

        private void button8_Click(object sender, EventArgs e)   //add friend button


        {

           string name = textBox6.Text;


            if (name == "")
            {

                logs.AppendText("Please enter an username \n");

            }

            else if (!listBox1.Items.Contains(name) && username != name)
            {


                        string fileName = "../../user-db.txt";
            string[] lines2 = File.ReadAllLines(fileName);

                if (!lines2.Contains(name))
                {

                    logs.AppendText("The server did not accept the user name!\n");

                }
                else
                {
                    listBox1.Items.Add(name);
                    listboxitems.Add(name);

                    string allstring = "SERVERFRIEND/" + username + "/" + name;



                    Byte[] buffer6 = Encoding.Default.GetBytes(allstring);
                    clientSocket.Send(buffer6);

                                        string message = "ADDFRIEND" + "/" + username + "/" + " added " + "/" + name + "/" + " as a friend \n";
                    logs.AppendText("You have added " + name + " to your list \n");
                                        Byte[] buffer10 = Encoding.Default.GetBytes(message);
                    clientSocket.Send(buffer10);


                }

            }

            else if(listBox1.Items.Contains(name))
            {

                logs.AppendText("You have already friend with her/him \n");
            }

            else if(username == name)
            {

                logs.AppendText("you can not use your username \n");
            }
        }


        private void button6_Click(object sender, EventArgs e)    //delete postid button
        {
            string postid = textBox5.Text;
            string message = "CHECKPOSTID" + postid;

            Byte[] buffer9 = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer9);

        }

        private void button7_Click(object sender, EventArgs e)    //friends post button
        {


            logs.AppendText("Showing All Posts from friends: \n");
            string message1 = "";
            foreach(var element in listBox1.Items)

            {
                message1 += element + "/";


            }

            string message = "ALLFRIENDS" + message1;
            Byte[] buffer10 = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer10);

        }
    }


}
