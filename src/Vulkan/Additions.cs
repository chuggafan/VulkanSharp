﻿using System;
using System.Runtime.InteropServices;
using Vulkan.Interop;

namespace Vulkan
{
	public partial class Instance : IDisposable
	{
		NativeMethods.vkCreateDebugReportCallbackEXT vkCreateDebugReportCallbackEXT;
		NativeMethods.vkDestroyDebugReportCallbackEXT vkDestroyDebugReportCallbackEXT;
		NativeMethods.vkDebugReportMessageEXT vkDebugReportMessageEXT;

		Delegate GetMethod (string name, Type type)
		{
			var funcPtr = GetProcAddr (name);

			if (funcPtr == IntPtr.Zero)
				return null;

			return Marshal.GetDelegateForFunctionPointer (funcPtr, type);
		}

		void InitializeFunctions ()
		{

			vkCreateDebugReportCallbackEXT = (NativeMethods.vkCreateDebugReportCallbackEXT)GetMethod ("vkCreateDebugReportCallbackEXT", typeof (NativeMethods.vkCreateDebugReportCallbackEXT));
			vkDestroyDebugReportCallbackEXT = (NativeMethods.vkDestroyDebugReportCallbackEXT)GetMethod ("vkDestroyDebugReportCallbackEXT", typeof (NativeMethods.vkDestroyDebugReportCallbackEXT));
			vkDebugReportMessageEXT = (NativeMethods.vkDebugReportMessageEXT)GetMethod ("vkDebugReportMessageEXT", typeof (NativeMethods.vkDebugReportMessageEXT));
		}

		public Instance (InstanceCreateInfo CreateInfo, AllocationCallbacks Allocator = null)
		{
			Result result;

			unsafe {
				fixed (IntPtr* ptrInstance = &m) {
					result = Interop.NativeMethods.vkCreateInstance (CreateInfo.m, Allocator != null ? Allocator.m : null, ptrInstance);
				}
			}

			if (result != Result.Success)
				throw new ResultException (result);

			InitializeFunctions ();
		}

		public Instance () : this (new InstanceCreateInfo ())
		{
		}

		public void Dispose ()
		{
			if (debugCallback != null && vkDestroyDebugReportCallbackEXT != null) {
				DestroyDebugReportCallbackEXT (debugCallback);
				debugCallback = null;
			}
			if (m != IntPtr.Zero) {
				Destroy ();
				m = IntPtr.Zero;
			}
		}

		public delegate Bool32 DebugReportCallback (DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong objectHandle, IntPtr location, int messageCode, IntPtr layerPrefix, IntPtr message, IntPtr userData);

		DebugReportCallbackExt debugCallback;
		public void EnableDebug (DebugReportCallback d, DebugReportFlagsExt flags = DebugReportFlagsExt.Debug | DebugReportFlagsExt.Error | DebugReportFlagsExt.Information | DebugReportFlagsExt.PerformanceWarning | DebugReportFlagsExt.Warning)
		{
			if (vkCreateDebugReportCallbackEXT == null)
				throw new InvalidOperationException ("vkCreateDebugReportCallbackEXT is not available, possibly you might be missing VK_EXT_debug_report extension. Try to enable it when creating the Instance.");

			var debugCreateInfo = new DebugReportCallbackCreateInfoExt () {
				Flags = flags,
				PfnCallback = Marshal.GetFunctionPointerForDelegate (d)
			};

			if (debugCallback != null)
				DestroyDebugReportCallbackEXT (debugCallback);
			debugCallback = CreateDebugReportCallbackEXT (debugCreateInfo);
		}
	}

	unsafe public partial class ShaderModuleCreateInfo
	{
		public byte [] CodeBytes {
			set {
				/* todo free allocated memory when already set */
				if (value == null) {
					m->CodeSize = UIntPtr.Zero;
					m->Code = IntPtr.Zero;
					return;
				}
				m->CodeSize = (UIntPtr)value.Length;
				m->Code = Marshal.AllocHGlobal (value.Length);
				Marshal.Copy (value, 0, m->Code, value.Length);
			}
		}
	}

	public partial class Device
	{
		public ShaderModule CreateShaderModule (byte [] shaderCode, uint flags = 0, AllocationCallbacks allocator = null)
		{
			ShaderModuleCreateInfo createInfo = new ShaderModuleCreateInfo {
				CodeBytes = shaderCode,
				Flags = flags
			};
			return CreateShaderModule (createInfo, allocator);
		}
	}

	unsafe public partial class ClearColorValue
	{
		public ClearColorValue (float [] floatArray) : this ()
		{
			Float32 = floatArray;
		}

		public ClearColorValue (int [] intArray) : this ()
		{
			Int32 = intArray;
		}

		public ClearColorValue (uint [] uintArray) : this ()
		{
			Uint32 = uintArray;
		}
	}

	public interface IMarshalling
	{
		IntPtr Handle { get; }
	}

	public interface INonDispatchableHandleMarshalling
	{
		UInt64 Handle { get; }
	}

	internal struct NativeReference
	{
		internal static NativeReference Empty;

		internal IntPtr Handle { get; private set; }
		int refCount;

		internal NativeReference (int size)
		{
			Handle = Marshal.AllocHGlobal (size);
			refCount = 1;
		}

		internal NativeReference (IntPtr ptr)
		{
			Handle = ptr;
			refCount = 1;
		}

		internal void AddRef ()
		{
			if (Handle == IntPtr.Zero)
				return;

			refCount++;
		}

		internal void Release ()
		{
			if (Handle == IntPtr.Zero)
				return;

			refCount--;
			if (refCount <= 0) {
				Marshal.FreeHGlobal (Handle);
				Handle = IntPtr.Zero;
			}
		}
	}

	internal struct NativePointer
	{
		internal static NativePointer Null;

		internal NativeReference Reference { get; private set; }
		internal IntPtr Handle { get; private set; }

		internal NativePointer (NativeReference reference, IntPtr pointer)
		{
			reference.AddRef ();
			Reference = reference;
			Handle = pointer;
		}

		internal NativePointer (NativeReference reference)
		{
			reference.AddRef ();
			Reference = reference;
			Handle = reference.Handle;
		}

		internal NativePointer (IntPtr handle)
		{
			Reference = new NativeReference (handle);
			Handle = Reference.Handle;
		}

		internal void Release ()
		{
			Reference.Release ();
			Reference = NativeReference.Empty;
			Handle = IntPtr.Zero;
		}
	}

	public class MarshalledObject : IDisposable, IMarshalling
	{
		internal NativePointer native;

		IntPtr IMarshalling.Handle {
			get {
				return native.Handle;
			}
		}

		public void Dispose ()
		{
			VirtualDispose ();
			native.Release ();
			native = NativePointer.Null;
		}

		public virtual void VirtualDispose ()
		{
		}
	}
}
