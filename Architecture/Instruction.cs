﻿using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Architecture {
	public class Instruction {
		public InstructionDefinition Definition { get; private set; }
		public byte Code { get; }
		public byte Length { get; }

		public Parameter Parameter1 { get; private set; }
		public Parameter Parameter2 { get; private set; }
		public Parameter Parameter3 { get; private set; }

		public Instruction(byte code, IList<Parameter> parameters) {
			this.Code = code;
			this.Length = 1;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = parameters[0];
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = parameters[1];
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = parameters[2];
				this.Length += this.Parameter3.Length;
			}
		}

		public Instruction(ulong[] memory, ulong address) {
			this.Code = (byte)((memory[address] & 0xFF00000000000000UL) >> 56);
			this.Length = 1;

			this.Definition = InstructionDefinition.Find(this.Code);

			if (this.Definition.ParameterCount >= 1) {
				this.Parameter1 = Parameter.CreateFromMemory((ParameterType)((memory[address] >> 53) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter1.Length;
			}

			if (this.Definition.ParameterCount >= 2) {
				this.Parameter2 = Parameter.CreateFromMemory((ParameterType)((memory[address] >> 50) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter2.Length;
			}

			if (this.Definition.ParameterCount >= 3) {
				this.Parameter3 = Parameter.CreateFromMemory((ParameterType)((memory[address] >> 47) & 0x07), memory, address + this.Length);
				this.Length += this.Parameter3.Length;
			}
		}

		public void Encode(BinaryWriter writer) {
			var value = (ulong)this.Code << 56;

			value |= (ulong)(this.Parameter1?.Type ?? 0) << 53;
			value |= (ulong)(this.Parameter2?.Type ?? 0) << 50;
			value |= (ulong)(this.Parameter3?.Type ?? 0) << 47;

			writer.Write(value);

			this.Parameter1?.Encode(writer);
			this.Parameter2?.Encode(writer);
			this.Parameter3?.Encode(writer);
		}

		public override string ToString() {
			return this.Definition.Mnemonic + " " + this.Parameter1?.ToString() + " " + this.Parameter2?.ToString() + " " + this.Parameter3?.ToString();
		}
	}
}