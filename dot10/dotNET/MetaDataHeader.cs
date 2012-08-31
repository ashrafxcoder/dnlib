﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// Storage flags found in the MD header
	/// </summary>
	[Flags]
	public enum StorageFlags : byte {
		/// <summary>
		/// Normal flags
		/// </summary>
		Normal = 0,

		/// <summary>
		/// More data after the header
		/// </summary>
		ExtraData = 1,
	}

	/// <summary>
	/// Represents the .NET metadata header
	/// </summary>
	/// <remarks><c>IMAGE_COR20_HEADER.MetaData</c> points to this header</remarks>
	public class MetaDataHeader : FileSection {
		uint signature;
		ushort majorVersion;
		ushort minorVersion;
		uint reserved1;
		uint stringLength;
		string versionString;
		uint offset2ndPart;
		StorageFlags flags;
		byte reserved2;
		ushort streams;
		IList<StreamHeader> streamHeaders;

		/// <summary>
		/// Returns the signature (should be 0x424A5342)
		/// </summary>
		public uint Signature {
			get { return signature; }
		}

		/// <summary>
		/// Returns the major version
		/// </summary>
		public ushort MajorVersion {
			get { return majorVersion; }
		}

		/// <summary>
		/// Returns the minor version
		/// </summary>
		public ushort MinorVersion {
			get { return minorVersion; }
		}

		/// <summary>
		/// Returns the reserved dword (pointer to extra header data)
		/// </summary>
		public uint Reserved1 {
			get { return reserved1; }
		}

		/// <summary>
		/// Returns the version string length value
		/// </summary>
		public uint StringLength {
			get { return stringLength; }
		}

		/// <summary>
		/// Returns the version string
		/// </summary>
		public string VersionString {
			get { return versionString; }
		}

		/// <summary>
		/// Returns the flags (reserved)
		/// </summary>
		public StorageFlags Flags {
			get { return flags; }
		}

		/// <summary>
		/// Returns the reserved byte (padding)
		/// </summary>
		public byte Reserved2 {
			get { return reserved2; }
		}

		/// <summary>
		/// Returns the number of streams
		/// </summary>
		public ushort Streams {
			get { return streams; }
		}

		/// <summary>
		/// Returns all stream headers
		/// </summary>
		public IList<StreamHeader> StreamHeaders {
			get { return streamHeaders; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public MetaDataHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.signature = reader.ReadUInt32();
			if (verify && this.signature != 0x424A5342)
				throw new BadImageFormatException("Invalid MetaData header signature");
			this.majorVersion = reader.ReadUInt16();
			this.minorVersion = reader.ReadUInt16();
			if (verify && !((majorVersion == 1 && minorVersion == 1) || (majorVersion == 0 && minorVersion >= 19)))
				throw new BadImageFormatException(string.Format("Unknown MetaData header version: {0}.{1}", majorVersion, minorVersion));
			this.reserved1 = reader.ReadUInt32();
			this.stringLength = reader.ReadUInt32();
			this.versionString = ReadString(reader, stringLength, verify);
			this.offset2ndPart = (uint)(reader.Position - startOffset.Value);
			this.flags = (StorageFlags)reader.ReadByte();
			this.reserved2 = reader.ReadByte();
			this.streams = reader.ReadUInt16();
			this.streamHeaders = new StreamHeader[streams];
			bool foundTables = false;
			for (int i = 0; i < streamHeaders.Count; i++) {
				var sh = new StreamHeader(reader, verify);
				streamHeaders[i] = sh;
				if (sh.Name == "#~" || sh.Name == "#-" || sh.Name == "#Schema") {
					if (verify && foundTables)
						throw new BadImageFormatException("More than one tables stream found");
					foundTables = true;
				}
			}
			if (verify && !foundTables)
				throw new BadImageFormatException("No metadata tables stream");
			SetEndoffset(reader);
		}

		string ReadString(IImageStream reader, uint maxLength, bool verify) {
			long endPos = reader.Position + maxLength;
			if (endPos < reader.Position || endPos > reader.Length)
				throw new BadImageFormatException("Invalid MD version string");
			var sb = new StringBuilder(50);
			uint i;
			for (i = 0; i < maxLength; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			if (verify && i == maxLength)
				throw new BadImageFormatException("Invalid MD version string");
			reader.Position = endPos;
			return sb.ToString();
		}
	}
}