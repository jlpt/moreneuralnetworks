#!/usr/bin/env python3
"""Extract label addresses from ld65's debug file into JSON (used by the playtest)."""
import json
import re
import sys

syms = {}
for line in open(sys.argv[1]):
    if line.startswith("sym") and "type=lab" in line:
        m = re.search(r'name="([^"]+)".*val=0x([0-9A-Fa-f]+)', line)
        if m:
            syms[m.group(1)] = int(m.group(2), 16)
json.dump(syms, open(sys.argv[2], "w"))
