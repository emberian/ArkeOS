﻿using System;
using ArkeOS.Architecture;

namespace ArkeOS.Hardware {
	public partial class Processor {
		#region Basic

		private void ExecuteHlt(Instruction instruction) {
			this.interruptController.Wait(50);
			this.supressRIPIncrement = true;
		}

		private void ExecuteNop(Instruction instruction) {

		}

		private void ExecuteInt(Instruction instruction) {
			this.Access(instruction, a => {
				this.interruptController.Enqueue((Interrupt)a);
			});
		}

		private void ExecuteEint(Instruction instruction) {
			this.Registers[Register.RIP] = this.Registers[Register.RSIP];

			this.inIsr = false;
			this.inProtectedIsr = false;
			this.supressRIPIncrement = true;
		}

		private void ExecuteMov(Instruction instruction) {
			this.Access(instruction, a => a);
		}

		private void ExecuteXchg(Instruction instruction) {
			this.Access(instruction, (a, b) => {
				this.SetValue(instruction.Parameter1, b);
				this.SetValue(instruction.Parameter2, a);
			});
		}

		private void ExecuteIn(Instruction instruction) {

		}

		private void ExecuteOut(Instruction instruction) {

		}

		private void ExecutePush(Instruction instruction) {
			this.Access(instruction, a => {
				this.Registers[Register.RSP] -= Helpers.SizeToBytes(instruction.Size);

				switch (instruction.Size) {
					case InstructionSize.OneByte: this.memoryController.WriteU8(this.Registers[Register.RSP], (byte)a); break;
					case InstructionSize.TwoByte: this.memoryController.WriteU16(this.Registers[Register.RSP], (ushort)a); break;
					case InstructionSize.FourByte: this.memoryController.WriteU32(this.Registers[Register.RSP], (uint)a); break;
					case InstructionSize.EightByte: this.memoryController.WriteU64(this.Registers[Register.RSP], a); break;
				}
			});
		}

		private void ExecutePop(Instruction instruction) {
			this.Access(instruction, () => {
				var value = 0UL;

				switch (instruction.Size) {
					case InstructionSize.OneByte: value = this.memoryController.ReadU8(this.Registers[Register.RSP]); break;
					case InstructionSize.TwoByte: value = this.memoryController.ReadU16(this.Registers[Register.RSP]); break;
					case InstructionSize.FourByte: value = this.memoryController.ReadU32(this.Registers[Register.RSP]); break;
					case InstructionSize.EightByte: value = this.memoryController.ReadU64(this.Registers[Register.RSP]); break;
				}

				this.Registers[Register.RSP] += Helpers.SizeToBytes(instruction.Size);

				return value;
			});
		}

		private void ExecuteJz(Instruction instruction) {
			if (this.GetValue(instruction.Parameter1) == 0) {
				this.Registers[Register.RIP] = this.GetValue(instruction.Parameter2);

				this.supressRIPIncrement = true;
			}
		}

		private void ExecuteJnz(Instruction instruction) {
			if (this.GetValue(instruction.Parameter1) != 0) {
				this.Registers[Register.RIP] = this.GetValue(instruction.Parameter2);

				this.supressRIPIncrement = true;
			}
		}

		private void ExecuteJmp(Instruction instruction) {
			this.Registers[Register.RIP] = this.GetValue(instruction.Parameter1);

			this.supressRIPIncrement = true;
		}

		private void ExecuteInte(Instruction instruction) {
			this.interruptsEnabled = true;
		}

		private void ExecuteIntd(Instruction instruction) {
			this.interruptsEnabled = false;
		}

		#endregion

		#region Math

		private void ExecuteAdd(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Helpers.SizeToMask(instruction.Size);

			if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) + (max & b)));
			}
		}

		private void ExecuteAdc(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Helpers.SizeToMask(instruction.Size);

			if (a < max) {
				a += carry;
			}
			else if (b < max) {
				b += carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.Parameter3, max);

				return;
			}

			if (max - a < b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) + (max & b)));
			}
		}

		private void ExecuteAdf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(aa + bb), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void ExecuteSub(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Helpers.SizeToMask(instruction.Size);

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & b) - (max & a)));
			}
		}

		private void ExecuteSbb(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var carry = this.Registers[Register.RC] > 0 ? 1UL : 0UL;
			var max = Helpers.SizeToMask(instruction.Size);

			if (a > 0) {
				a -= carry;
			}
			else if (b > 0) {
				b -= carry;
			}
			else if (carry == 1) {
				this.Registers[Register.RC] = ulong.MaxValue;

				this.SetValue(instruction.Parameter3, max);

				return;
			}

			if (a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & b) - (max & a)));
			}
		}

		private void ExecuteSbf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb - aa), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void ExecuteDiv(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				this.SetValue(instruction.Parameter3, b / a);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteDvf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
				var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb / aa), 0);

				this.SetValue(instruction.Parameter3, cc);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		private void ExecuteMul(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);
			var max = Helpers.SizeToMask(instruction.Size);

			if (a == 0) {
				var t = a;
				a = b;
				b = t;
			}

			if (a == 0) {
				this.SetValue(instruction.Parameter3, 0);

				return;
			}

			if (max / a > b)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter3, max & ((max & a) * (max & b)));
			}
		}

		private void ExecuteMlf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
			var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
			var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb * aa), 0);

			this.SetValue(instruction.Parameter3, cc);
		}

		private void ExecuteInc(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var max = Helpers.SizeToMask(instruction.Size);

			if (max == a)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter2, a + 1);
			}
		}

		private void ExecuteDec(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var max = Helpers.SizeToMask(instruction.Size);

			if (a == 0)
				this.Registers[Register.RC] = ulong.MaxValue;

			unchecked {
				this.SetValue(instruction.Parameter2, a - 1);
			}
		}

		private void ExecuteNeg(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var mask = (ulong)(1 << (this.CurrentInstruction.SizeInBits - 1));

			if ((a & mask) == 0) {
				a |= mask;
			}
			else {
				a &= ~mask;
			}

			this.SetValue(instruction.Parameter2, a);
		}

		private void ExecuteMod(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				this.SetValue(instruction.Parameter3, b % a);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}

		}

		private void ExecuteMdf(Instruction instruction) {
			var a = this.GetValue(instruction.Parameter1);
			var b = this.GetValue(instruction.Parameter2);

			if (a != 0) {
				var aa = BitConverter.ToDouble(BitConverter.GetBytes(a), 0);
				var bb = BitConverter.ToDouble(BitConverter.GetBytes(b), 0);
				var cc = BitConverter.ToUInt64(BitConverter.GetBytes(bb % aa), 0);

				this.SetValue(instruction.Parameter3, cc);
			}
			else {
				this.interruptController.Enqueue(Interrupt.DivideByZero);
			}
		}

		#endregion

		#region Logic

		private void ExecuteSr(Instruction instruction) {
			this.Access(instruction, (a, b) => b >> (byte)a);
		}

		private void ExecuteSl(Instruction instruction) {
			this.Access(instruction, (a, b) => b << (byte)a);
		}

		private void ExecuteRr(Instruction instruction) {
			this.Access(instruction, (a, b) => (b >> (byte)a) | (b << (Helpers.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void ExecuteRl(Instruction instruction) {
			this.Access(instruction, (a, b) => (b << (byte)a) | (b >> (Helpers.SizeToBits(instruction.Size) - (byte)a)));
		}

		private void ExecuteNand(Instruction instruction) {
			this.Access(instruction, (a, b) => ~(a & b));
		}

		private void ExecuteAnd(Instruction instruction) {
			this.Access(instruction, (a, b) => a & b);
		}

		private void ExecuteNor(Instruction instruction) {
			this.Access(instruction, (a, b) => ~(a | b));
		}

		private void ExecuteOr(Instruction instruction) {
			this.Access(instruction, (a, b) => a | b);
		}

		private void ExecuteNxor(Instruction instruction) {
			this.Access(instruction, (a, b) => ~(a ^ b));
		}

		private void ExecuteXor(Instruction instruction) {
			this.Access(instruction, (a, b) => a ^ b);
		}

		private void ExecuteNot(Instruction instruction) {
			this.Access(instruction, a => ~a);
		}

		private void ExecuteGt(Instruction instruction) {
			this.Access(instruction, (a, b) => b > a ? ulong.MaxValue : 0);
		}

		private void ExecuteGte(Instruction instruction) {
			this.Access(instruction, (a, b) => b >= a ? ulong.MaxValue : 0);
		}

		private void ExecuteLt(Instruction instruction) {
			this.Access(instruction, (a, b) => b < a ? ulong.MaxValue : 0);
		}

		private void ExecuteLte(Instruction instruction) {
			this.Access(instruction, (a, b) => b <= a ? ulong.MaxValue : 0);
		}

		private void ExecuteEq(Instruction instruction) {
			this.Access(instruction, (a, b) => b == a ? ulong.MaxValue : 0);
		}

		private void ExecuteNeq(Instruction instruction) {
			this.Access(instruction, (a, b) => b != a ? ulong.MaxValue : 0);
		}

		#endregion

		#region Debug

		private void ExecuteDbg(Instruction instruction) {
			this.Access(instruction, () => (ulong)DateTime.UtcNow.TimeOfDay.TotalMilliseconds);
		}

		private void ExecutePau(Instruction instruction) {
			this.Break();

			this.ExecutionPaused?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}