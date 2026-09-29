#!/bin/bash

if [[ -z "$1" ]]; then
    echo "Usage: ./demo_decrypt.sh <base64_encrypted_text>" >&2
    exit 1
fi

if [[ -z "$DB_ENCRYPTION_KEY" ]]; then
    echo "Error: DB_ENCRYPTION_KEY environment variable is not set." >&2
    exit 1
fi

B64_DATA=$1

# convert it to hex
KEY_HEX=$(echo -n "$DB_ENCRYPTION_KEY" | base64 -d | xxd -p | tr -d '\n')
# then binary
echo "$B64_DATA" | base64 -d > raw_data.bin

# take out the first 16 bytes as IV
IV_HEX=$(head -c 16 raw_data.bin | xxd -p | tr -d '\n')

# openSSL to decrypt data
tail -c +17 raw_data.bin > ciphertext.bin
echo -n "Decrypted Text: "
openssl enc -d -aes-256-cbc -K "$KEY_HEX" -iv "$IV_HEX" -in ciphertext.bin 2>/dev/null
echo ""

rm raw_data.bin ciphertext.bin
