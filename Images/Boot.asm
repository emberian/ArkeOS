﻿BASE 0x40000000000000
OFFSET 0x00
DEFINE BUS_ENTRY_LENGTH 0d4
MOV (0x10000000000001 - $BUS_ENTRY_LENGTH) R0
LABEL LoopStart
ADD $BUS_ENTRY_LENGTH R0 R0
EQ 0d5 [R0] R1
MOV:Z:R1 {LoopStart} RIP
MOV [(R0 + 0d3)] RBASE
SL 0d52 RBASE RBASE
EQ 0x000000444556494E [RBASE] R2
MOV:Z:R2 {LoopStart} RIP
MOV 0d0 R0
MOV 0d0 R1
MOV 0d0 R2
MOV (RBASE + 0d1) RIP