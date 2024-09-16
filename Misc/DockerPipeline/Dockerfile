FROM mono:latest

RUN apt-get update && apt-get install -y wget unzip xxd

WORKDIR /app

ENV PAYLOAD_PATH /app/payload.bin

# csharp payload compiler
Copy csharp /app/csharp

# Copy the build script into the container
COPY build.sh /app
# Make the build script executable
RUN chmod +x build.sh

# Copy the payload into the container
COPY payload.bin /app

# Run the build script, passing the filename of each .template file in the templates directory as an argument
CMD ["/bin/bash", "/app/build.sh"]

