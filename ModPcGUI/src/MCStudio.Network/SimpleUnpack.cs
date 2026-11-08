using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MCStudio.Network;

public class SimpleUnpack
{
	private byte[] content;

	private int idx;

	private ushort lastLen;

	public SimpleUnpack(byte[] bytes)
	{
		content = bytes;
		idx = 0;
		lastLen = 0;
	}

	public void Unpack<T>(ref T content)
	{
		FieldInfo[] fields = typeof(T).GetFields();
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			object value = fieldInfo.GetValue(content);
			Type fieldType = fieldInfo.FieldType;
			InnerUnpack(ref value, fieldType);
			object obj = content;
			fieldInfo.SetValue(obj, value);
			content = ConvertValue<T>(obj);
		}
	}

	public static T ConvertValue<T>(object value)
	{
		return (T)Convert.ChangeType(value, typeof(T));
	}

	private void InnerUnpack(ref object value, Type type)
	{
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Object:
			if (type == typeof(byte[]))
			{
				ushort num2 = lastLen;
				value = content.Skip(idx).Take(num2).ToArray();
				idx += num2;
			}
			else if (type == typeof(List<uint>))
			{
				ushort num3 = lastLen;
				List<uint> list = new List<uint>();
				while (num3 > 0)
				{
					uint item = BitConverter.ToUInt32(content, idx);
					list.Add(item);
					idx += 4;
					num3 -= 4;
				}
				value = list;
			}
			break;
		case TypeCode.Byte:
			value = content[idx];
			idx++;
			break;
		case TypeCode.Int16:
			value = BitConverter.ToInt16(content, idx);
			idx += 2;
			break;
		case TypeCode.UInt16:
			value = BitConverter.ToUInt16(content, idx);
			idx += 2;
			lastLen = (ushort)value;
			break;
		case TypeCode.Int32:
			value = BitConverter.ToInt32(content, idx);
			idx += 4;
			break;
		case TypeCode.UInt32:
			value = BitConverter.ToUInt32(content, idx);
			idx += 4;
			break;
		case TypeCode.String:
		{
			ushort num = lastLen;
			value = Encoding.UTF8.GetString(content, idx, num);
			idx += num;
			break;
		}
		}
	}
}
