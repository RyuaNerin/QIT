#!/bin/python3
#-*- coding: utf-8 -*-

import base64
import re
import sys

import gnupg

FILE_PATH = 'TwitterOAuth.cs'

gpg = gnupg.GPG()

with open(FILE_PATH, 'r') as fs:
    data = fs.read()

m = re.search(r'AppKeySecret *= *"(.+)";', data)
m = m.group(1)

if 'enc' in sys.argv:
    data = data.replace(
        m,
        base64.b64encode(
            str(
                gpg.encrypt(
                    m.encode('utf-8'),
                    [ "admin@ryuar.in" ]
                )
            ).encode('utf-8')
        ).decode('utf-8')
    )
elif 'dec' in sys.argv:
    data = data.replace(
        m,
        str(
            gpg.decrypt(
                base64.b64decode(m)
            )
        )
    )
else:
    quit()

with open(FILE_PATH, 'w') as fs:
    fs.truncate(0)
    fs.write(data)
