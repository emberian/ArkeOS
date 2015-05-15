﻿using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Executable {
	public class Image {
		public Header Header { get; set; }
		public List<Section> Sections { get; set; }

		public Image() {
			this.Header = new Header();
			this.Sections = new List<Section>();
		}

		public byte[] ToArray() {
			using (var stream = new MemoryStream()) {
				using (var writer = new BinaryWriter(stream)) {
					this.Header.Write(writer);
					this.Sections.ForEach(s => s.Write(writer));
				}

				return stream.ToArray();
			}
		}

		public void FromArray(byte[] data) {
			using (var reader = new BinaryReader(new MemoryStream(data))) {
				this.Header.Read(reader);

				while (reader.PeekChar() != -1) {
					var section = new Section();

					section.Read(reader);

					this.Sections.Add(section);
				}
			}
		}
	}
}