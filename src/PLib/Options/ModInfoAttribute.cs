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

using Harmony;
using System;

namespace PeterHan.PLib {
	/// <summary>
	/// Allows mod authors to specify attributes for their mods to be shown in the Options
	/// dialog.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class ModInfoAttribute : Attribute {
		/// <summary>
		/// Creates a ModInfoAttribute using an object from another mod.
		/// </summary>
		/// <param name="attr">The attribute from the other mod.</param>
		/// <returns>A ModInfoAttribute object with the values from that object, where
		/// possible to retrieve; or null if none could be obtained.</returns>
		internal static ModInfoAttribute CreateFrom(object attr) {
			string title = null, image = null, url = null;
			bool collapse = false;
			if (attr.GetType().Name == typeof(ModInfoAttribute).Name) {
				var trAttr = Traverse.Create(attr);
				try {
					title = trAttr.GetProperty<string>(nameof(Title));
					image = trAttr.GetProperty<string>(nameof(Image));
					url = trAttr.GetProperty<string>(nameof(URL));
					collapse = trAttr.GetProperty<bool>(nameof(ForceCollapseCategories));
				} catch (Exception e) {
					PUtil.LogExcWarn(e);
				}
			}
			return string.IsNullOrEmpty(title) ? null : new ModInfoAttribute(title, url,
				image, collapse);
		}

		/// <summary>
		/// If true, forces all categories in the options screen to begin collapsed (except
		/// the default category).
		/// </summary>
		public bool ForceCollapseCategories { get; }

		/// <summary>
		/// The name of the image file (in the mod's root directory) to display in the options
		/// dialog. If null or empty (or it cannot be loaded), no image is displayed.
		/// </summary>
		public string Image { get; }

		/// <summary>
		/// The mod title. If not null or empty, overrides the title derived from the mod name.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// The URL to use for the mod. If null or empty, the Steam workshop link will be used
		/// if possible, or otherwise the button will not be shown.
		/// </summary>
		public string URL { get; }

		public ModInfoAttribute(string title, string url = null, string image = null,
				bool collapse = false) {
			ForceCollapseCategories = collapse;
			Image = image;
			Title = title;
			URL = url;
		}

		public override string ToString() {
			return "ModInfoAttribute[Title={0},URL={1},Image={2}]".F(Title, URL, Image);
		}
	}
}
