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
using System.Reflection;
using System.Reflection.Emit;

namespace PeterHan.PLib.Detours {
	/// <summary>
	/// Efficiently detours around many changes in the game by creating detour methods and
	/// accessors which are resilient against many types of source compatible but binary
	/// incompatible changes.
	/// </summary>
	public static class PDetours {
		/// <summary>
		/// Creates a dynamic detour method of the specified delegate type to wrap a base game
		/// method with the same name as the delegate type. The dynamic method will
		/// automatically adapt if optional parameters are added, filling in their default
		/// values.
		/// </summary>
		/// <typeparam name="D">The delegate type to be used to call the detour.</typeparam>
		/// <param name="type">The target type.</param>
		/// <returns>The detour that will call the method with the name of the delegate type.</returns>
		/// <exception cref="DetourException">If the delegate does not match any valid target method.</exception>
		public static D Detour<D>(this Type type) where D : Delegate {
			return Detour<D>(type, typeof(D).Name);
		}

		/// <summary>
		/// Creates a dynamic detour method of the specified delegate type to wrap a base game
		/// method with the specified name. The dynamic method will automatically adapt if
		/// optional parameters are added, filling in their default values.
		/// </summary>
		/// <typeparam name="D">The delegate type to be used to call the detour.</typeparam>
		/// <param name="type">The target type.</param>
		/// <param name="name">The method name.</param>
		/// <returns>The detour that will call that method.</returns>
		/// <exception cref="DetourException">If the delegate does not match any valid target method.</exception>
		public static D Detour<D>(this Type type, string name) where D : Delegate {
			if (type == null)
				throw new ArgumentNullException("type");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			var methods = type.GetMethods(PPatchTools.BASE_FLAGS | BindingFlags.Static |
				BindingFlags.Instance);
			// Determine delegate return type
			var expected = DelegateInfo.Create(typeof(D));
			MethodInfo bestMatch = null;
			int bestParamCount = int.MaxValue;
			foreach (var method in methods)
				if (method.Name == name) {
					try {
						var result = ValidateDelegate(expected, method);
						int n = result.Length;
						// Choose overload with fewest parameters to substitute
						if (n < bestParamCount) {
							bestParamCount = n;
							bestMatch = method;
						}
					} catch (DetourException) {
						// Keep looking
					}
				}
			if (bestMatch == null)
				throw new DetourException("No match found for {1}.{0}".F(name, type.FullName));
			return Detour<D>(bestMatch);
		}

		/// <summary>
		/// Creates a dynamic detour method of the specified delegate type to wrap a base game
		/// method with the specified name. The dynamic method will automatically adapt if
		/// optional parameters are added, filling in their default values.
		/// 
		/// This overload creates a lazy detour that only performs the expensive reflection
		/// when it is first used.
		/// </summary>
		/// <typeparam name="D">The delegate type to be used to call the detour.</typeparam>
		/// <param name="type">The target type.</param>
		/// <param name="name">The method name.</param>
		/// <returns>The detour that will call that method.</returns>
		/// <exception cref="DetourException">If the delegate does not match any valid target method.</exception>
		public static DetouredMethod<D> DetourLazy<D>(this Type type, string name)
				where D : Delegate {
			if (type == null)
				throw new ArgumentNullException("type");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			return new DetouredMethod<D>(type, name);
		}

		/// <summary>
		/// Creates a dynamic detour method of the specified delegate type to wrap a base game
		/// method with the specified name. The dynamic method will automatically adapt if
		/// optional parameters are added, filling in their default values.
		/// </summary>
		/// <typeparam name="D">The delegate type to be used to call the detour.</typeparam>
		/// <param name="target">The target method to be called.</param>
		/// <returns>The detour that will call that method.</returns>
		/// <exception cref="DetourException">If the delegate does not match the target.</exception>
		public static D Detour<D>(this MethodInfo target) where D : Delegate {
			if (target == null)
				throw new ArgumentNullException("target");
			if (target.ContainsGenericParameters)
				throw new ArgumentException("Generic types must have all parameters defined");
			var expected = DelegateInfo.Create(typeof(D));
			var parentType = target.DeclaringType;
			var expectedParamTypes = expected.ParameterTypes;
			var actualParams = ValidateDelegate(expected, target);
			int offset = target.IsStatic ? 0 : 1;
			// Method will be "declared" in the type of the target, as we are detouring around
			// a method of that type
			var caller = new DynamicMethod(target.Name + "_Detour", expected.ReturnType,
				expectedParamTypes, parentType, true);
			var generator = caller.GetILGenerator();
			// Load the known method arguments onto the stack
			int n = expectedParamTypes.Length, need = actualParams.Length + offset;
			if (n > 0)
				generator.Emit(OpCodes.Ldarg_0);
			if (n > 1)
				generator.Emit(OpCodes.Ldarg_1);
			if (n > 2)
				generator.Emit(OpCodes.Ldarg_2);
			if (n > 3)
				generator.Emit(OpCodes.Ldarg_3);
			for (int i = 4; i < n; i++)
				generator.Emit(OpCodes.Ldarg_S, i);
			// Load the rest as defaults
			for (int i = n; i < need; i++) {
				var param = actualParams[i - offset];
				PTranspilerTools.GenerateDefaultLoad(generator, param.ParameterType, param.
					DefaultValue);
			}
			if (parentType.IsValueType || target.IsStatic)
				generator.Emit(OpCodes.Call, target);
			else
				generator.Emit(OpCodes.Callvirt, target);
			generator.Emit(OpCodes.Ret);
			// Define the parameter names, parameter indexes start at 1
			if (offset > 0)
				caller.DefineParameter(1, ParameterAttributes.None, "this");
			for (int i = offset; i < n; i++) {
				var oldParam = actualParams[i - offset];
				caller.DefineParameter(i + 1, oldParam.Attributes, oldParam.Name);
			}
#if DEBUG
			PUtil.LogDebug("Created delegate {0} for method {1}.{2} with parameters [{3}]".
				F(caller.Name, parentType.FullName, target.Name, actualParams.Join(",")));
#endif
			return caller.CreateDelegate(typeof(D)) as D;
		}

		/// <summary>
		/// Creates dynamic detour methods to wrap a base game field or property with the
		/// specified name. The detour will still work even if the field is converted to a
		/// source compatible property and vice versa.
		/// </summary>
		/// <typeparam name="P">The type of the parent class.</typeparam>
		/// <typeparam name="T">The type of the field or property element.</typeparam>
		/// <param name="name">The name of the field or property to be accessed.</param>
		/// <returns>A detour element that wraps the field or property with common getter and
		/// setter delegates which will work on both types.</returns>
		public static IDetouredField<P, T> DetourField<P, T>(string name) {
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			var pt = typeof(P);
			var field = pt.GetField(name, PPatchTools.BASE_FLAGS | BindingFlags.
				Static | BindingFlags.Instance);
			IDetouredField<P, T> d;
			if (field == null) {
				try {
					var property = pt.GetProperty(name, PPatchTools.BASE_FLAGS |
						BindingFlags.Static | BindingFlags.Instance);
					if (property == null)
						throw new DetourException("Unable to find {0} on type {1}".
							F(name, typeof(P).FullName));
					d = DetourProperty<P, T>(property);
				} catch (AmbiguousMatchException) {
					throw new DetourException("Unable to find {0} on type {1}".
						F(name, typeof(P).FullName));
				}
			} else {
				if (pt.IsValueType || (pt.IsByRef && pt.GetElementType().IsValueType))
					throw new ArgumentException("For accessing struct fields, use DetourStructField");
				d = DetourField<P, T>(field);
			}
			return d;
		}

		/// <summary>
		/// Creates dynamic detour methods to wrap a base game field or property with the
		/// specified name. The detour will still work even if the field is converted to a
		/// source compatible property and vice versa.
		/// 
		/// This overload creates a lazy detour that only performs the expensive reflection
		/// when it is first used.
		/// </summary>
		/// <typeparam name="P">The type of the parent class.</typeparam>
		/// <typeparam name="T">The type of the field or property element.</typeparam>
		/// <param name="name">The name of the field or property to be accessed.</param>
		/// <returns>A detour element that wraps the field or property with common getter and
		/// setter delegates which will work on both types.</returns>
		public static IDetouredField<P, T> DetourFieldLazy<P, T>(string name) {
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			return new LazyDetouredField<P, T>(typeof(P), name);
		}

		/// <summary>
		/// Creates dynamic detour methods to wrap a base game field with the specified name.
		/// </summary>
		/// <typeparam name="P">The type of the parent class.</typeparam>
		/// <typeparam name="T">The type of the field element.</typeparam>
		/// <param name="target">The field which will be accessed.</param>
		/// <returns>A detour element that wraps the field with a common interface matching
		/// that of a detoured property.</returns>
		private static IDetouredField<P, T> DetourField<P, T>(FieldInfo target) {
			if (target == null)
				throw new ArgumentNullException("target");
			var parentType = target.DeclaringType;
			string name = target.Name;
			if (parentType != typeof(P))
				throw new ArgumentException("Parent type does not match delegate to be created");
			var getter = new DynamicMethod(name + "_Detour_Get", typeof(T), new Type[] {
				typeof(P)
			}, true);
			var generator = getter.GetILGenerator();
			// Getter will load the first argument and use ldfld/ldsfld
			if (target.IsStatic)
				generator.Emit(OpCodes.Ldsfld, target);
			else {
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, target);
			}
			generator.Emit(OpCodes.Ret);
			DynamicMethod setter;
			if (target.IsInitOnly)
				// Handle readonly fields
				setter = null;
			else {
				setter = new DynamicMethod(name + "_Detour_Set", null, new Type[] {
					typeof(P), typeof(T)
				}, true);
				generator = setter.GetILGenerator();
				// Setter will load both arguments and use stfld/stsfld (argument 1 is ignored
				// for static fields)
				if (target.IsStatic) {
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Stsfld, target);
				} else {
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Stfld, target);
				}
				generator.Emit(OpCodes.Ret);
			}
#if DEBUG
			PUtil.LogDebug("Created delegate for field {0}.{1} with type {2}".
				F(parentType.FullName, target.Name, typeof(T).FullName));
#endif
			return new DetouredField<P, T>(name, getter.CreateDelegate(typeof(Func<P, T>)) as
				Func<P, T>, setter?.CreateDelegate(typeof(Action<P, T>)) as Action<P, T>);
		}

		/// <summary>
		/// Creates dynamic detour methods to wrap a base game property with the specified name.
		/// </summary>
		/// <typeparam name="P">The type of the parent class.</typeparam>
		/// <typeparam name="T">The type of the property element.</typeparam>
		/// <param name="target">The property which will be accessed.</param>
		/// <returns>A detour element that wraps the property with a common interface matching
		/// that of a detoured field.</returns>
		/// <exception cref="DetourException">If the property has indexers.</exception>
		private static IDetouredField<P, T> DetourProperty<P, T>(PropertyInfo target) {
			if (target == null)
				throw new ArgumentNullException("target");
			var parentType = target.DeclaringType;
			string name = target.Name;
			if (parentType != typeof(P))
				throw new ArgumentException("Parent type does not match delegate to be created");
			var indexes = target.GetIndexParameters();
			if (indexes != null && indexes.Length > 0)
				throw new DetourException("Cannot detour on properties with index arguments");
			DynamicMethod getter, setter;
			if (target.CanRead) {
				getter = new DynamicMethod(name + "_Detour_Get", typeof(T), new Type[] {
					typeof(P)
				}, true);
				var generator = getter.GetILGenerator();
				var getMethod = target.GetGetMethod();
				// Getter will load the first argument and call the property getter
				if (!getMethod.IsStatic)
					generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, getMethod);
				generator.Emit(OpCodes.Ret);
			} else
				getter = null;
			if (target.CanWrite) {
				setter = new DynamicMethod(name + "_Detour_Set", null, new Type[] {
					typeof(P), typeof(T)
				}, true);
				var generator = setter.GetILGenerator();
				var setMethod = target.GetSetMethod();
				// Setter will load both arguments and call property setter (argument 1 is
				// ignored for static properties)
				if (!setMethod.IsStatic)
					generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Call, setMethod);
				generator.Emit(OpCodes.Ret);
			} else
				setter = null;
#if DEBUG
			PUtil.LogDebug("Created delegate for property {0}.{1} with type {2}".
				F(parentType.FullName, target.Name, typeof(T).FullName));
#endif
			return new DetouredField<P, T>(name, getter?.CreateDelegate(typeof(Func<P, T>)) as
				Func<P, T>, setter?.CreateDelegate(typeof(Action<P, T>)) as Action<P, T>);
		}

		/// <summary>
		/// Creates dynamic detour methods to wrap a base game struct field with the specified
		/// name. For static fields, use the regular DetourField.
		/// </summary>
		/// <typeparam name="T">The type of the field element.</typeparam>
		/// <param name="target">The field which will be accessed.</param>
		/// <returns>A detour element that wraps the field with a common interface matching
		/// that of a detoured property.</returns>
		public static IDetouredField<object, T> DetourStructField<T>(this Type parentType,
				string name) {
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			var target = parentType.GetField(name, PPatchTools.BASE_FLAGS | BindingFlags.
				Instance);
			if (target == null)
				throw new DetourException("Unable to find {0} on type {1}".F(name, parentType.
					FullName));
			var getter = new DynamicMethod(name + "_Detour_Get", typeof(T), new Type[] {
				typeof(object)
			}, true);
			var generator = getter.GetILGenerator();
			// Getter will load the first argument, unbox. and use ldfld
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Unbox, parentType);
			generator.Emit(OpCodes.Ldfld, target);
			generator.Emit(OpCodes.Ret);
			DynamicMethod setter;
			if (target.IsInitOnly)
				// Handle readonly fields
				setter = null;
			else {
				setter = new DynamicMethod(name + "_Detour_Set", null, new Type[] {
					typeof(object), typeof(T)
				}, true);
				generator = setter.GetILGenerator();
				// Setter will load both arguments, unbox and use stfld
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Unbox, parentType);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Stfld, target);
				generator.Emit(OpCodes.Ret);
			}
#if DEBUG
			PUtil.LogDebug("Created delegate for struct field {0}.{1} with type {2}".
				F(parentType.FullName, target.Name, typeof(T).FullName));
#endif
			return new DetouredField<object, T>(name, getter.CreateDelegate(typeof(
				Func<object, T>)) as Func<object, T>, setter?.CreateDelegate(typeof(
				Action<object, T>)) as Action<object, T>);
		}

		/// <summary>
		/// Verifies that the delegate signature provided in dst can be dynamically mapped to
		/// the method provided by src, with the possible addition of optional parameters set
		/// to their default values.
		/// </summary>
		/// <param name="returnType">The method return type expected.</param>
		/// <param name="parameterTypes">The method parameter types expected.</param>
		/// <param name="actual">The method to be called.</param>
		/// <returns>The parameters used in the call to the actual method.</returns>
		/// <exception cref="DetourException">If the delegate does not match the target.</exception>
		private static ParameterInfo[] ValidateDelegate(DelegateInfo expected,
				MethodInfo actual) {
			var parameterTypes = expected.ParameterTypes;
			var returnType = expected.ReturnType;
			// Validate return types
			if (!returnType.IsAssignableFrom(actual.ReturnType))
				throw new DetourException("Return type {0} cannot be converted to type {1}".
					F(actual.ReturnType.FullName, returnType.FullName));
			// Do not allow methods declared in not yet closed generic types
			var baseType = actual.DeclaringType;
			if (baseType.ContainsGenericParameters)
				throw new DetourException(("Method parent type {0} must have all " +
					"generic parameters defined").F(baseType.FullName));
			// Validate parameter types
			string actualName = baseType.FullName + "." + actual.Name;
			var actualParams = actual.GetParameters();
			int n = actualParams.Length, check = parameterTypes.Length;
			Type[] actualParamTypes, currentTypes = new Type[n];
			for (int i = 0; i < n; i++)
				currentTypes[i] = actualParams[i].ParameterType;
			if (actual.IsStatic)
				actualParamTypes = currentTypes;
			else {
				actualParamTypes = PTranspilerTools.PushDeclaringType(currentTypes, baseType);
				n++;
			}
			if (check > n)
				throw new DetourException(("Method {0} has only {1:D} parameters, but " +
					"{2:D} were supplied").F(actual.ToString(), n, check));
			// Check up to the number we have
			for (int i = 0; i < check; i++) {
				Type have = actualParamTypes[i], want = parameterTypes[i];
				if (!have.IsAssignableFrom(want))
					throw new DetourException(("Argument {0:D} for method {3} cannot be " +
						"converted from {1} to {2}").F(i, have.FullName, want.FullName,
						actualName));
			}
			// Any remaining parameters must be optional
			int offset = actual.IsStatic ? 0 : 1;
			for (int i = check; i < n; i++) {
				var cParam = actualParams[i - offset];
				if (!cParam.IsOptional)
					throw new DetourException(("New argument {0:D} for method {1} ({2}) " +
						"is not optional").F(i, actualName, cParam.ParameterType.FullName));
			}
			return actualParams;
		}

		/// <summary>
		/// Stores information about a delegate.
		/// </summary>
		private sealed class DelegateInfo {
			/// <summary>
			/// Creates delegate information on the specified delegate type.
			/// </summary>
			/// <param name="delegateType">The delegate type to wrap.</param>
			/// <returns>Information about that delegate's return and parameter types.</returns>
			public static DelegateInfo Create(Type delegateType) {
				if (delegateType == null)
					throw new ArgumentNullException("delegateType");
				var expected = delegateType.GetMethodSafe("Invoke", false, PPatchTools.
					AnyArguments);
				if (expected == null)
					throw new ArgumentException("Invalid delegate type: " + delegateType);
				return new DelegateInfo(delegateType, expected.GetParameterTypes(),
					expected.ReturnType);
			}

			/// <summary>
			/// The delegate's type.
			/// </summary>
			public readonly Type DelegateType;

			/// <summary>
			/// The delegate's parameter types.
			/// </summary>
			public readonly Type[] ParameterTypes;

			/// <summary>
			/// The delegate's return types.
			/// </summary>
			public readonly Type ReturnType;

			private DelegateInfo(Type delegateType, Type[] parameterTypes, Type returnType) {
				DelegateType = delegateType;
				ParameterTypes = parameterTypes;
				ReturnType = returnType;
			}

			public override string ToString() {
				return "DelegateInfo[delegate={0},return={1},parameters={2}]".F(DelegateType,
					ReturnType, ParameterTypes.Join(","));
			}
		}
	}
}
