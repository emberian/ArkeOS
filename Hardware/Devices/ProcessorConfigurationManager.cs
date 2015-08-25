﻿using System.Collections.Generic;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
    public class ProcessorConfigurationManager : SystemBusDevice {
        private ulong[] interruptVectors;

        public byte ProtectionMode { get; private set; }
        public byte SystemTickInterval { get; private set; }
        public bool InstructionCachingEnabled { get; private set; }

        public IReadOnlyList<ulong> InterruptVectors => this.interruptVectors;

        public ProcessorConfigurationManager() : base(1, 1, DeviceType.ProcessorConfiguration) {
            this.Reset();
        }

        public override ulong ReadWord(ulong address) {
            if (address >= 0x100 && address < 0x200) {
                return this.interruptVectors[(int)address - 0x100];
            }
            else if (address == 0) {
                return this.ProtectionMode;
            }
            else if (address == 1) {
                return this.SystemTickInterval;
            }
            else if (address == 2) {
                return this.InstructionCachingEnabled ? 1UL : 0UL;
            }
            else {
                return 0;
            }
        }

        public override void WriteWord(ulong address, ulong data) {
            if (address >= 0x100 && address < 0x200) {
                this.interruptVectors[(int)address - 0x100] = data;
            }
            else if (address == 0) {
                this.ProtectionMode = (byte)data;
            }
            else if (address == 0) {
                this.SystemTickInterval = (byte)data;
            }
            else if (address == 0) {
                this.InstructionCachingEnabled = data != 0;
            }
        }

        public void Reset() {
            this.SystemTickInterval = 50;
            this.InstructionCachingEnabled = true;
            this.ProtectionMode = 0;

            this.interruptVectors = new ulong[0xFF];
        }
    }
}