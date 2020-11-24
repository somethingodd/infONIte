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
using System.Collections.Generic;
using System.Reflection;
using PeterHan.PLib.UI;
using UnityEngine;

namespace PeterHan.PLib.Options {
	/// <summary>
	/// An options entry which represents Enum and displays a spinner with text options.
	/// </summary>
	public class SelectOneOptionsEntry : OptionsEntry {
		/// <summary>
		/// Obtains the title and tool tip for an enumeration value.
		/// </summary>
		/// <param name="enumValue">The value in the enumeration.</param>
		/// <param name="fieldType">The type of the Enum field.</param>
		/// <returns>The matching Option</returns>
		private static Option GetAttribute(object enumValue, Type fieldType) {
			if (enumValue == null)
				throw new ArgumentNullException("enumValue");
			string valueName = enumValue.ToString(), title = valueName, tooltip = "";
			foreach (var enumField in fieldType.GetMember(valueName, BindingFlags.Public |
						BindingFlags.Static))
				if (enumField.DeclaringType == fieldType) {
					OptionAttribute oa;
					// Search for OptionsAttribute
					foreach (var attrib in enumField.GetCustomAttributes(false))
						if ((oa = OptionAttribute.CreateFrom(attrib)) != null) {
							title = LookInStrings(oa.Title);
							tooltip = LookInStrings(oa.Tooltip);
							break;
						}
					break;
				}
			return new Option(title, tooltip, enumValue);
		}

		public override object Value {
			get {
				return chosen?.Value;
			}
			set {
				// Find a matching value, if possible
				string valueStr = value?.ToString() ?? "";
				foreach (var option in options)
					if (valueStr == option.Value.ToString()) {
						chosen = option;
						Update();
						break;
					}
			}
		}

		/// <summary>
		/// The chosen item in the array.
		/// </summary>
		private Option chosen;

		/// <summary>
		/// The realized item label.
		/// </summary>
		private GameObject comboBox;
		
		/// <summary>
		/// The available options to cycle through.
		/// </summary>
		protected readonly IList<Option> options;

		protected SelectOneOptionsEntry(string title, string tooltip, string category = "") :
				base(title, tooltip, category) {
			chosen = null;
			comboBox = null;
			options = new List<Option>(8);
		}

		internal SelectOneOptionsEntry(string field, OptionAttribute oa, Type fieldType) :
				base(field, oa) {
			var eval = Enum.GetValues(fieldType);
			if (eval == null)
				throw new ArgumentException("No values, or invalid values, for enum");
			int n = eval.Length;
			if (n == 0)
				throw new ArgumentException("Enum has no declared members");
			chosen = null;
			comboBox = null;
			options = new List<Option>(n);
			for (int i = 0; i < n; i++)
				options.Add(GetAttribute(eval.GetValue(i), fieldType));
		}

		public override GameObject GetUIComponent() {
			// Find largest option to size the label appropriately, using em width this time!
			Option firstOption = null;
			int maxLen = 0;
			foreach (var option in options) {
				int len = option.Title?.Trim()?.Length ?? 0;
				// Kerning each string is slow, so estimate based on em width instead
				if (firstOption == null && len > 0)
					firstOption = option;
				if (len > maxLen)
					maxLen = len;
			}
			comboBox = new PComboBox<Option>("Select") {
				BackColor = PUITuning.Colors.ButtonPinkStyle, InitialItem = firstOption,
				Content = options, EntryColor = PUITuning.Colors.ButtonBlueStyle,
				TextStyle = PUITuning.Fonts.TextLightStyle, OnOptionSelected = UpdateValue,
			}.SetMinWidthInCharacters(maxLen).Build();
			Update();
			return comboBox;
		}

		/// <summary>
		/// Updates the displayed text to match the current item.
		/// </summary>
		private void Update() {
			if (comboBox != null && chosen != null)
				PComboBox<Option>.SetSelectedItem(comboBox, chosen, false);
		}

		/// <summary>
		/// Triggered when the value chosen from the combo box has been changed.
		/// </summary>
		/// <param name="selected">The value selected by the user.</param>
		private void UpdateValue(GameObject _, Option selected) {
			if (selected != null)
				chosen = selected;
		}

		/// <summary>
		/// Represents a selectable option.
		/// </summary>
		protected sealed class Option : ITooltipListableOption {
			/// <summary>
			/// The option title.
			/// </summary>
			public string Title { get; }

			/// <summary>
			/// The option tool tip.
			/// </summary>
			public string ToolTip { get; }

			/// <summary>
			/// The value to assign if this option is chosen.
			/// </summary>
			public object Value { get; }

			public Option(string title, string toolTip, object value) {
				Title = title ?? throw new ArgumentNullException("title");
				ToolTip = toolTip;
				Value = value;
			}

			public string GetProperName() {
				return Title;
			}

			public string GetToolTipText() {
				return ToolTip;
			}

			public override string ToString() {
				return "Option[Title={0},Value={1}]".F(Title, Value);
			}
		}
	}
}
