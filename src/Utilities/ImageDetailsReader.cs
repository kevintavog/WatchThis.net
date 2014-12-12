using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ExifLib;
using NLog;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Rangic.Utilities.Geo;

namespace WatchThis.Utilities
{
	static public class ImageDetailsReader
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public static Location GetLocation(string fullPath)
		{
			try
			{
				using (var exif = new ExifLib.ExifReader(fullPath))
				{
					string latRef, longRef;
					double[] latitude, longitude;
					exif.GetTagValue<string>(ExifTags.GPSLatitudeRef, out latRef);
					exif.GetTagValue<string>(ExifTags.GPSLongitudeRef, out longRef);
					exif.GetTagValue<double[]>(ExifTags.GPSLatitude, out latitude);
					exif.GetTagValue<double[]>(ExifTags.GPSLongitude, out longitude);

					if (latRef != null && longRef != null)
					{
						return new Location(
							ConvertLocation(latRef, latitude),
							ConvertLocation(longRef, longitude));
					}
				}
			}
			catch (ExifLib.ExifLibException)
			{
				// Eat it, this file isn't supported
			}
			catch (Exception ex)
			{
				logger.Info("Exception getting location: {0}", ex);
			}
			return null;
		}

		static private double ConvertLocation(string geoRef, double[] val)
		{
			var v = val[0] + val[1] / 60 + val[2] / 3600;
			if (geoRef == "S" || geoRef == "W")
			{
				v *= -1;
			}
			return v;
		}

		public static Size SizeFromFile(string fullPath)
		{
			using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(fullPath)))
			{
				int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;
				byte[] magicBytes = binaryReader.ReadBytes(maxMagicBytesLength);

				foreach(var kvPair in imageFormatDecoders)
				{
					if (StartsWith(magicBytes, kvPair.Key))
					{
						binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
						return kvPair.Value(binaryReader, fullPath);
					}
				}

				logger.Info("Unsupported file type: '{0}'", fullPath);
				return new Size(0, 0);
			}
		}

		private static Size DecodeBitmap(BinaryReader binaryReader, string fullPath)
		{
			binaryReader.ReadBytes(16);
			int width = binaryReader.ReadInt32();
			int height = binaryReader.ReadInt32();

			return new Size(width, height);
		}

		private static Size DecodeGif(BinaryReader binaryReader, string fullPath)
		{
			int width = binaryReader.ReadInt16();
			int height = binaryReader.ReadInt16();

			return new Size(width, height);
		}

		private static Size DecodePng(BinaryReader binaryReader, string fullPath)
		{
			binaryReader.ReadBytes(8);
			int width = ReadLittleEndianInt32(binaryReader);
			int height = ReadLittleEndianInt32(binaryReader);

			return new Size(width, height);
		}

		private static Size Decode_Jfif(BinaryReader reader, string fullPath)
		{
			try
			{
				using (var exif = new ExifLib.ExifReader(reader.BaseStream))
				{
					object width = null, height = null;
					if (!exif.GetTagValue(ExifTags.ImageWidth, out width))
					{
						exif.GetTagValue(ExifTags.PixelXDimension, out width);
					}
					if (!exif.GetTagValue(ExifTags.ImageLength, out height))
					{
						exif.GetTagValue(ExifTags.PixelYDimension, out height);
					}

					if (width == null || height == null)
					{
						var props = Enum.GetValues(typeof (ExifTags)).Cast<ushort>().Select(tagID =>
							{
								object val;
								if (exif.GetTagValue(tagID, out val))
								{
									// Special case - some doubles are encoded as TIFF rationals. These
									// items can be retrieved as 2 element arrays of {numerator, denominator}
									if (val is double)
									{
										int[] rational;
										if (exif.GetTagValue(tagID, out rational))
											val = string.Format("{0} ({1}/{2})", val, rational[0], rational[1]);
									}

									return string.Format("{0}: {1}; {2}", Enum.GetName(typeof (ExifTags), tagID), RenderTag(val), val.GetType().Name);
								}

								return null;

							}).Where(x => x != null).ToArray();

						logger.Info("EXIF problem for '{3}' - Failed getting width {0} or height {1}; properties: {2}", string.Join("\n", width, height, props), fullPath);
					}

					if (width is ushort)
					{
						return new Size((ushort) width, (ushort) height);
					}
					else
					if (width is uint)
					{
						return new Size((int) (uint) width, (int) (uint) height);
					}
				}
			}
			catch (Exception ex)
			{
				logger.Warn("Exception reading EXIF data from '{0}': {1}", fullPath, ex);
			}

			return new Size(0, 0);
		}

		private static string RenderTag(object tagValue)
		{
			// Arrays don't render well without assistance.
			var array = tagValue as Array;
			if (array != null)
				return string.Join(", ", array.Cast<object>().Select(x => x.ToString()).ToArray());

			return tagValue.ToString();
		}

		private static short ReadLittleEndianInt16(BinaryReader binaryReader)
		{
			byte[] bytes = new byte[sizeof(short)];
			for (int i = 0; i < sizeof(short); i += 1)
			{
				bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
			}
			return BitConverter.ToInt16(bytes, 0);
		}

		private static ushort ReadLittleEndianUInt16(BinaryReader binaryReader)
		{
			byte[] bytes = new byte[sizeof(short)];
			for (int i = 0; i < sizeof(short); i += 1)
			{
				bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
			}
			return BitConverter.ToUInt16(bytes, 0);
		}

		private static int ReadLittleEndianInt32(BinaryReader binaryReader)
		{
			byte[] bytes = new byte[sizeof(int)];
			for (int i = 0; i < sizeof(int); i += 1)
			{
				bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
			}
			return BitConverter.ToInt32(bytes, 0);
		}

		private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
		{
			for(int i = 0; i < thatBytes.Length; i+= 1)
			{
				if (thisBytes[i] != thatBytes[i])
				{
					return false;
				}
			}
			return true;
		}


		private static Dictionary<byte[], Func<BinaryReader, string, Size>> imageFormatDecoders = 
			new Dictionary<byte[], Func<BinaryReader, string, Size>>()
		{
			{ new byte[]{ 0x42, 0x4D },											DecodeBitmap},
			{ new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },					DecodeGif },
			{ new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, 				DecodeGif },
			{ new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },		DecodePng },
			{ new byte[]{ 0xff, 0xd8 },											Decode_Jfif },
		};

	}
}

