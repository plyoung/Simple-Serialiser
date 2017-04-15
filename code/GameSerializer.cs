
// https://github.com/plyoung/Simple-Serialiser
//
// version 1.0.0
//	- initial release
//
// version 1.0.1
//	- improved enum, array and list handling to use less space
//	- there was a bug in the array and list serialisers causing them to generate too big arrays
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_WII || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS || UNITY_WP_8_1 || UNITY_WSA || UNITY_WSA_8_1 || UNITY_WSA_10_0 || UNITY_WINRT || UNITY_WINRT_8_1 || UNITY_WINRT_10_0
using UnityEngine;
#endif

namespace BMGame
{
	public class GameSerializer
	{
		// ------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<Type, Func<object, byte[]>> writers = new Dictionary<Type, Func<object, byte[]>>();
        private readonly Dictionary<Type, Func<byte[], object>> readers = new Dictionary<Type, Func<byte[], object>>();
		private readonly Dictionary<Type, byte> lengths = new Dictionary<Type, byte>();

		public GameSerializer()
		{
			writers[typeof(bool)] = value => GetBytes((bool)value);
			writers[typeof(byte)] = value => GetBytes((byte)value);
			writers[typeof(sbyte)] = value => GetBytes((sbyte)value);
			writers[typeof(char)] = value => GetBytes((char)value);
			writers[typeof(int)] = value => GetBytes((int)value);
			writers[typeof(uint)] = value => GetBytes((uint)value);
			writers[typeof(short)] = value => GetBytes((short)value);
			writers[typeof(ushort)] = value => GetBytes((ushort)value);
			writers[typeof(long)] = value => GetBytes((long)value);
			writers[typeof(ulong)] = value => GetBytes((ulong)value);
			writers[typeof(float)] = value => GetBytes((float)value);
			writers[typeof(double)] = value => GetBytes((double)value);
			writers[typeof(decimal)] = value => GetBytes((decimal)value);
			writers[typeof(string)] = value => GetBytes((string)value);
			writers[typeof(Vector2)] = value => GetBytes((Vector2)value);
			writers[typeof(Vector3)] = value => GetBytes((Vector3)value);
			writers[typeof(Vector4)] = value => GetBytes((Vector4)value);
			writers[typeof(Quaternion)] = value => GetBytes((Quaternion)value);
			writers[typeof(Rect)] = value => GetBytes((Rect)value);
			writers[typeof(Color)] = value => GetBytes((Color)value);
			writers[typeof(Color32)] = value => GetBytes((Color32)value);

			readers[typeof(bool)] = data => ToBool(data);
			readers[typeof(byte)] = data => ToByte(data);
			readers[typeof(sbyte)] = data => ToSByte(data);
			readers[typeof(char)] = data => ToChar(data);
			readers[typeof(int)] = data => ToInt(data);
			readers[typeof(uint)] = data => ToUInt(data);
			readers[typeof(short)] = data => ToShort(data);
			readers[typeof(ushort)] = data => ToUShort(data);
			readers[typeof(long)] = data => ToLong(data);
			readers[typeof(ulong)] = data => ToULong(data);
			readers[typeof(float)] = data => ToFloat(data);
			readers[typeof(double)] = data => ToDouble(data);
			readers[typeof(decimal)] = data => ToDecimal(data);
			readers[typeof(string)] = data => ToString(data);
			readers[typeof(Vector2)] = data => ToVector2(data);
			readers[typeof(Vector3)] = data => ToVector3(data);
			readers[typeof(Vector4)] = data => ToVector4(data);
			readers[typeof(Quaternion)] = data => ToQuaternion(data);
			readers[typeof(Rect)] = data => ToRect(data);
			readers[typeof(Color)] = data => ToColor(data);
			readers[typeof(Color32)] = data => ToColor32(data);

			lengths[typeof(bool)] = 1;
			lengths[typeof(byte)] = 1;
			lengths[typeof(sbyte)] = 1;
			lengths[typeof(char)] = 1;
			lengths[typeof(int)] = 4;
			lengths[typeof(uint)] = 4;
			lengths[typeof(short)] = 2;
			lengths[typeof(ushort)] = 2;
			lengths[typeof(long)] = 8;
			lengths[typeof(ulong)] = 8;
			lengths[typeof(float)] = 4;
			lengths[typeof(double)] = 8;
			lengths[typeof(decimal)] = 16;
			lengths[typeof(Vector2)] = 8;
			lengths[typeof(Vector3)] = 12;
			lengths[typeof(Vector4)] = 16;
			lengths[typeof(Quaternion)] = 16;
			lengths[typeof(Rect)] = 16;
			lengths[typeof(Color)] = 16;
			lengths[typeof(Color32)] = 4;
		}

		private Func<object, byte[]> GetSerializer(Type t)
		{
			if (t.IsArray) return SerializeArray;
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return SerializeList;
			if (t.IsEnum) return SerializeEnum;
			Func<object, byte[]> f = null;
			if (writers.TryGetValue(t, out f)) return f;
			return null;
		}

		private byte ByteLength(Type t)
		{
			byte l;
			if (lengths.TryGetValue(t, out l)) return l;
			return 0;
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte[] Serialize(object obj)
		{
			if (obj == null) return new byte[0];
			Func<object, byte[]> f = GetSerializer(obj.GetType());
			if (f != null) return f(obj);

			throw new Exception("[GameSerializer] Unsupported Type: " + obj.GetType());
		}

		public object Deserialize(byte[] data, Type t)
		{
			if (data == null || data.Length == 0) return null;

			if (t.IsArray) return DeserializeArray(data, t);
			if (t.IsEnum) return DeserializeEnum(data, t);
			if (readers.ContainsKey(t)) return readers[t](data);

			throw new Exception("[GameSerializer] Unsupported Type: " + t);
		}

		public T Deserialize<T>(byte[] data)
		{
			if (data == null || data.Length == 0) return default(T);

			Type t = typeof(T);
			if (t.IsArray) return (T)DeserializeArray(data, t);
			if (t.IsEnum) return (T)DeserializeEnum(data, t);
			if (readers.ContainsKey(t)) return (T)readers[t](data);

			throw new Exception("[GameSerializer] Unsupported Type: " + t);
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte[] SerializeArray(object obj)
		{
			Array arr = obj as Array;
			if (arr == null) return new byte[0];

			Type t = obj.GetType().GetElementType();
			Func<object, byte[]> f = GetSerializer(t);
			if (f == null) throw new Exception("[GameSerializer] Unsupported Array type.");

			byte entryByteLength = ByteLength(t);
			MemoryStream stream = new MemoryStream();
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write((Int32)arr.Length);
				writer.Write((Byte)entryByteLength);
				for (int i = 0; i < arr.Length; i++)
				{
					object value = arr.GetValue(i);
					byte[] bytes = Serialize(value);
					if (entryByteLength == 0) writer.Write((UInt16)bytes.Length); // variable length - store size of entry
					writer.Write(bytes);
				}
			}

			stream.Flush();
			byte[] returnData = stream.ToArray();
			stream.Close();

			return returnData;
		}

		public object DeserializeArray(byte[] data, Type t)
		{
			if (data == null || data.Length == 0) return Array.CreateInstance(t, 0);

			Func<byte[], object> f = null;
			readers.TryGetValue(t, out f);

			Array arr = null;
			using (MemoryStream stream = new MemoryStream(data, false))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int count = reader.ReadInt32();
					byte bytesLength = reader.ReadByte();
					byte[] bytes = new byte[bytesLength];
					arr = Array.CreateInstance(t, count);
					for (int i = 0; i < count; i++)
					{
						if (bytesLength == 0)
						{	 // variable length entries
							int len = reader.ReadUInt16();
							bytes = new byte[len];
							reader.Read(bytes, 0, len);
							object obj = (f == null ? Deserialize(bytes, t) : f(bytes));
							arr.SetValue(obj, i);
						}
						else
						{
							reader.Read(bytes, 0, bytesLength);
							object obj = (f == null ? Deserialize(bytes, t) : f(bytes));
							arr.SetValue(obj, i);
						}
					}
				}
			}
			return arr;
		}

		public T[] DeserializeArray<T>(byte[] data)
		{
			T[] arr = null;
			if (data == null || data.Length == 0) return arr;

			Type t = typeof(T);
			Func<byte[], object> f = null;
			readers.TryGetValue(t, out f);

			using (MemoryStream stream = new MemoryStream(data, false))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int count = reader.ReadInt32();
					byte bytesLength = reader.ReadByte();
					byte[] bytes = new byte[bytesLength];
					arr = new T[count];
					for (int i = 0; i < count; i++)
					{
						if (bytesLength == 0)
						{	 // variable length entries
							int len = reader.ReadUInt16();
							bytes = new byte[len];
							reader.Read(bytes, 0, len);
							arr[i] = (T)(f == null ? Deserialize(bytes, t) : f(bytes));
						}
						else
						{
							reader.Read(bytes, 0, bytesLength);
							arr[i] = (T)(f == null ? Deserialize(bytes, t) : f(bytes));
						}
					}
				}
			}
			return arr;
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte[] SerializeList(object obj)
		{
			IList lst = obj as IList;
			if (lst == null) return new byte[0];

			Type t = obj.GetType().GetGenericArguments()[0];
			Func<object, byte[]> f = GetSerializer(t);
			if (f == null) throw new Exception("[GameSerializer] Unsupported Array type.");

			byte entryByteLength = ByteLength(t);
			MemoryStream stream = new MemoryStream();
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write((Int32)lst.Count);
				writer.Write((Byte)entryByteLength);
				for (int i = 0; i < lst.Count; i++)
				{
					object value = lst[i];
					byte[] bytes = f(value);
					if (entryByteLength == 0) writer.Write((UInt16)bytes.Length); // variable length - store size of entry
					writer.Write(bytes);
				}
			}

			stream.Flush();
			byte[] returnData = stream.ToArray();
			stream.Close();

			return returnData;
		}

		public List<T> DeserializeList<T>(byte[] data)
		{
			List<T> lst = new List<T>();
			if (data == null || data.Length == 0) return lst;

			Type t = typeof(T);
			Func<byte[], object> f = null;
			readers.TryGetValue(t, out f);

			using (MemoryStream stream = new MemoryStream(data, false))
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int count = reader.ReadInt32();
					byte bytesLength = reader.ReadByte();
					byte[] bytes = new byte[bytesLength];
					for (int i = 0; i < count; i++)
					{
						if (bytesLength == 0)
						{
							int len = reader.ReadUInt16();
							bytes = new byte[len];
							reader.Read(bytes, 0, len);
							T obj = (T)(f == null ? Deserialize(bytes, t) : f(bytes));
							lst.Add(obj);
						}
						else
						{
							reader.Read(bytes, 0, bytesLength);
							T obj = (T)(f == null ? Deserialize(bytes, t) : f(bytes));
							lst.Add(obj);
						}
					}
				}
			}
			return lst;
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte[] SerializeEnum(object obj)
		{
			Type t = Enum.GetUnderlyingType(obj.GetType());
			Func<object, byte[]> f = null;
			if (writers.TryGetValue(t, out f)) return f(obj);
			return new byte[0];
		}

		public object DeserializeEnum(byte[] data, Type t)
		{
			Type tt = Enum.GetUnderlyingType(t);
			Func<byte[], object> f = null;
			if (readers.TryGetValue(tt, out f))
			{
				object val = f(data);
				return Enum.ToObject(t, val);
			}

			throw new Exception("[GameSerializer] Could not Deserialize Enum.");
		}

		public T DeserializeEnum<T>(byte[] data)
		{
			return (T)DeserializeEnum(data, typeof(T));
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte[] GetBytes(byte value)
		{
			return new byte[] { value };
		}

		public byte[] GetBytes(sbyte value)
		{
			return new byte[] { (byte)value };
		}

		public byte[] GetBytes(char value)
		{
			return new byte[] { (byte)value };
		}

		public byte[] GetBytes(bool value)
		{
			return new byte[] { (byte)(value ? 1 : 0) };
		}

		public byte[] GetBytes(short value)
		{
			return new byte[] 
			{
				(byte)value,
				(byte)(value >> 8)
			};
		}

		public byte[] GetBytes(ushort value)
		{
			return new byte[]
			{
				(byte)value,
				(byte)(value >> 8)
			};
		}

		public byte[] GetBytes(uint value)
		{
			return new byte[]
			{
				(byte)value,
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24)
			};
		}

		public byte[] GetBytes(int value)
		{
			return new byte[]
			{
				(byte)value,
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24)
			};
		}

		public byte[] GetBytes(long value)
		{
			return new byte[]
			{
				(byte)value,
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24),
				(byte)(value >> 32),
				(byte)(value >> 40),
				(byte)(value >> 48),
				(byte)(value >> 56)
			};
		}

		public byte[] GetBytes(ulong value)
		{
			return new byte[]
			{
				(byte)value,
				(byte)(value >> 8),
				(byte)(value >> 16),
				(byte)(value >> 24),
				(byte)(value >> 32),
				(byte)(value >> 40),
				(byte)(value >> 48),
				(byte)(value >> 56)
			};
		}

		public byte[] GetBytes(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (false == BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		public byte[] GetBytes(double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (false == BitConverter.IsLittleEndian) Array.Reverse(bytes);
			return bytes;
		}

		public byte[] GetBytes(decimal value)
		{
			byte[] bytes = new byte[16];

			int[] bits = decimal.GetBits(value);
			int lo = bits[0];
			int mid = bits[1];
			int hi = bits[2];
			int flags = bits[3];

			bytes[0] = (byte)lo;
			bytes[1] = (byte)(lo >> 8);
			bytes[2] = (byte)(lo >> 0x10);
			bytes[3] = (byte)(lo >> 0x18);
			bytes[4] = (byte)mid;
			bytes[5] = (byte)(mid >> 8);
			bytes[6] = (byte)(mid >> 0x10);
			bytes[7] = (byte)(mid >> 0x18);
			bytes[8] = (byte)hi;
			bytes[9] = (byte)(hi >> 8);
			bytes[10] = (byte)(hi >> 0x10);
			bytes[11] = (byte)(hi >> 0x18);
			bytes[12] = (byte)flags;
			bytes[13] = (byte)(flags >> 8);
			bytes[14] = (byte)(flags >> 0x10);
			bytes[15] = (byte)(flags >> 0x18);

			return bytes;
		}

		public byte[] GetBytes(string value)
		{
			return System.Text.Encoding.Unicode.GetBytes(value);
		}

		public byte[] GetBytes(Vector2 value)
		{
			byte[] x = BitConverter.GetBytes(value.x);
			byte[] y = BitConverter.GetBytes(value.y);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
			}

			byte[] bytes = new byte[8];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			return bytes;
		}

		public byte[] GetBytes(Vector3 value)
		{
			byte[] x = BitConverter.GetBytes(value.x);
			byte[] y = BitConverter.GetBytes(value.y);
			byte[] z = BitConverter.GetBytes(value.z);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
				Array.Reverse(z);
			}

			byte[] bytes = new byte[12];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			BlockCopy(z, bytes, 8, 4);
			return bytes;
		}

		public byte[] GetBytes(Vector4 value)
		{
			byte[] x = BitConverter.GetBytes(value.x);
			byte[] y = BitConverter.GetBytes(value.y);
			byte[] z = BitConverter.GetBytes(value.z);
			byte[] w = BitConverter.GetBytes(value.w);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
				Array.Reverse(z);
				Array.Reverse(w);
			}

			byte[] bytes = new byte[16];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			BlockCopy(z, bytes, 8, 4);
			BlockCopy(w, bytes, 12, 4);
			return bytes;
		}

		public byte[] GetBytes(Quaternion value)
		{
			byte[] x = BitConverter.GetBytes(value.x);
			byte[] y = BitConverter.GetBytes(value.y);
			byte[] z = BitConverter.GetBytes(value.z);
			byte[] w = BitConverter.GetBytes(value.w);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
				Array.Reverse(z);
				Array.Reverse(w);
			}

			byte[] bytes = new byte[16];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			BlockCopy(z, bytes, 8, 4);
			BlockCopy(w, bytes, 12, 4);
			return bytes;
		}

		public byte[] GetBytes(Color value)
		{
			byte[] x = BitConverter.GetBytes(value.r);
			byte[] y = BitConverter.GetBytes(value.g);
			byte[] z = BitConverter.GetBytes(value.b);
			byte[] w = BitConverter.GetBytes(value.a);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
				Array.Reverse(z);
				Array.Reverse(w);
			}

			byte[] bytes = new byte[16];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			BlockCopy(z, bytes, 8, 4);
			BlockCopy(w, bytes, 12, 4);
			return bytes;
		}

		public byte[] GetBytes(Color32 value)
		{
			return new byte[] { value.r, value.g, value.b, value.a };
		}

		public byte[] GetBytes(Rect value)
		{
			byte[] x = BitConverter.GetBytes(value.x);
			byte[] y = BitConverter.GetBytes(value.y);
			byte[] z = BitConverter.GetBytes(value.width);
			byte[] w = BitConverter.GetBytes(value.height);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(x);
				Array.Reverse(y);
				Array.Reverse(z);
				Array.Reverse(w);
			}

			byte[] bytes = new byte[16];
			BlockCopy(x, bytes, 0, 4);
			BlockCopy(y, bytes, 4, 4);
			BlockCopy(z, bytes, 8, 4);
			BlockCopy(w, bytes, 12, 4);
			return bytes;
		}

		private void BlockCopy(byte[] src, byte[] dst, int offs, int count)
		{
			for (int i = 0; i < count; i++)
			{
				dst[i + offs] = src[i];
			}
		}

		// ------------------------------------------------------------------------------------------------------------

		public byte ToByte(byte[] data)
		{
			return data[0];
		}

		public sbyte ToSByte(byte[] data)
		{
			return (sbyte)data[0];
		}

		public char ToChar(byte[] data)
		{
			return (char)data[0];
		}

		public bool ToBool(byte[] data)
		{
			return (data[0] != 0);
		}

		public short ToShort(byte[] data)
		{
			return (short)((int)data[0] | (int)data[1] << 8);
		}

		public ushort ToUShort(byte[] data)
		{
			return (ushort)((int)data[0] | (int)data[1] << 8);
		}

		public uint ToUInt(byte[] data)
		{
			return (uint)((int)data[0] | (int)data[1] << 8 | (int)data[2] << 16 | (int)data[3] << 24);
		}

		public int ToInt(byte[] data)
		{
			return (int)data[0] | (int)data[1] << 8 | (int)data[2] << 16 | (int)data[3] << 24;
		}

		public long ToLong(byte[] data)
		{
			uint num1 = (uint)((int)data[0] | (int)data[1] << 8 | (int)data[2] << 16 | (int)data[3] << 24);
			uint num2 = (uint)((int)data[4] | (int)data[5] << 8 | (int)data[6] << 16 | (int)data[7] << 24);
			return (long)((ulong)num2 << 32 | (ulong)num1);
		}

		public ulong ToULong(byte[] data)
		{
			uint num1 = (uint)((int)data[0] | (int)data[1] << 8 | (int)data[2] << 16 | (int)data[3] << 24);
			uint num2 = (uint)((int)data[4] | (int)data[5] << 8 | (int)data[6] << 16 | (int)data[7] << 24);
			return (ulong)num2 << 32 | (ulong)num1;
		}

		public float ToFloat(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian) Array.Reverse(data, 0, 4);
			return BitConverter.ToSingle(data, 0);
		}

		public double ToDouble(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian) Array.Reverse(data, 0, 8);
			return BitConverter.ToDouble(data, 0);
		}

		public decimal ToDecimal(byte[] data)
		{
			int[] bits = new int[4];
			bits[0] = ((data[0] | (data[1] << 8)) | (data[2] << 0x10)) | (data[3] << 0x18); //lo
			bits[1] = ((data[4] | (data[5] << 8)) | (data[6] << 0x10)) | (data[7] << 0x18); //mid
			bits[2] = ((data[8] | (data[9] << 8)) | (data[10] << 0x10)) | (data[11] << 0x18); //hi
			bits[3] = ((data[12] | (data[13] << 8)) | (data[14] << 0x10)) | (data[15] << 0x18); //flags
			return new decimal(bits);
		}

		public string ToString(byte[] data)
		{
			return System.Text.Encoding.Unicode.GetString(data);
		}

		public Rect ToRect(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
				Array.Reverse(data, 8, 4);
				Array.Reverse(data, 12, 4);
			}

			float x = BitConverter.ToSingle(data, 0);
			float y = BitConverter.ToSingle(data, 4);
			float w = BitConverter.ToSingle(data, 8);
			float h = BitConverter.ToSingle(data, 12);
			return new Rect(x, y, w, h);
		}

		public Vector2 ToVector2(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
			}

			float x = BitConverter.ToSingle(data, 0);
			float y = BitConverter.ToSingle(data, 4);
			return new Vector2(x, y);
		}

		public Vector3 ToVector3(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
				Array.Reverse(data, 8, 4);
			}

			float x = BitConverter.ToSingle(data, 0);
			float y = BitConverter.ToSingle(data, 4);
			float z = BitConverter.ToSingle(data, 8);
			return new Vector3(x, y, z);
		}

		public Vector4 ToVector4(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
				Array.Reverse(data, 8, 4);
				Array.Reverse(data, 12, 4);
			}

			float x = BitConverter.ToSingle(data, 0);
			float y = BitConverter.ToSingle(data, 4);
			float z = BitConverter.ToSingle(data, 8);
			float w = BitConverter.ToSingle(data, 12);
			return new Vector4(x, y, z, w);
		}

		public Quaternion ToQuaternion(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
				Array.Reverse(data, 8, 4);
				Array.Reverse(data, 12, 4);
			}

			float x = BitConverter.ToSingle(data, 0);
			float y = BitConverter.ToSingle(data, 4);
			float z = BitConverter.ToSingle(data, 8);
			float w = BitConverter.ToSingle(data, 12);
			return new Quaternion(x, y, z, w);
		}

		public Color ToColor(byte[] data)
		{
			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
				Array.Reverse(data, 8, 4);
				Array.Reverse(data, 12, 4);
			}

			float r = BitConverter.ToSingle(data, 0);
			float g = BitConverter.ToSingle(data, 4);
			float b = BitConverter.ToSingle(data, 8);
			float a = BitConverter.ToSingle(data, 12);
			return new Color(r, g, b, a);
		}

		public Color32 ToColor32(byte[] data)
		{
			return new Color32(data[0], data[1], data[2], data[3]);
		}

		// ------------------------------------------------------------------------------------------------------------
	}
}
