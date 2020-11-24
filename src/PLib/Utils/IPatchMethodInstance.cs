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

using PostLoadHandler = System.Action<Harmony.HarmonyInstance>;

namespace PeterHan.PLib {
	/// <summary>
	/// Refers to a single instance of the annotation, with its annotated method.
	/// </summary>
	public interface IPatchMethodInstance {
		/// <summary>
		/// Runs the patch or method if the conditions are met. This method should check its
		/// preconditions before executing the target.
		/// </summary>
		/// <param name="instance">The Harmony instance to use.</param>
		void Run(HarmonyInstance instance);
	}

	/// <summary>
	/// A legacy handler for old post-load actions.
	/// </summary>
	internal sealed class LegacyPostloadMethod : IPatchMethodInstance {
		/// <summary>
		/// The handler from the mod to execute.
		/// </summary>
		public PostLoadHandler Handler { get; }

		internal LegacyPostloadMethod(PostLoadHandler handler) {
			Handler = handler ?? throw new ArgumentNullException("handler");
		}

		public void Run(HarmonyInstance instance) {
			Handler.Invoke(instance);
		}
	}
}
