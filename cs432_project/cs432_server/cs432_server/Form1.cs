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
using System.Security.Cryptography;

namespace cs432_server
{
    public partial class Form1 : Form
    {
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

        bool terminating = false;
        bool listening = false;

        //i generated a Client class for getting client's informations such as random challenge number for client, socket and channel name as well
        class Client
        {
            public string username;
            public string password;
            public Socket user_socket;
            public byte[] user_challenge;
            public byte[] key;
            public byte[] iv;
            public string channelName;
            public Client(string userName, string Password, Socket Socket, byte [] User_Challenge, byte[] Key, byte[] Iv, string ChannelName) { username = userName; password = Password; user_socket = Socket; user_challenge = User_Challenge; key = Key; iv = Iv; channelName = ChannelName; }
        }

        class Channel

        {

            public string channel_name;
            public byte[] channel_iv;
            public byte[] channel_hmac;
            public byte[] channel_aes;
            public List<Client> users;
            public Channel(string channel_Name, byte[] Channel_iv, byte[] Channel_aes, byte[] Channel_Hmac, List<Client> User) { channel_name = channel_Name; channel_iv = Channel_iv; channel_aes = Channel_aes; channel_hmac = Channel_Hmac; users = User; }


        }

        List<string> clientNames = new List<string>();
        List<Socket> socketlist = new List<Socket>();
        string RSA_private = "";
        string RSA_sign = "";
        //below two dictionary helps to get current user's password and channel name easily from the txt file
        Dictionary<string, string> currentusernamepass_dict = new Dictionary<string, string>();
        Dictionary<string, string> currentusernamechannel_dict = new Dictionary<string, string>();
        Channel channel1 = new Channel("IF100", new byte[16], new byte[16], new byte[16], new List<Client>());
        Channel channel2 = new Channel("MATH101", new byte[16], new byte[16], new byte[16], new List<Client>());
        Channel channel3 = new Channel("SPS101", new byte[16], new byte[16], new byte[16], new List<Client>());


        System.Net.Sockets.Socket serverSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private void button1_Click(object sender, EventArgs e)
        {

            //for enrollment i wanted to store all names inside the txt file so i can easily see the enrolled users before
            using (System.IO.StreamReader fileReader = new System.IO.StreamReader("../../usersinfo.txt"))
            {
                string line;
                while ((line = fileReader.ReadLine()) != null)
                {
                    // Split the line on the space character to extract the username and also creating username-correct password and username-channelname dictionaries in txt
                    string[] parts = line.Split(' ');
                    clientNames.Add(parts[0]);
                    currentusernamepass_dict.Add(parts[0], parts[1]);
                    currentusernamechannel_dict.Add(parts[0], parts[2]);


                }

            }

            //rsa signature and encryption private keys read as below when listen button clicked in the server
            if (RSA_private == "")
            {

                using (System.IO.StreamReader fileReader = new System.IO.StreamReader("../../server_enc_dec_pub_prv.txt"))
                {
                    RSA_private = fileReader.ReadLine();
                    byte[] bytes = Encoding.Default.GetBytes(RSA_private);
                    logs.AppendText("RSA private key as follows: " + generateHexStringFromByteArray(bytes) + "\n");
                }
            }



            if (RSA_sign == "")
            {

                using (System.IO.StreamReader fileReader = new System.IO.StreamReader("../../server_sign_verify_pub_prv.txt"))
                {
                    RSA_sign = fileReader.ReadLine();
                }
            }

            int serverPort;

            if (Int32.TryParse(textBox2.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(10);

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

                    socketlist.Add(serverSocket.Accept());
                    Thread receiveThread = new Thread(new ThreadStart(Receive));
                    receiveThread.Start();

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

        private void Receive()
        {
            bool connected = true;
            //current client socket can be taken as below
            Socket clientsocket = socketlist[socketlist.Count - 1];
            //a default Client object is taken as below with current client's socket and other empty values
            Client current_client = new Client("", "", clientsocket, new byte[0], new byte[16], new byte[16], "");
            while (connected && !terminating)
            {
                try
                {
                    Byte[] buffer1 = new Byte[500];
                    current_client.user_socket.Receive(buffer1);
                    string result = Encoding.Default.GetString(buffer1);

                    if (result.Substring(0, 10) == "ENROLLMENT")
                    {

                        result = result.Substring(10);
                        //trims is for deleting extra characters at the end of the taken string
                        result = result.Trim('\0');
                        //decrypting enrollment request from client with RSA
                        byte[] decryptedByteArray = decryptWithRSA(result, 3072, RSA_private);
                        string taken_msg = Encoding.Default.GetString(decryptedByteArray);

                        string[] parsed = taken_msg.Split(new string[] { "bosluk" }, StringSplitOptions.None);

                        if (!clientNames.Contains(parsed[0]))
                        {

                            current_client.username = parsed[0];
                            string txt_password = parsed[1];
                            byte[] byte_pass = Encoding.Default.GetBytes(parsed[1]);
                            //i needed to store the hashed password as hexadecimal because when i stored as string then because of some empty character, hashed password is seperated
                            string txt_password_new = generateHexStringFromByteArray(byte_pass);
                            string txt_channel_name = parsed[2];
                            clientNames.Add(parsed[0]);

                            currentusernamepass_dict.Add(parsed[0], txt_password_new);
                            currentusernamechannel_dict.Add(parsed[0], parsed[2]);
                            using (StreamWriter writer = new StreamWriter("../../usersinfo.txt", append: true))
                            {

                                writer.WriteLine(current_client.username + " " + txt_password_new + " " + txt_channel_name);

                            }
                            logs.AppendText("New client " + current_client.username + " is enrolled.\n");
                            //"enrolled" keyword for giving information to the user for the enrollment process is successful and below i took the signature of this word
                            byte[] signature = signWithRSA("enrolled", 3072, RSA_sign);
                            logs.AppendText("Digital signature of enrollment result: " + generateHexStringFromByteArray(signature) + "\n");
                            string sign_response = Encoding.Default.GetString(signature);
                            byte[] buffer = Encoding.Default.GetBytes("DENEME" + sign_response + "bosluk" + "enrolled");
                            current_client.user_socket.Send(buffer);
                            //channel addition to the specific channel name of the current client as below if the client is new enrolled
                            foreach (KeyValuePair<string, string> entry in currentusernamechannel_dict)
                            {

                                if (current_client.username == entry.Key && entry.Value == "IF100" && !channel1.users.Contains(current_client))
                                {
                                    current_client.channelName = "IF100";
                                    channel1.users.Add(current_client);

                                }

                                else if (current_client.username == entry.Key && entry.Value == "MATH101" && !channel2.users.Contains(current_client))
                                {
                                    current_client.channelName = "MATH101";
                                    channel2.users.Add(current_client);

                                }


                                else if (current_client.username == entry.Key && entry.Value == "SPS101" && !channel3.users.Contains(current_client))
                                {
                                    current_client.channelName = "SPS101";
                                    channel3.users.Add(current_client);

                                }


                            }


                        }

                        else
                        {
                            //if already enrolled so username, password and channel name can be put as taken credentials from the current user
                            current_client.username = parsed[0];
                            //"already" keyword for giving information to the user for the enrollment process is not successful and below i took the signature of this word
                            byte[] signature = signWithRSA("already", 3072, RSA_sign);

                            //i put some AppendTexts as below to show the output inside the richtextbox in server GUI, and below one is to show the hexadecimal value of signature
                            logs.AppendText("Digital signature of enrollment result: " + generateHexStringFromByteArray(signature) + "\n");
                            string sign_response = Encoding.Default.GetString(signature);
                            byte[] buffer = Encoding.Default.GetBytes("DENEME" + sign_response + "bosluk" + "already");
                            current_client.user_socket.Send(buffer);
                            logs.AppendText("Another client with the same username is already enrolled. So the client can login or if want he/she can use different username for enrollment\n");

                        }

                    }

                    else if (result.Substring(0, 8) == "LOGINGEN")
                    {

                        result = result.Substring(8);
                        result = result.Trim('\0');
                        current_client.username = result;

                        //channel addition to the specific channel name of the current client as below if the client is already enrolled
                        foreach (KeyValuePair<string, string> entry in currentusernamechannel_dict)
                        {

                            if (current_client.username == entry.Key && entry.Value == "IF100" && !channel1.users.Contains(current_client))
                            {
                                current_client.channelName = "IF100";
                                channel1.users.Add(current_client);

                            }

                            else if (current_client.username == entry.Key && entry.Value == "MATH101" && !channel2.users.Contains(current_client))
                            {
                                current_client.channelName = "MATH101";
                                channel2.users.Add(current_client);

                            }


                            else if (current_client.username == entry.Key && entry.Value == "SPS101" && !channel3.users.Contains(current_client))
                            {
                                current_client.channelName = "SPS101";
                                channel3.users.Add(current_client);

                            }


                        }

                        //going back to enrollment phase in the case of changing username before login and after enrollment success with valid username that exists inside database users.
                        if (!currentusernamepass_dict.ContainsKey(result))
                        {

                            string str2 = "Username that you entered is not an enrolled user so please enroll firstly.";
                            string str = "CHALLENGE" + "ERRORENR" + str2;
                            byte[] msg = Encoding.Default.GetBytes(str);
                            current_client.user_socket.Send(msg);

                        }
                       
                        //i wanted to check the online users list from the listbox if contains error message will be shown both server and client
                        else if (!listBox1.Items.Contains(result))
                        {
                            //server generates and sends a 128-bit random number to the current client as below then we can assign the user_challenge
                            current_client.user_challenge = Challenge();

                            //below one is to show the current user's random challenge number in hexadecimal format in server GUI
                            logs.AppendText("For client: " + current_client.username + " following challenge number is generated: " + generateHexStringFromByteArray(current_client.user_challenge) + "\n");
                            string str = Encoding.Default.GetString(current_client.user_challenge);
                            str = "CHALLENGE" + str;
                            byte[] msg = Encoding.Default.GetBytes(str);

                            current_client.user_socket.Send(msg);

                        }

                        else
                        {


                            logs.AppendText("Client with this username is already logged in \n");
                            string str = "Client with this username is already logged in";
                            str = "CHALLENGE" + "ERROR" + str;
                            byte[] msg = Encoding.Default.GetBytes(str);
                            current_client.user_socket.Send(msg);
                        }

                    }

                    else if(result.Substring(0,6) == "AUTHOK")
                    {

                        if (!listBox1.Items.Contains(current_client.username))
                        {
                            listBox1.Items.Add(current_client.username);
                            logs.AppendText("AUTHENTICATION SUCCESSFUL\n" + current_client.username + " is authenticated.\n");

                        }
                    }

                    //in order to accomplish end to end communication server takes the broadcast request and gives this to the users that belongs to sender client's channel
                    else if(result.Substring(0,9) == "BROADCAST")
                    {

                        result = result.Substring(9);
                        result = result.Trim('\0');
                        string again_msg = "ALLUSERS" + "bosluk" + current_client.username + "bosluk" + result + "bosluk" + current_client.channelName;
                        byte[] buffer = Encoding.Default.GetBytes(again_msg);
                        //broadcasting to all clients which is in same channel as below

                        if(current_client.channelName == "IF100")
                        {


                            logs1.AppendText("Message is received from " + current_client.username + "\n");
                        }

                        else if(current_client.channelName == "MATH101")
                        {

                            logs2.AppendText("Message is received from " + current_client.username + "\n");


                        }

                        else if (current_client.channelName == "SPS101")
                        {

                            logs3.AppendText("Message is received from " + current_client.username + "\n");


                        }
                        foreach (KeyValuePair<string, string> entry in currentusernamechannel_dict)
                        {


                            if(entry.Key == current_client.username)
                            {


                                if(entry.Value == "IF100")
                                {


                                    foreach(Client client in channel1.users)
                                    {

                                        logs1.AppendText("Broadcast message received from " + current_client.username + " is relayed to " + client.username + "\n");
                                        client.user_socket.Send(buffer);
                                    }
                                }
                                else if(entry.Value == "MATH101")
                                {
                                    foreach (Client client in channel2.users)
                                    {

                                        logs2.AppendText("Broadcast message received from " + current_client.username + " is relayed to " + client.username + "\n");
                                        client.user_socket.Send(buffer);
                                    }

                                }
                                else if(entry.Value == "SPS101")
                                {


                                    foreach (Client client in channel3.users)
                                    {

                                        logs3.AppendText("Broadcast message received from " + current_client.username + " is relayed to " + client.username + "\n");
                                        client.user_socket.Send(buffer);
                                    }
                                }

                            }
                        }



                    }
                    else if (result.Substring(0,4) == "HMAC")
                    {



                        result = result.Substring(4);
                        result = result.Trim('\0');

                        //below line is for getting current client password which put to the txt during enrollment with dictionary method, i preferred key value pair as key username and password is the value
                        foreach (KeyValuePair<string, string> entry in currentusernamepass_dict)
                        {

                            if (current_client.username == entry.Key)
                            {
                                byte[] byte_pass1 = hexStringToByteArray(entry.Value);

                                current_client.password = Encoding.Default.GetString(byte_pass1);

                            }
                        }

                        //so by taking current client password from the txt file then start calculating hmac from its correct password
                        byte[] byte_pass = Encoding.Default.GetBytes(current_client.password);

                        int lowerQuarterLength = byte_pass.Length / 4;

                        byte[] lowerQuarter = new byte[lowerQuarterLength];

                        Array.Copy(byte_pass, 0, lowerQuarter, 0, lowerQuarterLength);

                        string challenge_str = Encoding.Default.GetString(current_client.user_challenge);

                        byte[] hmacsha512 = applyHMACwithSHA512(challenge_str, lowerQuarter);

                        string hmacsha512_str = Encoding.Default.GetString(hmacsha512);

                        string willbe_sent_msg;

                        //significant half bits of hashed password which is 0 to 255 256 bits out of 512 bits ahshed SHA-512 password
                        string significantHalf = current_client.password.Substring(0, current_client.password.Length / 2);

                        byte[] significantHalf_byte = Encoding.Default.GetBytes(significantHalf);

                        Array.Copy(significantHalf_byte, 0, current_client.key, 0, 16); // copy the first 16 bytes to the key array
                        Array.Copy(significantHalf_byte, 16, current_client.iv, 0, 16); // copy the next 16 bytes to the IV array


                        logs.AppendText("HMAC value generated by server for the correct user credentials as follows: " + generateHexStringFromByteArray(hmacsha512) + "\n");


                        if (hmacsha512_str.Equals(result))
                        {

                            willbe_sent_msg = "Authentication Successful";
                            logs.AppendText("User " + current_client.username + " key as follows: " + generateHexStringFromByteArray(current_client.key) + "\n");
                            logs.AppendText("User " + current_client.username + " IV as follows: " + generateHexStringFromByteArray(current_client.iv) + "\n");
                            //if current client belongs to IF100 channel
                            if (channel1.users.Contains(current_client))
                            {
                                //encrypting the keys
                                byte[] encrypted_keys = encryptWithAES128(Encoding.Default.GetString(channel1.channel_aes) + "bosluk" + Encoding.Default.GetString(channel1.channel_iv) + "bosluk" + Encoding.Default.GetString(channel1.channel_hmac), current_client.key, current_client.iv);

                                //appending successful to encryted keys and signs a signature
                                string send_string = "Authentication Successful" + "bosluk" + Encoding.Default.GetString(encrypted_keys);
                                //sign the concatenated message using the signing RSA key of the server.
                                byte[] signature = signWithRSA(send_string, 3072, RSA_sign);

                                logs.AppendText("Digital signature of Authentication Successful response: " + generateHexStringFromByteArray(signature) + "\n");

                                string sign_response = Encoding.Default.GetString(signature);

                                byte[] buffer = Encoding.Default.GetBytes("GETKEYS" + sign_response + "bosluk" + send_string);

                                current_client.user_socket.Send(buffer);
                            }

                            //if current client belongs to MATH101 channel
                            else if (channel2.users.Contains(current_client))
                            {
                                //encrypting the keys
                                byte[] encrypted_keys = encryptWithAES128(Encoding.Default.GetString(channel2.channel_aes) + "bosluk" + Encoding.Default.GetString(channel2.channel_iv) + "bosluk" + Encoding.Default.GetString(channel2.channel_hmac), current_client.key, current_client.iv);

                                //appending successful to encryted keys and signs a signature
                                string send_string = "Authentication Successful" + "bosluk" + Encoding.Default.GetString(encrypted_keys);
                                //sign the concatenated message using the signing RSA key of the server.
                                byte[] signature = signWithRSA(send_string, 3072, RSA_sign);

                                logs.AppendText("Digital signature of Authentication Successful response: " + generateHexStringFromByteArray(signature) + "\n");

                                string sign_response = Encoding.Default.GetString(signature);

                                byte[] buffer = Encoding.Default.GetBytes("GETKEYS" + sign_response + "bosluk" + send_string);

                                current_client.user_socket.Send(buffer);


                            }

                            //if current client belongs to SPS101 channel
                            else if (channel3.users.Contains(current_client))
                            {
                                //encrypting the keys
                                byte[] encrypted_keys = encryptWithAES128(Encoding.Default.GetString(channel3.channel_aes) + "bosluk" + Encoding.Default.GetString(channel3.channel_iv) + "bosluk" + Encoding.Default.GetString(channel3.channel_hmac), current_client.key, current_client.iv);

                                //appending successful to encryted keys and signs a signature
                                string send_string = "Authentication Successful" + "bosluk" + Encoding.Default.GetString(encrypted_keys);
                                //sign the concatenated message using the signing RSA key of the server.
                                byte[] signature = signWithRSA(send_string, 3072, RSA_sign);

                                logs.AppendText("Digital signature of Authentication Successful response: " + generateHexStringFromByteArray(signature) + "\n");

                                string sign_response = Encoding.Default.GetString(signature);

                                byte[] buffer = Encoding.Default.GetBytes("GETKEYS" + sign_response + "bosluk" + send_string);
                                current_client.user_socket.Send(buffer);
                            }

                        }

                        else
                        {

                            willbe_sent_msg = "Authentication Unsuccessful";
                            logs.AppendText("AUTHENTICATION UNSUCCESSFUL\n" + current_client.username + " is not authenticated.\n");
                            logs.AppendText("User " + current_client.username + " key as follows: " + generateHexStringFromByteArray(current_client.key) + "\n");
                            logs.AppendText("User " + current_client.username + " IV as follows: " + generateHexStringFromByteArray(current_client.iv) + "\n");
                            byte[] hmac_result = encryptWithAES128(willbe_sent_msg, current_client.key, current_client.iv);

                            string aes_ciphertext = Encoding.Default.GetString(hmac_result);
                            //sign with RSA the encrypted hmac result
                            byte[] signature = signWithRSA(aes_ciphertext, 3072, RSA_sign);
                            logs.AppendText("Digital signature of HMAC result: " + generateHexStringFromByteArray(signature) + "\n");

                            string sign_response = Encoding.Default.GetString(signature);
                            byte[] buffer = Encoding.Default.GetBytes("HMACRESULT" + sign_response + "bosluk" + aes_ciphertext);
                            clientsocket.Send(buffer);


                        }

                    }

                }

                catch
                {

                    if (!terminating)
                    {


                        logs.AppendText(current_client.username + " has disconnected!\n");
                        listBox1.Items.Remove(current_client.username);
                        //below if else's are for removing current client from its belonging channel after disconnecting
                        if (channel1.users.Contains(current_client))
                        {
                            channel1.users.Remove(current_client);

                        }
                        else if (channel2.users.Contains(current_client))
                        {

                            channel2.users.Remove(current_client);

                        }
                        else if (channel3.users.Contains(current_client))
                        {

                            channel3.users.Remove(current_client);

                        }

                    }
                    current_client.user_socket.Close();
                    connected = false;
                }


            }

        }

        static byte[] decryptWithRSA(string input, int algoLength, string xmlStringKey)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlStringKey);
            byte[] result = null;

            try
            {
                result = rsaObject.Decrypt(byteInput, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }


        static byte[] signWithRSA(string input, int algoLength, string xmlString)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlString);
            byte[] result = null;

            try
            {
                result = rsaObject.SignData(byteInput, "SHA512");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }


        static byte[] Challenge()
        {
            //16 bytes 128 bit random number will be generated as cryptographically secure
            byte[] bytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }


            return bytes;
        }

        // HMAC with SHA-512
        static byte[] applyHMACwithSHA512(string input, byte[] key)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create HMAC applier object from System.Security.Cryptography
            HMACSHA512 hmacSHA512 = new HMACSHA512(key);
            // get the result of HMAC operation
            byte[] result = hmacSHA512.ComputeHash(byteInput);

            return result;
        }

        // encryption with AES-128
        static byte[] encryptWithAES128(string input, byte[] key, byte[] IV)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);

            // create AES object from System.Security.Cryptography
            RijndaelManaged aesObject = new RijndaelManaged();
            // since we want to use AES-128
            aesObject.KeySize = 128;
            // block size of AES is 128 bits
            aesObject.BlockSize = 128;
            // mode -> CipherMode.*
            aesObject.Mode = CipherMode.CBC;
            // feedback size should be equal to block size
            aesObject.FeedbackSize = 128;
            // set the key
            aesObject.Key = key;
            // set the IV
            aesObject.IV = IV;
            // create an encryptor with the settings provided
            ICryptoTransform encryptor = aesObject.CreateEncryptor();
            byte[] result = null;

            try
            {
                result = encryptor.TransformFinalBlock(byteInput, 0, byteInput.Length);
            }
            catch (Exception e) // if encryption fails
            {
                Console.WriteLine(e.Message); // display the cause
            }

            return result;
        }


        // helper functions
        static string generateHexStringFromByteArray(byte[] input)
        {
            string hexString = BitConverter.ToString(input);
            return hexString.Replace("-", "");
        }

        public static byte[] hexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        // hash function: SHA-512
        static byte[] hashWithSHA512(string input)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create a hasher object from System.Security.Cryptography
            SHA512CryptoServiceProvider sha512Hasher = new SHA512CryptoServiceProvider();
            // hash and save the resulting byte array
            byte[] result = sha512Hasher.ComputeHash(byteInput);

            return result;
        }


        //when the generate button clicked only the keys are generated and stored inside server
        private void button2_Click(object sender, EventArgs e)
        {
            string master_secret = textBox3.Text;
            byte [] hash_result = hashWithSHA512(master_secret);
            channel1.channel_aes = new Byte[16];
            channel1.channel_iv = new Byte[16];
            channel1.channel_hmac = new Byte[16];
            Array.Copy(hash_result, 0, channel1.channel_aes, 0, 16);
            Array.Copy(hash_result, 16, channel1.channel_iv, 0, 16);
            Array.Copy(hash_result, 48, channel1.channel_hmac, 0, 16);

        }

        //when the generate button clicked only the keys are generated and stored inside server
        private void button3_Click(object sender, EventArgs e)
        {
            string master_secret = textBox4.Text;
            byte[] hash_result = hashWithSHA512(master_secret);
            channel2.channel_aes = new Byte[16];
            channel2.channel_iv = new Byte[16];
            channel2.channel_hmac = new Byte[16];
            Array.Copy(hash_result, 0, channel2.channel_aes, 0, 16);
            Array.Copy(hash_result, 16, channel2.channel_iv, 0, 16);
            Array.Copy(hash_result, 48, channel2.channel_hmac, 0, 16);

        }

        //when the generate button clicked only the keys are generated and stored inside server
        private void button4_Click(object sender, EventArgs e)
        {
        
            string master_secret = textBox5.Text;
            byte[] hash_result = hashWithSHA512(master_secret);
            channel3.channel_aes = new Byte[16];
            channel3.channel_iv = new Byte[16];
            channel3.channel_hmac = new Byte[16];
            Array.Copy(hash_result, 0, channel3.channel_aes, 0, 16);
            Array.Copy(hash_result, 16, channel3.channel_iv, 0, 16);
            Array.Copy(hash_result, 48, channel3.channel_hmac, 0, 16);

        }
    }
}
