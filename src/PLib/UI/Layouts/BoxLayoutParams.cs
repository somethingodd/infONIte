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
using UnityEngine;

namespace PeterHan.PLib.UI {
	/// <summary>
	/// The parameters used for laying out a box layout.
	/// </summary>
	[Serializable]
	public sealed class BoxLayoutParams {
		/// <summary>
		/// The alignment to use for components that are not big enough to fit and have no
		/// flexible width.
		/// </summary>
		public TextAnchor Alignment { get; set; }

		/// <summary>
		/// The direction of layout.
		/// </summary>
		public PanelDirection Direction { get; set; }

		/// <summary>
		/// The margin between the children and the component edge.
		/// </summary>
		public RectOffset Margin { get; set; }

		/// <summary>
		/// The spacing between components.
		/// </summary>
		public float Spacing { get; set; }

		public BoxLayoutParams() {
			Alignment = TextAnchor.MiddleCenter;
			Direction = PanelDirection.Horizontal;
			Margin = null;
			Spacing = 0.0f;
		}

		public override string ToString() {
			return "BoxLayoutParams[Alignment={0},Direction={1},Spacing={2:F2}]".F(Alignment,
				Direction, Spacing);
		}
	}
}
