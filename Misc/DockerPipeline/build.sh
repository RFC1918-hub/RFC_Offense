#!/bin/bash

echo "Building C# payload..."
echo "Payload path: $PAYLOAD_PATH"
## csharp source code and compiling
payload_hex=$(xxd -p "$PAYLOAD_PATH")
payload_hex=$(echo "$payload_hex" | tr -d '\n')
formatted_payload_hex=$(echo "$payload_hex" | sed 's/\(..\)/0x\1,/g' | sed 's/,$//')
payload_length=$(echo "$payload_hex" | wc -c)
payload_length=$((payload_length / 2))
payload="byte[] buf = new byte[$payload_length] { $formatted_payload_hex };"

# parsing csharp template and compiling
for template in "$(find /app/csharp/ -type f -name "*.template")"
do 
    file_name=$(basename "$template")
    script_name="${file_name%.*}"
    sed "s/{{payload}}/$payload/g" "$template" > "/app/csharp/src/$script_name.cs"
    mcs "/app/csharp/src/$script_name.cs" -out:"/app/csharp/output/$script_name.exe"
done