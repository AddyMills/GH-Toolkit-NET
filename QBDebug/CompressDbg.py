import sys
import gzip
import os

dbgFile = sys.argv[1]

with open(dbgFile, 'rt') as original_file:
    data = original_file.read()
if "QBKeys.txt" in dbgFile:
    dbg_name = "PS2Pak.dbg"
elif "AllQs.txt" in dbgFile:
    dbg_name = "keys_qs.dbg"
else:
    dbg_name = "keys.dbg"
saveName = os.path.join(os.path.dirname(dbgFile), dbg_name)

with gzip.open(saveName, 'wt') as compressed_file:
    compressed_file.write(data)