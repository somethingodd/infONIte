﻿/*
 * Copyright 2020 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;

namespace PeterHan.PLib {
	/// <summary>
	/// Used to pass the PLib version in the ILMerged assembly since the PLib version will
	/// not be included in the file version.
	/// </summary>
	public static class PVersion {
		/// <summary>
		/// The PLib version.
		/// </summary>
		public const string VERSION = "3.12.0.0";

		/// <summary>
		/// Reports whether the PLib version included or referenced by this mod is the latest
		/// version loaded on the client.
		/// 
		/// This accessor will only work after PLib is fully loaded. Therefore, it will be
		/// unavailable in OnLoad or RegisterPostload, and will always return false in those
		/// cases.
		/// </summary>
		public static bool IsLatestVersion {
			get {
				return VERSION == PSharedData.GetData<string>(PRegistry.KEY_VERSION);
			}
		}
	}
}
