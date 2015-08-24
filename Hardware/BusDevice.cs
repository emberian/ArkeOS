﻿namespace ArkeOS.Hardware {
    public abstract class SystemBusDevice {
        public SystemBusController SystemBus { get; set; }
        public ulong Id { get; set; }

        public abstract ulong VendorId { get; }
        public abstract ulong ProductId { get; }
        public abstract ulong DeviceType { get; }

        public ulong this[ulong address] {
            get {
                return this.ReadWord(address);
            }
            set {
                this.WriteWord(address, value);
            }
        }

        public void Copy(ulong source, ulong destination, ulong length) {
            this.Write(destination, this.Read(source, length));
        }

        public virtual ulong[] Read(ulong source, ulong length) {
            var buffer = new ulong[length];

            for (var i = 0UL; i < length; i++)
                buffer[i] = this.ReadWord(source + i);

            return buffer;
        }

        public virtual void Write(ulong destination, ulong[] data) {
            for (var i = 0UL; i < (ulong)data.Length; i++)
                this.WriteWord(destination + i, data[i]);
        }

        public abstract ulong ReadWord(ulong address);
        public abstract void WriteWord(ulong address, ulong data);
    }
}