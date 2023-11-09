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
using System.Security.Cryptography;

namespace cs432_client
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
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }


        bool terminating = false;
        bool connected = false;
        string selected;
        byte[] password_hashed;
        string password;
        Socket clientSocket;
        string RSAPublicKey_ver = "";
        string RSAPublicKey_enc = "";
        string channel_name;
        byte[] key = new byte[16];
        byte[] iv = new byte[16];
        byte[] hmac_ch = new byte[16];
        byte[] iv_ch = new byte[16];
        byte[] aes_ch = new byte[16];
        string user_name;


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            object selectedItem = comboBox1.SelectedItem;
            if (selectedItem != null)
            {
                string selectedValue = selectedItem.ToString();
                channel_name = selectedValue;
            }

        }

        private void button1_Click(object sender, EventArgs e)  //connect button
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox3.Text;
            int portNum;
            //for the beginning below two if conditions is for getting encryption and verification key before connection process
            if (RSAPublicKey_enc == "")
            {

                using (System.IO.StreamReader fileReader = new System.IO.StreamReader("../../server_enc_dec_pub.txt"))
                {
                    RSAPublicKey_enc = fileReader.ReadLine();
                    byte[] bytes = Encoding.Default.GetBytes(RSAPublicKey_enc);
                    logs.AppendText("RSA public key as follows: " + generateHexStringFromByteArray(bytes) + "\n");
                }

            }

            if (RSAPublicKey_ver == "")
            {

                using (System.IO.StreamReader fileReader = new System.IO.StreamReader("../../server_sign_verify_pub.txt"))
                {
                    RSAPublicKey_ver = fileReader.ReadLine();
                }
            }

            if (Int32.TryParse(textBox4.Text, out portNum))
            {
                try
                {
                    clientSocket.Connect(IP, portNum);
                    button1.Enabled = false;
                    connected = true;
                    button4.Enabled = false;
                    button3.Enabled = true;
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    button2.Enabled = true;
                    logs.AppendText("Connected to the server\n");

                }
                catch
                {
                    logs.AppendText("Couldn't connect to the server!\n");
                    textBox3.Enabled = true;
                    textBox4.Enabled = true;
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    button1.Enabled = true;
                    button2.Enabled = false;
                    button4.Enabled = false;
                    connected = false;
                }

            }

            else
            {
                logs.AppendText("Check the port number\n");
            }
        }

        private void button2_Click(object sender, EventArgs e)   //disconnect button
        {
            connected = false;
            terminating = true;
            clientSocket.Close();
            logs.AppendText("Successfully disconnected!\n");
            button1.Enabled = true;
            button2.Enabled = false;
            button4.Enabled = false;
            textBox4.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
            textBox4.Enabled = true;
            button3.Enabled = false;
            button5.Enabled = false;
            textBox5.Enabled = false;
            iv_ch = new byte[16];
            aes_ch = new byte[16];
            hmac_ch = new byte[16];
        }

        private void Receive()
        {
            while (connected)    //continuously check for messages sent from server
            {
                try
                {
                    Byte[] buffer1 = new Byte[500];
                    clientSocket.Receive(buffer1);
                    string result = Encoding.Default.GetString(buffer1);

                    if (result.Substring(0, 6) == "DENEME")
                    {

                        result = result.Substring(6);
                        result = result.Trim('\0');
                        string sign = result.Substring(0, result.IndexOf("bosluk"));
                        string rest = result.Substring(result.IndexOf("bosluk") + 6);


                        byte[] signRSA = Encoding.Default.GetBytes(sign);
                        //verification of signature taken enrollment response from server
                        bool verificationResult = verifyWithRSA(rest, 3072, RSAPublicKey_ver, signRSA);


                        if (verificationResult == true)
                        {
                            logs.AppendText("Signature is verified of enrollment result\n");
                            if (rest.Contains("already"))
                            {

                                logs.AppendText("Username has already taken, try another username for enrollment or if you enrolled before you can login with your existing username \n");
                                button4.Enabled = true;
                                button2.Enabled = false;
                                button1.Enabled = false;


                            }

                            else if (rest.Contains("enrolled"))
                            {

                                logs.AppendText("You have successfully enrolled \n");
                                button1.Enabled = false;
                                button4.Enabled = true;


                            }


                        }

                        //signature is not valid
                        else
                        {
                            logs.AppendText("Signature is not verified.\n");
                            clientSocket.Close();
                            connected = false;
                        }


                    }

                    else if (result.Substring(0, 9) == "CHALLENGE")
                    {

                        result = result.Substring(9);
                        result = result.Trim('\0');
                        //successful enrollment with no already logged in client and correct entered available user credentials in database
                        if (result.Substring(0, 5) != "ERROR" && result.Substring(0,8) != "ERRORENR")
                        {
                            logs.AppendText("Taken random challenge as following: " + generateHexStringFromByteArray(Encoding.Default.GetBytes(result)) + "\n");
                            //below HMAC calculations and password hashing taken from the user is for sending to server to get a response for HMAC result
                            password_hashed = hashWithSHA512(password);

                            //lower quarter of hashed password
                            int lowerQuarterLength = password_hashed.Length / 4;

                            byte[] lowerQuarter = new byte[lowerQuarterLength];

                            Array.Copy(password_hashed, 0, lowerQuarter, 0, lowerQuarterLength);
                            //SHA 512 HMAC taken
                            byte[] hmac_result = applyHMACwithSHA512(result, lowerQuarter);

                            //convert the byte array to a hexadecimal string, and remove the dashes between each pair of hex digits using the String.Replace method.
                            logs.AppendText("HMAC GENERATED FROM CLIENT AS: " + generateHexStringFromByteArray(hmac_result) + "\n");


                            string hmac_result_asstring = Encoding.Default.GetString(hmac_result);


                            string willbe_sent = "HMAC" + hmac_result_asstring;


                            byte[] final = Encoding.Default.GetBytes(willbe_sent);

                            clientSocket.Send(final);


                        }
                        //already logged in client error
                        else if(result.Substring(0, 5) == "ERROR" && result.Substring(0, 8) != "ERRORENR")
                        {

                            logs.AppendText(result.Substring(5) + "\n");


                        }
                        //entered enrolled user before login button clicked but changing username after enrollment process
                        else if(result.Substring(0, 8) == "ERRORENR")
                        {

                            logs.AppendText(result.Substring(8) + "\n");

                        }


                    }

                    else if (result.Substring(0, 7) == "GETKEYS")
                    {


                        result = result.Substring(7);
                        result = result.Trim('\0');
                        string sign = result.Substring(0, result.IndexOf("bosluk"));
                        string rest = result.Substring(result.IndexOf("bosluk") + 6);

                        byte[] signRSA = Encoding.Default.GetBytes(sign);

                        bool verificationResult = verifyWithRSA(rest, 3072, RSAPublicKey_ver, signRSA);

                        if (verificationResult == true)
                        {

                            logs.AppendText("Signature is verified of getting channel keys result\n");
                            string[] parsed = rest.Split(new string[] { "bosluk" }, StringSplitOptions.None);
                            string pass_hash_string = Encoding.Default.GetString(password_hashed);
                            //most significant part of the hashed password taken from user
                            string significantHalf = pass_hash_string.Substring(0, password_hashed.Length / 2);

                            byte[] significantHalf_byte = Encoding.Default.GetBytes(significantHalf);

                            key = new byte[16];
                            iv = new byte[16];
                            //key and iv is taken below
                            Array.Copy(significantHalf_byte, 0, key, 0, 16); // copy the first 16 bytes to the key array
                            Array.Copy(significantHalf_byte, 16, iv, 0, 16); // copy the next 16 bytes to the IV array
                            byte[] decryption_result = decryptWithAES128(parsed[1], key, iv);
                            string decryption_result_enc = Encoding.Default.GetString(decryption_result);
                            logs.AppendText("User key as follows: " + generateHexStringFromByteArray(key) + "\n");
                            logs.AppendText("User IV as follows: " + generateHexStringFromByteArray(iv) + "\n");

                            string[] parsed2 = decryption_result_enc.Split(new string[] { "bosluk" }, StringSplitOptions.None);
                            iv_ch = Encoding.Default.GetBytes(parsed2[1]);
                            aes_ch = Encoding.Default.GetBytes(parsed2[0]);
                            hmac_ch = Encoding.Default.GetBytes(parsed2[2]);
                            bool isEmpty1 = hmac_ch.All(b => b == 0);
                            bool isEmpty2 = iv_ch.All(b => b == 0);
                            bool isEmpty3 = aes_ch.All(b => b == 0);

                            if (!isEmpty1 && !isEmpty2 && !isEmpty3)
                            {
                                button2.Enabled = true;
                                logs.AppendText(parsed[0] + "\n");
                                logs.AppendText("Channel key IV is: " + generateHexStringFromByteArray(iv_ch) + "\n");
                                logs.AppendText("Channel key AES is: " + generateHexStringFromByteArray(aes_ch) + "\n");
                                logs.AppendText("Channel key HMAC is: " + generateHexStringFromByteArray(hmac_ch) + "\n");
                                button5.Enabled = true;
                                textBox5.Enabled = true;
                                button4.Enabled = false;
                                string msg = "AUTHOK";
                                byte[] final2 = Encoding.Default.GetBytes(msg);
                                clientSocket.Send(final2);


                            }

                            else
                            {

                                logs.AppendText("Channel is unavailable\n");
                                button4.Enabled = true;



                            }
                        }

                        else
                        {


                            logs.AppendText("Signature is not verified\n");

                        }

                    }

                    else if(result.Substring(0,8) == "ALLUSERS")
                    {

                        result = result.Substring(14);
                        result = result.Trim('\0');
                        string[] parsed2 = result.Split(new string[] { "bosluk" }, StringSplitOptions.None);
                        //parsed2[0] gönderenin ismi [1] gönderilen mesajın enc hali [2] ise hmac-channel

                        byte [] hmac_validity = applyHMACwithSHA512(parsed2[1], hmac_ch);
                        logs.AppendText("HMAC RESULT OF BROADCASTING WITH CLIENT HMAC CHANNEL KEY: " + generateHexStringFromByteArray(hmac_validity) + "\n");
                        byte[] msg_decrypted = decryptWithAES128(parsed2[1], aes_ch, iv_ch);

                        //for each client if sender's specific channel are same these two client's channel hmac's should be same
                        if(parsed2[2].Equals(Encoding.Default.GetString(hmac_validity))) {


                            logs.AppendText("Client " + parsed2[0] + " is sent a message to the channel " + parsed2[3] + ": " + Encoding.Default.GetString(msg_decrypted) + "\n");


                        }

                        else
                        {


                            logs.AppendText("ALTHOUGH MESSAGE SENDER IS SAME CHANNEL ITS HMAC CHANNEL KEY IS DIFFERENT SO SENDING MESSAGE CAN NOT BE SEEN");
                        }



                    }

                    else if (result.Substring(0, 10) == "HMACRESULT")
                    {


                        result = result.Substring(10);
                        result = result.Trim('\0');
                        string sign = result.Substring(0, result.IndexOf("bosluk"));
                        string rest = result.Substring(result.IndexOf("bosluk") + 6);


                        byte[] signRSA = Encoding.Default.GetBytes(sign);

                        //try catch block is for in line 293, decryption may failed because key and iv are calculated with user entered credentials during wrong password it goes to catch block
                        try
                        {

                            bool verificationResult = verifyWithRSA(rest, 3072, RSAPublicKey_ver, signRSA);

                            if (verificationResult == true)
                            {

                                logs.AppendText("Signature is verified of hmac result\n");

                                string pass_hash_string = Encoding.Default.GetString(password_hashed);
                                //most significant part of the hashed password taken from user
                                string significantHalf = pass_hash_string.Substring(0, password_hashed.Length / 2);

                                byte[] significantHalf_byte = Encoding.Default.GetBytes(significantHalf);

                                //key and iv is taken below
                                Array.Copy(significantHalf_byte, 0, key, 0, 16); // copy the first 16 bytes to the key array
                                Array.Copy(significantHalf_byte, 16, iv, 0, 16); // copy the next 16 bytes to the IV array
                                byte[] decryption_result = decryptWithAES128(rest, key, iv);
                                logs.AppendText("User key as follows: " + generateHexStringFromByteArray(key) + "\n");
                                logs.AppendText("User IV as follows: " + generateHexStringFromByteArray(iv) + "\n");

                                string decrypted_res = Encoding.Default.GetString(decryption_result);
                                button4.Enabled = true;
                                logs.AppendText(decrypted_res + "\n");
                            }

                            else
                            {

                                logs.AppendText("Signature is not verified\n");
                                button4.Enabled = true;

                            }


                        }

                        catch
                        {


                            logs.AppendText("Login failed, Try Again\n");
                            button4.Enabled = true;
                            textBox1.Enabled = true;
                            textBox2.Enabled = true;
                            button2.Enabled = false;

                        }

                    }

                }

                catch
                {
                    if (!terminating)  //lost connection, but disconnect button has not been clicked
                    {
                        logs.AppendText("The server has disconnected\n");


                        button1.Enabled = true;
                        button2.Enabled = false;
                        button4.Enabled = false;
                        button3.Enabled = false;
                        textBox1.Enabled = true;
                        textBox1.Enabled = true;
                        textBox3.Enabled = true;
                        textBox4.Enabled = true;
                        button5.Enabled = false;
                        textBox5.Enabled = false;
                        iv_ch = new byte[16];
                        aes_ch = new byte[16];
                        hmac_ch = new byte[16];
                    }


                    clientSocket.Close();           //close connection to the socket
                    connected = false;
                }

            }

        }

        private void button4_Click(object sender, EventArgs e) //login button
        {

            user_name = textBox1.Text;
            password = textBox2.Text;


            if (user_name != "" && password != "")
            {

                string msg = "LOGINGEN" + user_name;
                byte [] buffer = Encoding.Default.GetBytes(msg);
                clientSocket.Send(buffer);
            }
            else
            {
                logs.AppendText("Enter Valid Username/Password\n");
            }



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


        static byte[] encryptWithRSA(string input, int algoLength, string xmlStringKey)
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
                //true flag is set to perform direct RSA encryption using OAEP padding
                result = rsaObject.Encrypt(byteInput, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }

        static bool verifyWithRSA(string input, int algoLength, string xmlString, byte[] signature)
        {
            // convert input string to byte array
            byte[] byteInput = Encoding.Default.GetBytes(input);
            // create RSA object from System.Security.Cryptography
            RSACryptoServiceProvider rsaObject = new RSACryptoServiceProvider(algoLength);
            // set RSA object with xml string
            rsaObject.FromXmlString(xmlString);
            bool result = false;

            try
            {
                result = rsaObject.VerifyData(byteInput, "SHA512", signature);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
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

        static byte[] decryptWithAES128(string input, byte[] key, byte[] IV)
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
            // aesObject.FeedbackSize = 128;
            // set the key
            aesObject.Key = key;
            // set the IV
            aesObject.IV = IV;
            // create an encryptor with the settings provided
            ICryptoTransform decryptor = aesObject.CreateDecryptor();
            byte[] result = null;

            try
            {
                result = decryptor.TransformFinalBlock(byteInput, 0, byteInput.Length);
            }
            catch (Exception e) // if encryption fails
            {
                Console.WriteLine(e.Message); // display the cause
            }

            return result;
        }

        private void button3_Click(object sender, EventArgs e)
        {

            user_name = textBox1.Text;
            string password = textBox2.Text;
            password_hashed = hashWithSHA512(password);

            byte[] pass = hashWithSHA512(password);
            //concatenating username password and channel name 
            string message = user_name + "bosluk" + Encoding.Default.GetString(pass) + "bosluk" + channel_name;

            //rsa encryption of user credentials
            byte[] encryptedRSA = encryptWithRSA(message, 3072, RSAPublicKey_enc);
            Console.WriteLine("RSA 3072 Encryption:");
            Console.WriteLine(generateHexStringFromByteArray(encryptedRSA));


            string sending_msg = Encoding.Default.GetString(encryptedRSA);
            sending_msg = "ENROLLMENT" + sending_msg;
            encryptedRSA = Encoding.Default.GetBytes(sending_msg);


            try
            {
                clientSocket.Send(encryptedRSA);

                Thread receiveThread = new Thread(new ThreadStart(Receive));
                receiveThread.Start();

            }


            catch
            {
                logs.AppendText("An error maybe from server has occured\n");

            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string msg = textBox5.Text;
            // encrypt its message using 128-bit AES algorithm in CBC mode with channel aes and channel iv
            byte [] enc_res = encryptWithAES128(msg, aes_ch, iv_ch);

            //HMAC of this encrypted message using the channel HMAC key
            byte[] hmac_res = applyHMACwithSHA512(Encoding.Default.GetString(enc_res), hmac_ch);


            string msg_send = "BROADCAST" + Encoding.Default.GetString(enc_res) + "bosluk" + Encoding.Default.GetString(hmac_res);

            byte[] buffer = Encoding.Default.GetBytes(msg_send);

            clientSocket.Send(buffer);


        }
    }
}
