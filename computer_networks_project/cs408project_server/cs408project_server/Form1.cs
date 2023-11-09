using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace cs408project_server
{
    public partial class Form1 : Form

    {


        struct Socket
        {
            public string userName;
            public System.Net.Sockets.Socket clientSocket;
            public Socket(string name, System.Net.Sockets.Socket socket) { userName = name; clientSocket = socket; }
        }



        struct friends

        {


            public List<string> userlist;
            public string user;
            public friends(string User, List<string> Userlist) { user = User;  userlist = Userlist; }
        }



        System.Net.Sockets.Socket serverSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();
        List<string> clientNames = new List<string>();

        List<friends> allfriends = new List<friends>();

        List<friends> fordeleted = new List<friends>();



        bool terminating = false;
        bool listening = false;
        string user_name = "";

        int postid = File.ReadAllLines("../../posts.log.txt").Length;




        public Form1()
        {

            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }


        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int serverPort;

            if (Int32.TryParse(TextBox1.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(3);

                listening = true;
                button1.Enabled = false;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Check port number!\n");
            }
        }


        private void Accept()
        {
            while (listening)
            {
                try
                {
                    System.Net.Sockets.Socket newClient = serverSocket.Accept();
                    CheckDB(newClient);
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void Receive(Socket thisClient)
        {
            bool connected = true;
            var lines = new List<string>();


            while (connected && !terminating)
            {
                try
                {

                    Byte[] buffer11 = new Byte[64];
                    thisClient.clientSocket.Receive(buffer11);

                    string incomingMessage = Encoding.Default.GetString(buffer11);
                    incomingMessage = incomingMessage.Trim('\0');



                    if (incomingMessage.Substring(0, 4) == "POST")
                    {
                        AddPosts(incomingMessage.Substring(4));

                        incomingMessage = incomingMessage.Substring(4);

                        string[] findpost = incomingMessage.Split('/');

                        logs.AppendText(findpost[1] + " has sent a post: \n");
                        logs.AppendText(findpost[3] + "\n");


                    }

                    else if (incomingMessage.Substring(0, 8) == "GETPOSTS")


                    {

                        incomingMessage = incomingMessage.Substring(8);
                        logs.AppendText("Showing All Posts for: \n");
                        logs.AppendText(incomingMessage + "\n");


                        using (StreamReader reader = new StreamReader("../../posts.log.txt"))
                        {
                            var line = reader.ReadLine();




                            while (line != null)
                            {
                                lines.Add(line);
                                line = reader.ReadLine();
                            }
                            reader.Close();
                        }




                        foreach (string line in lines)
                        {


                            string totalmessage = "";
                            string[] eachline = line.Split('/');
                            string name = eachline[1];


                            if (name != incomingMessage)
                            {


                                totalmessage = "Username:" + name + "/" + "PostID:" + eachline[2] + "/" + "Post:" + eachline[3] + "/" + "Time:" + eachline[0];
                                Byte[] buffer12 = Encoding.Default.GetBytes(totalmessage);
                                thisClient.clientSocket.Send(buffer12);







                            }


                        }

                        lines.Clear();




                    }

                    else if (incomingMessage.Substring(0, 9) == "ADDFRIEND")
                    {

                        string tmessage = "";

                        incomingMessage = incomingMessage.Substring(9);


                        string[] rest = incomingMessage.Split('/');


                        logs.AppendText(rest[1] + rest[2] + rest[3] + rest[4]);


                        foreach (var element in clientSockets)
                        {



                            if (element.userName == rest[3])
                            {

                                tmessage = "ADDBACK" + "/" + thisClient.userName + "/" + " has added you as friend \n";
                                Byte[] buffer13 = Encoding.Default.GetBytes(tmessage);
                                element.clientSocket.Send(buffer13);

                            }
                        }

                    }

                    else if (incomingMessage.Substring(0, 10) == "ALLFRIENDS")
                    {


                        logs.AppendText("Showing All Friends Post for: \n");
                        logs.AppendText(thisClient.userName + "\n");



                        incomingMessage = incomingMessage.Substring(10);

                        string[] restmessage = incomingMessage.Split('/');

                        string line = null;

                        using (StreamReader reader = new StreamReader("../../posts.log.txt"))
                        {

                            while ((line = reader.ReadLine()) != null)
                            {
                                string totalmessage = "";

                                string[] compare = line.Split('/');

                                foreach (var element in restmessage)
                                {



                                    if (element == compare[1])
                                    {

                                        totalmessage = "Username: " + element + "/" + "PostID: " + compare[2] + "/" + "Post: " + compare[3] + "/" + "Time: " + compare[0];
                                        Byte[] buffer15 = Encoding.Default.GetBytes(totalmessage);
                                        thisClient.clientSocket.Send(buffer15);


                                    }



                                }



                            }




                        }




                    }


                    else if (incomingMessage.Substring(0, 11) == "DELETEERROR")
                    {


                        string tmessage = "";

                        incomingMessage = incomingMessage.Substring(11);


                        string[] rest = incomingMessage.Split('/');


                        logs.AppendText(rest[1] + rest[2] + rest[3] + rest[4]);


                        foreach (var element in clientSockets)
                        {



                            if (element.userName == rest[3])
                            {


                                tmessage = "REMOVEBACK" + "/" + rest[1] + "/" + " has removed you from friend list \n";
                                Byte[] buffer14 = Encoding.Default.GetBytes(tmessage);
                                element.clientSocket.Send(buffer14);

                            }
                        }




                    }


                    else if (incomingMessage.Substring(0, 11) == "CHECKPOSTID")
                    {

                        incomingMessage = incomingMessage.Substring(11);
                        int number = Convert.ToInt32(incomingMessage);

                        string line = null;

                        bool check = false;
                        string foundline = "";

                        if (number < postid)
                        {

                            using (var sr = new StreamReader("../../posts.log.txt"))  //for finding line with postid
                            {
                                for (int i = 0; i < postid; i++)
                                {

                                    if (i == number)
                                    {
                                        foundline = sr.ReadLine();
                                        check = true;
                                        break;

                                    }
                                    sr.ReadLine();

                                }
                            }

                            if (check)

                            {
                                string[] latestcurrentline = foundline.Split('/');

                                if (latestcurrentline[1] == thisClient.userName)
                                {


                                    //burada iç içe kullanmak için bir daha bakarız da onun yerine writerları bir array de tutup streamwriter ile en baştan yazmak olabilir

                                    List<string> totallines = new List<string>();

                                    using (StreamReader reader = new StreamReader("../../posts.log.txt"))
                                    {

                                        while ((line = reader.ReadLine()) != null)
                                        {



                                            totallines.Add(line);
                                        }
                                    }


                                    File.WriteAllText("../../posts.log.txt", String.Empty);

                                    using (StreamWriter writer = new StreamWriter("../../posts.log.txt"))
                                    {

                                        for (int i = 0; i < totallines.Count; i++)
                                        {

                                            if (i == number)
                                            {
                                                logs.AppendText(thisClient.userName + "deleted post with post number: " + number + "\n");
                                                string message = "SENDERROR" + "You have deleted post with postID: " + number + "\n";
                                                Byte[] buffer16 = Encoding.Default.GetBytes(message);
                                                thisClient.clientSocket.Send(buffer16);


                                                continue;
                                            }

                                            else if (i < number)
                                            {

                                                writer.WriteLine(totallines[i]);


                                            }

                                            else
                                            {

                                                string[] result = totallines[i].Split('/');
                                                int changeid = Convert.ToInt32(result[2]);
                                                changeid--;
                                                result[2] = changeid.ToString();

                                                string totalstr = "";

                                                for (int a = 0; a < result.Length; a++)
                                                {

                                                    if (a + 1 != result.Length)
                                                    {
                                                        totalstr += result[a] + "/";
                                                    }

                                                    else
                                                    {
                                                        continue;
                                                    }


                                                }

                                                writer.WriteLine(totalstr);

                                            }


                                        }
                                    }



                                }

                                else
                                {

                                    string message = "SENDERROR" + "Post with id " + incomingMessage + " is not yours! \n";
                                    Byte[] buffer17 = Encoding.Default.GetBytes(message);
                                    thisClient.clientSocket.Send(buffer17);

                                }

                            }

                        }

                        else
                        {


                            string message = "SENDERROR" + "There is no post with postID:" + number + "\n";
                            Byte[] buffer3 = Encoding.Default.GetBytes(message);
                            thisClient.clientSocket.Send(buffer3);



                        }



                    }


                    else if (incomingMessage.Substring(0, 13) == "SERVERFRIEND/")
                    {



                        incomingMessage = incomingMessage.Substring(13);


                        string currentname = incomingMessage.Substring(0, incomingMessage.IndexOf('/'));


                        incomingMessage = incomingMessage.Substring(incomingMessage.IndexOf('/'));

                        string[] rest = incomingMessage.Split('/');


                        if (allfriends.Count == 0)
                        {


                            List<string> somelist = new List<string>();
                            somelist.Add(rest[1]);
                            friends willadded = new friends(currentname, somelist);
                            allfriends.Add(willadded);

                            friends willadded1 = new friends(currentname, somelist);
                            fordeleted.Add(willadded1);
                        }

                        else
                        {

                            int count = 0;
                            bool check = false;

                            foreach (var friendelement in allfriends)
                            {


                                if (friendelement.user == currentname)
                                {
                                    friendelement.userlist.Add(rest[1]);
                                    check = true;


                                    List<string> somelist1 = new List<string>();
                                    somelist1.Add(rest[1]);
                                    friends willadded1 = new friends(currentname, somelist1);
                                    fordeleted.Add(willadded1);


                                }

                                count++;

                            }

                            if (count == allfriends.Count && !check)
                            {


                                List<string> somelist = new List<string>();
                                somelist.Add(rest[1]);
                                friends willadded = new friends(currentname, somelist);
                                allfriends.Add(willadded);


                            }



                        }

                    }

                    else if (incomingMessage.Substring(0, 14) == "SERVERFRIEND2/")
                    {



                        incomingMessage = incomingMessage.Substring(14);


                        string currentname = incomingMessage.Substring(0, incomingMessage.IndexOf('/'));


                        incomingMessage = incomingMessage.Substring(incomingMessage.IndexOf('/'));

                        string[] rest = incomingMessage.Split('/');


                        foreach (var element in allfriends)
                        {


                            if (element.user == currentname)
                            {

                                for (int i = 0; i < element.userlist.Count; i++)
                                {


                                    if (element.userlist[i] == rest[1])
                                    {


                                        element.userlist.Remove(rest[1]);


                                    }
                                }

                            }
                        }

                        foreach (var element in fordeleted)
                        {
                            if (thisClient.userName == element.user)
                            {


                                foreach (var elements in element.userlist)
                                {

                                    if (elements == rest[1])
                                    {

                                        element.userlist.Remove(rest[1]);

                                    }
                                }
                            }
                        }

                    }

                }
                catch
                {
                    if (!terminating)
                    {


                        logs.AppendText(thisClient.userName + " has disconnected!\n");
                    }
                    thisClient.clientSocket.Close();
                    clientSockets.Remove(thisClient);
                    clientNames.Remove(thisClient.userName);

                    connected = false;
                }
            }
        }



        private void AddPosts(string messageinfo)
        {


            postid = File.ReadAllLines("../../posts.log.txt").Length;

            using (StreamWriter file = new StreamWriter("../../posts.log.txt", append: true))
            {


                string[] messageinfo1 = messageinfo.Split('/');

                messageinfo1[2] = postid.ToString();


                string fixmessage = messageinfo1[0] + "/" + messageinfo1[1] + "/" + messageinfo1[2] + "/" + messageinfo1[3] + "/";
                

                file.WriteLine(fixmessage);
                postid++;


            }





        }


        private void CheckDB(System.Net.Sockets.Socket newClient)
        {


            Byte[] buffer18 = new Byte[64];
            newClient.Receive(buffer18);                              //receive user name from client
            user_name = Encoding.Default.GetString(buffer18);
            user_name = user_name.Substring(0, user_name.IndexOf("\0"));


            if (clientNames.Contains(user_name))      //a client with this user name has already connected
            {


                Byte[] buffer19 = Encoding.Default.GetBytes("FALSE");
                newClient.Send(buffer19);

                logs.AppendText("Another client with the same username is already connected, client is not accepted.\n");
                newClient.Close();
            }


            else
            {


                string fileName = "../../user-db.txt";
                string[] lines = File.ReadAllLines(fileName);

                if (!lines.Contains(user_name))
                {

                    Byte[] buffer20 = Encoding.Default.GetBytes("FALSE");
                    newClient.Send(buffer20);

                    logs.AppendText("The client's username does not exists in the database, client is not accepted.\n");
                    newClient.Close();

                }
                else
                {
                    Byte[] buffer21 = Encoding.Default.GetBytes("TRUEE");
                    newClient.Send(buffer21);

                    Socket accepted_client = new Socket(user_name, newClient);
                    clientSockets.Add(accepted_client);
                    clientNames.Add(user_name);

                    logs.AppendText(user_name + " is connected.\n");

                    string message = "LISTBOX/";


                    foreach (var element in allfriends)
                    {


                        foreach (var listelements in element.userlist)
                        {


                            if (listelements == user_name)
                            {


                                message += element.user + "/";

                            }

                        }

                    }

                    foreach (var element in fordeleted)
                    {


                        foreach (var listelements in element.userlist)
                        {


                            if (element.user == user_name)
                            {

                                    message += listelements + "/";

                            }
                        }
                    }

                    Byte[] buffer22 = Encoding.Default.GetBytes(message);
                    accepted_client.clientSocket.Send(buffer22);

                    Thread receiveThread = new Thread(() => Receive(accepted_client));  //connection is successfull, start receiving messages
                    receiveThread.Start();
                }

            }


        }


    }
}
