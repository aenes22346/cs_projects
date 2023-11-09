import math
import time
import random
import sympy
import warnings
from random import randint, seed
import sys
from ecpy.curves import Curve,Point
from Crypto.Hash import SHA3_256, HMAC, SHA256
import requests
from Crypto.Cipher import AES
from Crypto import Random
from Crypto.Util.Padding import pad
from Crypto.Util.Padding import unpad
import random
import re
import json

API_URL = 'http://10.92.52.255:5000/'

stuID = 22346
#stuID = 26883
stuIDB = 2014


curve = Curve.get_curve('secp256k1')

def egcd(a, b):
    x,y, u,v = 0,1, 1,0
    while a != 0:
        q, r = b//a, b%a
        m, n = x-u*q, y-v*q
        b,a, x,y, u,v = a,r, u,v, m,n
    gcd = b
    return gcd, x, y

def modinv(a, m):
    gcd, x, y = egcd(a, m)
    if gcd != 1:
        return None  # modular inverse does not exist
    else:
        return x % m

def Setup():
    E = Curve.get_curve('secp256k1')
    return E

def KeyGen(E):
    n = E.order
    P = E.generator
    sA = randint(1,n-1)
    QA = sA*P
    return sA, QA

def SignGen(message, E, sA):
    n = E.order
    P = E.generator
    k = randint(1, n-2)
    R = k*P
    r = R.x % n
    h = int.from_bytes(SHA3_256.new(r.to_bytes((r.bit_length()+7)//8, byteorder='big')+message).digest(), byteorder='big')%n
    s = (sA*h + k) % n
    return h, s, k

def SignVer(message, h, s, E, QA):
    n = E.order
    P = E.generator
    V = s*P - h*QA
    v = V.x % n
    h_ = int.from_bytes(SHA3_256.new(v.to_bytes((v.bit_length()+7)//8, byteorder='big')+message).digest(), byteorder='big')%n
    if h_ == h:
        return True
    else:
        return False


#server's Identitiy public key
IKey_Ser = Point(93223115898197558905062012489877327981787036929201444813217704012422483432813 , 8985629203225767185464920094198364255740987346743912071843303975587695337619, curve)

def IKRegReq(h,s,x,y):
    mes = {'ID':stuID, 'H': h, 'S': s, 'IKPUB.X': x, 'IKPUB.Y': y}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "IKRegReq"), json = mes)		
    if((response.ok) == False): print(response.json())

def IKRegVerify(code):
    mes = {'ID':stuID, 'CODE': code}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "IKRegVerif"), json = mes)
    if((response.ok) == False): raise Exception(response.json())
    print(response.json())

def SPKReg(h,s,x,y):
    mes = {'ID':stuID, 'H': h, 'S': s, 'SPKPUB.X': x, 'SPKPUB.Y': y}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "SPKReg"), json = mes)		
    if((response.ok) == False): 
        print(response.json())
    else: 
        res = response.json()
        return res['SPKPUB.X'], res['SPKPUB.Y'], res['H'], res['S']

def OTKReg(keyID,x,y,hmac):
    mes = {'ID':stuID, 'KEYID': keyID, 'OTKI.X': x, 'OTKI.Y': y, 'HMACI': hmac}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "OTKReg"), json = mes)		
    print(response.json())
    if((response.ok) == False): return False
    else: return True


def ResetIK(rcode):
    mes = {'ID':stuID, 'RCODE': rcode}
    print("Sending message is: ", mes)
    response = requests.delete('{}/{}'.format(API_URL, "ResetIK"), json = mes)		
    print(response.json())
    if((response.ok) == False): return False
    else: return True

def ResetSPK(h,s):
    mes = {'ID':stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.delete('{}/{}'.format(API_URL, "ResetSPK"), json = mes)		
    print(response.json())
    if((response.ok) == False): return False
    else: return True

def ResetOTK(h,s):
    mes = {'ID':stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.delete('{}/{}'.format(API_URL, "ResetOTK"), json = mes)		
    print(response.json())

############## The new functions of phase 2 ###############

#Pseudo-client will send you 5 messages to your inbox via server when you call this function
def PseudoSendMsg(h,s):
    mes = {'ID':stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "PseudoSendMsg"), json = mes)		
    print(response.json())

#Get your messages. server will send 1 message from your inbox
def ReqMsg(h,s):
    mes = {'ID':stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.get('{}/{}'.format(API_URL, "ReqMsg"), json = mes)	
    print(response.json())	
    if((response.ok) == True): 
        res = response.json()
        return res["IDB"], res["OTKID"], res["MSGID"], res["MSG"], res["EK.X"], res["EK.Y"]

#Get the list of the deleted messages' ids.
def ReqDelMsg(h,s):
    mes = {'ID':stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.get('{}/{}'.format(API_URL, "ReqDelMsgs"), json = mes)      
    print(response.json())      
    if((response.ok) == True): 
        res = response.json()
        return res["MSGID"]

#If you decrypted the message, send back the plaintext for checking
def Checker(stuID, stuIDB, msgID, decmsg):
    mes = {'IDA':stuID, 'IDB':stuIDB, 'MSGID': msgID, 'DECMSG': decmsg}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "Checker"), json = mes)		
    print(response.json())


def SendMsg(idA, idB, otkID, msgid, msg, ekx, eky):
    mes = {"IDA": idA, "IDB": idB, "OTKID": int(otkID), "MSGID": msgid, "MSG": msg, "EK.X": ekx, "EK.Y": eky}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "SendMSG"), json=mes)
    print(response.json())    
        
def reqOTKB(stuID, stuIDB, h, s):
    OTK_request_msg = {'IDA': stuID, 'IDB':stuIDB, 'S': s, 'H': h}
    print("Requesting party B's OTK ...")
    response = requests.get('{}/{}'.format(API_URL, "ReqOTK"), json=OTK_request_msg)
    print(response.json()) 
    if((response.ok) == True):
        print(response.json()) 
        res = response.json()
        return res['KEYID'], res['OTK.X'], res['OTK.Y']
        
    else:
        return -1, 0, 0

def Status(stuID, h, s):
    mes = {'ID': stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.get('{}/{}'.format(API_URL, "Status"), json=mes)
    print(response.json())
    if (response.ok == True):
        res = response.json()
        return res['numMSG'], res['numOTK'], res['StatusMSG']

def PseudoSendMsgPH3(h,s):
    mes = {'ID': stuID, 'H': h, 'S': s}
    print("Sending message is: ", mes)
    response = requests.put('{}/{}'.format(API_URL, "PseudoSendMsgPH3"), json=mes)
    print(response.json())


E = Setup()

sA, QA = KeyGen(E)

stuID_bytes = stuID.to_bytes((stuID.bit_length()+7)//8, 'big')

h, s, k = SignGen(stuID_bytes, E, sA)




print("\nSending signature and my IKEY to server via IKRegReq() function in json format")
IKRegReq(h, s, QA.x, QA.y)


print("\nReceived the verification code through email")
code = input("Enter verification code which is sent to you: ")
print("Sending the verification code to server via IKRegVerify() function in json format")
IKRegVerify(int(code))


print("+++++++++++++++++++++++++++++++++++++++++++++")


print("Generating SPK...")


SPKA_Pri, SPKA_Pub = KeyGen(E)


SPKA_Pubx_inbytes = SPKA_Pub.x.to_bytes((SPKA_Pub.x.bit_length()+7)//8, 'big')
SPKA_Puby_inbytes = SPKA_Pub.y.to_bytes((SPKA_Pub.y.bit_length()+7)//8, 'big')
x_y_concatenating = SPKA_Pubx_inbytes + SPKA_Puby_inbytes


SPK_h, SPK_s, k = SignGen(x_y_concatenating, E, sA)


Server_Pub_x, Server_Pub_y, SPKS_h, SPKS_s = SPKReg(SPK_h, SPK_s, SPKA_Pub.x, SPKA_Pub.y)

Server_Point = Point(Server_Pub_x,Server_Pub_y,curve)

T = SPKA_Pri * Server_Point
Tx_inbytes = T.x.to_bytes((T.x.bit_length() + 7) // 8, byteorder='big')
Ty_inbytes = T.y.to_bytes((T.y.bit_length() + 7) // 8, byteorder='big')


U = b'CuriosityIsTheHMACKeyToCreativity' + Ty_inbytes + Tx_inbytes

KHMAC = SHA3_256.new(U).digest()

OTKs = {0: '', 1: '', 2: '', 3:'',4:'',5:'',6:'',7:'',8:'',9:''}
for i in range(11):
    OTKA_Pri, OTKA_Pub = KeyGen(E)    # 10 one-time public and private key pairs

    OTKs[i] = [OTKA_Pri, OTKA_Pub.x, OTKA_Pub.y]

    OTKA_Pubx_inbytes = OTKA_Pub.x.to_bytes((OTKA_Pub.x.bit_length()+7)//8, 'big')
    OTKA_Puby_inbytes = OTKA_Pub.y.to_bytes((OTKA_Pub.y.bit_length()+7)//8, 'big')
    OTKA_Pubxy_inbytes = OTKA_Pubx_inbytes + OTKA_Puby_inbytes

    ith_hmac = HMAC.new(key=KHMAC, msg=OTKA_Pubxy_inbytes, digestmod=SHA256).hexdigest()   #KHMAC is already in bytes so we passed it as key

    OTKReg(i, OTKA_Pub.x, OTKA_Pub.y, ith_hmac)



PseudoSendMsg(h,s)


# Calculating Session Key Ks
def calculate_Ks(OTKA_Pri, EKB_Pub):
    T = OTKA_Pri*EKB_Pub
    T_x_bytes = T.x.to_bytes((T.x.bit_length()+7)//8, 'big')
    T_y_bytes = T.y.to_bytes((T.y.bit_length()+7)//8, 'big')
    U = T_x_bytes + T_y_bytes + b'ToBeOrNotToBe'
    Ks = SHA3_256.new(U).digest()
    return Ks

def generateKDF(Ks):
    u1 = b'YouTalkingToMe'
    u2 = b'YouCannotHandleTheTruth'
    u3 = b'MayTheForceBeWithYou'
    list_of_keys= [] #we will keep the kenc and khmac values in the list to use it after
    for i in range(5):
        U1= Ks + u1 #concatenate KS and b'YouTalkingToMe'. KS is used at the first iteration. Then, KS will become KDFNEXT.
        KENC = SHA3_256.new(U1).digest() 
        U2= Ks + KENC + u2
        KHMAC= SHA3_256.new(U2).digest()
        U3= KENC + KHMAC + u3
        KDFNext= SHA3_256.new(U3).digest()
        Ks = KDFNext #KS becomes KDFNEXT
        list_of_keys.append([KENC, KHMAC])
    return list_of_keys


msg_list = []
for i in range(5):
    IDB, OTK_ID, msg_id, msg, EK_x, EK_y = ReqMsg(h,s)

    print(msg)
    msg_inbytes = msg.to_bytes((msg.bit_length() + 7) // 8, byteorder='big')
    print("Converted message is: ", msg_inbytes)
    print("Generating the key Ks, Kenc, & Khmac and then the HMAC value ..")
    EKB_Pub = Point(EK_x, EK_y, curve)   #i found out this in ECDH Key Exchange part in the lecture slides, "ephemeral key" appears there as "session key"
    Ks = calculate_Ks(OTKs[OTK_ID][0], EKB_Pub)   #generate KS from private part of the otk and ephemeral key, getting OTKA.Pri of returned in OTKs[OTK_ID]
    list_of_keys = generateKDF(Ks)


    nonce= msg_inbytes[:8]
    ciphertext = msg_inbytes[8:-32]
    hmac = msg_inbytes[-32:]


    K_enc = list_of_keys[i][0]

    K_hmac = list_of_keys[i][1]

    hmac_val = HMAC.new(K_hmac, digestmod=SHA256)
    hmac_val.update(ciphertext)
    hmac_val = hmac_val.digest()


    print("hmac is: ", hmac_val)


    check = False
    if(hmac_val == hmac):
        check = True
        print("HMAC value is verified!")
    else:
        check = False
        print("HMAC could not be verified!")


    #There is a note below about our output:    
    #appearantly we either got the wrong hmac_val value in line 303 or the hmac is calculating wrong in line 296 because we got "HMAC value is verified!"


    cipher = AES.new(K_enc, AES.MODE_CTR, nonce=nonce)


    decmsg = cipher.decrypt(ciphertext).decode() 
    msg_list.append({'ID': msg_id, 'MESSAGE': decmsg})
    print("The collected plaintext: ", decmsg)

    if(check):
        Checker(stuID, IDB, msg_id, decmsg)
    else:
        Checker(stuID, IDB, msg_id, "INVALIDHMAC")
        print("\n")


deleted_ids = ReqDelMsg(h,s)


print("Checking whether there were some deleted messages!! ")

for msg in msg_list:
  if msg['ID'] in deleted_ids:
    print("Message ", msg['ID'], " - Was deleted by sender - ")
  else:
    print("Message ", msg['ID'], " - ", msg['MESSAGE'])


print("Start of the main functions of phase 3 ...")

print("++++++++++++++++++++++++++++++++++++++++++++")


#i put here my teammate id number because when i put 26045 or 18007 i got point is not on curve error on thse two ids
receiverID = 26883

# receiverID = 26045

print("Now I want to send messages to my friend. Her id is ", receiverID, ". Yes she is also imaginary Signing The stuIDB of party B with my private IK")

print("Requesting party B's OTK ...")

print("Signing my stuID with my private IK")
#request messages from the pseudo client. It will send me the messages.
PseudoSendMsgPH3(h,s)

print("\nChecking the status of the inbox and keys...")
Status(stuID, h, s)


print("++++++++++++++++++++++++++++++++++++++++++++")

print("The other party's OTK public key is acquired from the server ...")


receiverID_bytes = receiverID.to_bytes((receiverID.bit_length()+7)//8, 'big')

#signature generation of receiver through my long term private key
hB, sB, k = SignGen(receiverID_bytes, E, sA)
#making friend's OTK's
OTK_B_id, OTK_B_x, OTK_B_y = reqOTKB(stuID, receiverID, hB, sB)


print("Generating Ephemeral key")
EKA_Pri, EKA_Pub = KeyGen(E)


OTK_B = Point(OTK_B_x, OTK_B_y, curve)

sending_msg = "Dormammu, I have come to bargain"
print("The message I want to send: ", sending_msg)
print("Generating the KDF chain for the encryption and the MAC value generation")
print("Generating session key using my EK and my friends Public OTK/ Phase 3...")

Ks = calculate_Ks(EKA_Pri, OTK_B)
KDF_Chain = generateKDF(Ks)

msg_kenc = KDF_Chain[0][0]
msg_khmac = KDF_Chain[0][1]


d_msg_bytes = str.encode(sending_msg)
nonce = d_msg_bytes[0:8]

cipher = AES.new(msg_kenc, AES.MODE_CTR, nonce = nonce)
nonce = cipher.nonce


d_msg_bytes = str.encode(sending_msg)
ciphertext = cipher.encrypt(d_msg_bytes)
mac = HMAC.new(key=msg_khmac, msg=ciphertext, digestmod=SHA256).digest()
encrypted_msg = nonce + ciphertext + mac   #this is aligned with an encrypted message structure
encrypted_msg_int = int.from_bytes(encrypted_msg, byteorder='big')
SendMsg(stuID, receiverID, OTK_B_id, 1, encrypted_msg_int, EKA_Pub.x, EKA_Pub.y)


print("++++++++++++++++++++++++++++++++++++++++++++")


print("Sending another message")

sending_msg = "I've come to talk with you again"
print("The message I want to send: ", sending_msg)
print("Generating the KDF chain for the encryption and the MAC value generation")
print("Generating session key using my EK and my friends Public OTK/ Phase 3...")

Ks = calculate_Ks(EKA_Pri, OTK_B)
KDF_Chain = generateKDF(Ks)

msg_kenc = KDF_Chain[0][0]
msg_khmac = KDF_Chain[0][1]


d_msg_bytes = str.encode(sending_msg)

nonce = d_msg_bytes[0:8]

cipher = AES.new(msg_kenc, AES.MODE_CTR, nonce = nonce)
nonce = cipher.nonce


ciphertext = cipher.encrypt(d_msg_bytes)
mac = HMAC.new(key=msg_khmac, msg=ciphertext, digestmod=SHA256).digest()
encrypted_msg = nonce + ciphertext + mac   #this is aligned with an encrypted message structure
encrypted_msg_int = int.from_bytes(encrypted_msg, byteorder='big')
SendMsg(stuID, receiverID, OTK_B_id, 2, encrypted_msg_int, EKA_Pub.x, EKA_Pub.y)


print("++++++++++++++++++++++++++++++++++++++++++++")

last_OTKID = OTK_B_id   #last otk we generated as i can see is 0 according to above OTKReg results


print("\nChecking the status of the inbox and keys...")
unread_msgs, remaining_otks, status_msg = Status(stuID, h, s)
Status(receiverID, 108758845629076834281427099620360071636951745436749817541121990309410962383447, 42261864381490223425574830679040276316516126224916809845237350693004156454265)

#after checking mailbox then remaining OTK's is generated.
if remaining_otks != 10:
    for i in range(10 - remaining_otks):
        OTK0_private, OTK0 = KeyGen(E)
        OTKs[last_OTKID+1] = [OTK0_private, OTK0.x, OTK0.y]
        OTK0_x_bytes = OTK0.x.to_bytes((OTK0.x.bit_length() + 7) // 8, byteorder='big')
        OTK0_y_bytes = OTK0.y.to_bytes((OTK0.y.bit_length() + 7) // 8, byteorder='big')
        temp = OTK0_x_bytes + OTK0_y_bytes
        hmac0 = HMAC.new(key=KHMAC, msg=temp, digestmod=SHA256)
        OTKReg(last_OTKID+1, OTK0.x, OTK0.y, hmac0.hexdigest())
        last_OTKID += 1

for i in range(len(msg_list)):

    d_msg = msg_list[i]['MESSAGE']
    d_msg_bytes = str.encode(d_msg)

    msg_kenc = KDF_Chain[i][0]
    msg_khmac = KDF_Chain[i][1]

    d_msg_bytes[0:8]
    cipher = AES.new(msg_kenc, AES.MODE_CTR, nonce = nonce)
    
    #When we send messages to your friends, they need to be encrypted below 4 lines is for that
    ciphertext = cipher.encrypt(d_msg_bytes)
    mac = HMAC.new(key=msg_khmac, msg=ciphertext, digestmod=SHA256).digest()
    encrypted_msg = nonce + ciphertext + mac   #this is aligned with an encrypted message structure
    encrypted_msg_int = int.from_bytes(encrypted_msg, byteorder='big')
    
    SendMsg(stuID, receiverID, OTK_B_id, msg_list[i]['ID'], encrypted_msg_int, EKA_Pub.x, EKA_Pub.y)

print("Checking the status of the inbox and keys...")
Status(stuID, h, s)

print("Checking the status of the inbox and keys...")
#below line is for seeing receiver's message box, it cosists h and s value taken from him
Status(receiverID, 108758845629076834281427099620360071636951745436749817541121990309410962383447, 42261864381490223425574830679040276316516126224916809845237350693004156454265)

'''BELOW PART IS FOR RE-AUTHENTICATE THE STUDENT-ID,
because when we run the code again, it does not generate new spk's, otk's for student,
because it exists and it gives error, so we need to reset for not to get problem'''



ResetOTK(h,s)

print("+++++++++++++++++++++++++++++++++++++++++++++")

ResetSPK(h,s)

print("+++++++++++++++++++++++++++++++++++++++++++++")
    
rcode = int(input("Enter reset code which is sent to you: "))
ResetIK(rcode)
