﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ArkeOS.Architecture;

namespace ArkeOS.Assembler {
	public class Assembler {
		private Dictionary<string, ulong> labels;
		private string inputFile;

		public Assembler(string inputFile) {
			this.labels = new Dictionary<string, ulong>();
			this.inputFile = inputFile;
		}

		public byte[] Assemble() {
			var lines = File.ReadAllLines(this.inputFile).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Replace(" + ", "+").Replace(" * ", "*").Replace(" - ", "-"));

			using (var stream = new MemoryStream()) {
				using (var writer = new BinaryWriter(stream)) {

					this.DiscoverLabelAddresses(lines);

					foreach (var line in lines) {
						var parts = line.Split(' ');

						if (parts[0] == "ORIGIN") {
							stream.Seek((long)Helpers.ParseLiteral(parts[1]), SeekOrigin.Begin);
						}
						else if (parts[0] == "LABEL") {

						}
						else if (parts[0].StartsWith("CONST")) {
							var size = int.Parse(parts[0].Split(':')[1]);

							if (parts[1].StartsWith("0")) {
								Helpers.SizedWrite(writer, Helpers.ParseLiteral(parts[1]), size);
							}
							else {
								Helpers.SizedWrite(writer, this.labels[parts[1].Substring(1, parts[1].Length - 2).Trim()], size);
							}
						}
						else if (parts[0] == "STRING") {
							var start = line.IndexOf("\"") + 1;
							var end = line.LastIndexOf("\"");

							writer.Write(Encoding.UTF8.GetBytes(line.Substring(start, end - start)));
						}
						else if (!parts[0].StartsWith(@"//")) {
							this.ParseInstruction(parts, true).Encode(writer);
						}
					}

					return stream.ToArray();
				}
			}
		}

		private void DiscoverLabelAddresses(IEnumerable<string> lines) {
			var address = 0UL;

			foreach (var line in lines) {
				var parts = line.Split(' ');

				if (parts[0] == "ORIGIN") {
					address = Helpers.ParseLiteral(parts[1]);
				}
				else if (parts[0] == "LABEL") {
					this.labels.Add(parts[1], address);
				}
				else if (parts[0].StartsWith("CONST")) {
					address += ulong.Parse(parts[0].Split(':')[1]);
				}
				else if (parts[0] == "STRING") {
					var start = line.IndexOf("\"") + 1;
					var end = line.LastIndexOf("\"");

					address += (ulong)(end - start);
				}
				else if (!parts[0].StartsWith(@"//")) {
					address += this.ParseInstruction(parts, false).Length;
				}
			}
		}

		private Instruction ParseInstruction(string[] parts, bool resolveLabels) {
			var size = InstructionSize.EightByte;

			var index = parts[0].IndexOf(':');
			if (index != -1) {
				switch (parts[0][index + 1]) {
					case '1': size = InstructionSize.OneByte; break;
					case '2': size = InstructionSize.TwoByte; break;
					case '4': size = InstructionSize.FourByte; break;
					case '8': size = InstructionSize.EightByte; break;
				}

				parts[0] = parts[0].Substring(0, index);
			}

			var def = InstructionDefinition.Find(parts[0]);

			if (def == null)
				throw new InvalidInstructionException();

			return new Instruction(def.Code, size, parts.Skip(1).Select(p => this.ParseParameter(size, p, resolveLabels)).ToList());
		}

		private Parameter ParseParameter(InstructionSize size, string value, bool resolveLabels) {
			if (value[0] == '[' && value[1] == '(') {
				Parameter calculatedBase = null, calculatedIndex = null, calculatedScale = null, calculatedOffset = null;
				bool sign = false;

				this.ParseCalculated(size, value.Substring(2, value.Length - 4).Trim(), resolveLabels, ref calculatedBase, ref calculatedIndex, ref calculatedScale, ref calculatedOffset, ref sign);

				return Parameter.CreateCalculatedAddress(calculatedBase, calculatedIndex, calculatedScale, calculatedOffset, sign);
			}
			else if (value[0] == '(') {
				Parameter calculatedBase = null, calculatedIndex = null, calculatedScale = null, calculatedOffset = null;
				bool sign = false;

				this.ParseCalculated(size, value.Substring(1, value.Length - 2).Trim(), resolveLabels, ref calculatedBase, ref calculatedIndex, ref calculatedScale, ref calculatedOffset, ref sign);

				return Parameter.CreateCalculatedLiteral(calculatedBase, calculatedIndex, calculatedScale, calculatedOffset, sign);
			}
			else if (value[0] == '[') {
				return this.ParseParameterType(InstructionSize.EightByte, resolveLabels, true, value.Substring(1, value.Length - 2).Trim());
			}
			else {
				return this.ParseParameterType(size, resolveLabels, false, value);
			}

			throw new InvalidParameterException();
		}

		private void ParseCalculated(InstructionSize size, string value, bool resolveLabels, ref Parameter calculatedBase, ref Parameter calculatedIndex, ref Parameter calculatedScale, ref Parameter calculatedOffset, ref bool sign) {
			var parts = value.Split('+', '-', '*');

			calculatedBase = this.ParseParameterType(size, resolveLabels, false, parts[0]);
			calculatedIndex = this.ParseParameterType(size, resolveLabels, false, parts[1]);

			if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
				calculatedScale = this.ParseParameterType(size, resolveLabels, false, parts[2]);

			if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]))
				calculatedOffset = this.ParseParameterType(size, resolveLabels, false, parts[3]);

			sign = value[parts[0].Length] == '+';
		}

		private Parameter ParseParameterType(InstructionSize size, bool resolveLabels, bool isAddress, string value) {
			if (value[0] == '{') {
				return Parameter.CreateLiteral(false, resolveLabels ? this.labels[value.Substring(1, value.Length - 2).Trim()] : 0, Helpers.SizeToBytes(size));
			}
			else if (value[0] == '0') {
				return Parameter.CreateLiteral(isAddress, Helpers.ParseLiteral(value), Helpers.SizeToBytes(size));
			}
			else if (value[0] == 'R') {
				return Parameter.CreateRegister(isAddress, Helpers.ParseEnum<Register>(value));
			}
			else if (value == "S") {
				return Parameter.CreateStack();
			}
			else {
				return null;
			}
		}
	}
}