﻿OFFSET 0x00
MOV (0x20000000000001 - 0d4) R0
LABEL LoopStart
ADD 0d4 R0 R0
EQ 0d4 [R0] R1
MVZ R1 {LoopStart} RIP
MOV [(R0 + 0d3)] R2
SL 0d52 R2 R2
EQ 0x000000444556494E [R2] R3
MVZ R3 {LoopStart} RIP
MOV (R2 + 0d1) RIP