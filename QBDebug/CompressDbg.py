import sys
import gzip
import os

dbgFile = sys.argv[1]

with open(dbgFile, 'rt') as original_file:
    data = original_file.read()

saveName = os.path.join(os.path.dirname(dbgFile), "keys.dbg")

with gzip.open(saveName, 'wt') as compressed_file:
    compressed_file.write(data)