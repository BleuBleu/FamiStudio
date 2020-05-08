using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace FamiStudio
{
    class MultiMediaNotificationListener : IMMNotificationClient, IDisposable
    {
        private readonly IMMDeviceEnumerator _deviceEnumerator;

        public delegate void DefaultDeviceChangedDelegate();
        public event DefaultDeviceChangedDelegate DefaultDeviceChanged;

        public MultiMediaNotificationListener()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }
            _deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            _deviceEnumerator.RegisterEndpointNotificationCallback(this);
        }

        ~MultiMediaNotificationListener()
        {
            Dispose(false);
        }

        public void OnDefaultDeviceChanged(EDataFlow dataFlow, ERole deviceRole, string pwstrDefaultDeviceId)
        {
            Debug.WriteLine("Audio device changed!");
            DefaultDeviceChanged?.Invoke();
        }

        public void OnDeviceStateChanged(string pwstrDeviceId, uint dwNewState)
        {
        }

        public void OnDeviceAdded(string deviceId)
        {
        }

        public void OnDeviceRemoved(string deviceId)
        {
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            Marshal.ReleaseComObject(_deviceEnumerator);
        }
    }

    public enum STGM : uint
    {
        STGM_READ = 0x0,
        STGM_WRITE = 0x1,
        STGM_READWRITE = 0x2
    }

    public enum DeviceState
    {
        DEVICE_STATE_ACTIVE = 0x00000001,
        DEVICE_STATE_DISABLED = 0x00000002,
        DEVICE_STATE_NOTPRESENT = 0x00000004,
        DEVICE_STATE_UNPLUGGED = 0x00000008,
        DEVICE_STATEMASK_ALL = 0x0000000f
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    public enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    public enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeLibType(TypeLibTypeFlags.FNonExtensible)]
    [ComImport]
    public interface IMMDeviceCollection
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetCount(out uint pcDevices);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void Item([In] uint nDevice, [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeLibType(TypeLibTypeFlags.FNonExtensible)]
    [ComImport]
    public interface IMMDeviceEnumerator
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void EnumAudioEndpoints([ComAliasName("MMDevAPI.Interop.EDataFlow")] [In] EDataFlow dataFlow, [In] uint dwStateMask, [MarshalAs(UnmanagedType.Interface)] out IMMDeviceCollection ppDevices);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetDefaultAudioEndpoint([ComAliasName("MMDevAPI.Interop.EDataFlow")] [In] EDataFlow dataFlow, [ComAliasName("MMDevAPI.Interop.ERole")] [In] ERole role, [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppEndpoint);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrId, [MarshalAs(UnmanagedType.Interface)] out IMMDevice ppDevice);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void RegisterEndpointNotificationCallback([MarshalAs(UnmanagedType.Interface)] [In] IMMNotificationClient pClient);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void UnregisterEndpointNotificationCallback([MarshalAs(UnmanagedType.Interface)] [In] IMMNotificationClient pClient);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeLibType(TypeLibTypeFlags.FNonExtensible)]
    [ComImport]
    public interface IMMDevice
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void Activate([In] ref Guid iid, [In] uint dwClsCtx, [In] IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OpenPropertyStore([In] uint stgmAccess, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppProperties);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetState(out uint pdwState);
    }

    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeLibType(TypeLibTypeFlags.FNonExtensible)]
    [ComImport]
    public interface IMMNotificationClient
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId, [In] uint dwNewState);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OnDefaultDeviceChanged([ComAliasName("MMDevAPI.Interop.EDataFlow")] [In] EDataFlow flow, [ComAliasName("MMDevAPI.Interop.ERole")] [In] ERole role, [MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDefaultDeviceId);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] [In] string pwstrDeviceId, [In] PropertyKey key);
    }

    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IPropertyStore
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetCount(out uint cProps);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetAt([In] uint iProp, out PropertyKey pkey);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void GetValue([In] ref PropertyKey key, out PROPVARIANT pv);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void SetValue([In] ref PropertyKey key, [In] ref PROPVARIANT propvar);
        [MethodImpl(MethodImplOptions.InternalCall)]
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey
    {
        public static PropertyKey PKEY_Device_DeviceDesc = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 2 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_HardwareIds = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 3 }; // DEVPROP_TYPE_STRING_LIST
        public static PropertyKey PKEY_Device_Service = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 6 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_Class = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 9 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_Driver = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 11 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_ConfigFlags = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 12 }; // DEVPROP_TYPE_UINT32
        public static PropertyKey PKEY_Device_Manufacturer = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 13 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_FriendlyName = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 14 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_LocationInfo = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 15 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_Capabilities = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 17 }; // DEVPROP_TYPE_UNINT32
        public static PropertyKey PKEY_Device_BusNumber = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 23 }; // DEVPROP_TYPE_UINT32
        public static PropertyKey PKEY_Device_EnumeratorName = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 24 }; // DEVPROP_TYPE_STRING
        public static PropertyKey PKEY_Device_DevType = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 27 }; // DEVPROP_TYPE_UINT32
        public static PropertyKey PKEY_Device_Characteristics = new PropertyKey { fmtid = new Guid(unchecked((int)0xa45c254e), unchecked((short)0xdf1c), 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid = 29 }; // DEVPROP_TYPE_UINT32
        public static PropertyKey PKEY_Device_ManufacturerAttributes = new PropertyKey { fmtid = new Guid(unchecked((int)0x80d81ea6), unchecked((short)0x7473), 0x4b0c, 0x82, 0x16, 0xef, 0xc1, 0x1a, 0x2c, 0x4c, 0x8b), pid = 4 }; // DEVPROP_TYPE_UINT32
        public static PropertyKey PKEY_DeviceClass_IconPath = new PropertyKey { fmtid = new Guid(unchecked((int)0x259abffc), 0x50a7, 0x47ce, 0xaf, 0x8, 0x68, 0xc9, 0xa7, 0xd7, 0x33, 0x66), pid = 12 }; // DEVPROP_TYPE_STRING_LIST
        public static PropertyKey PKEY_DeviceClass_ClassCoInstallers = new PropertyKey { fmtid = new Guid(unchecked((int)0x713d1703), 0xa2e2, 0x49f5, 0x92, 0x14, 0x56, 0x47, 0x2e, 0xf3, 0xda, 0x5c), pid = 2 }; // DEVPROP_TYPE_STRING_LIST
        public static PropertyKey PKEY_DeviceInterface_FriendlyName = new PropertyKey { fmtid = new Guid(unchecked((int)0x026e516e), 0xb814, 0x414b, 0x83, 0xcd, 0x85, 0x6d, 0x6f, 0xef, 0x48, 0x22), pid = 2 }; // DEVPROP_TYPE_STRING

        public Guid fmtid;
        public uint pid;

        public static IEnumerable<PropertyKey> GetPropertyKeys()
        {
            var keyFields = typeof(PropertyKey).GetFields(BindingFlags.Public | BindingFlags.Static);
            return keyFields.Where(fieldInfo => fieldInfo.FieldType == typeof(PropertyKey))
                            .Select(fieldInfo => (PropertyKey)fieldInfo.GetValue(null));
        }

        public static string GetKeyName(PropertyKey propertyKey)
        {
            var keyFields = typeof(PropertyKey).GetFields(BindingFlags.Public | BindingFlags.Static);
            return keyFields.Select(fieldInfo => new { fieldInfo, value = (PropertyKey)fieldInfo.GetValue(null) })
                            .Where(@t => propertyKey.pid == @t.value.pid && propertyKey.fmtid == @t.value.fmtid)
                            .Select(@t => @t.fieldInfo.Name).FirstOrDefault();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct PROPVARIANT
    {
        public ushort variantType;
        public byte wReserved1;
        public byte wReserved2;
        public uint wReserved3;
        public PROPVARIANTVALUE value;

        public object Value
        {
            get
            {
                switch ((VarEnum)variantType)
                {
                    case VarEnum.VT_EMPTY:
                        return null;
                    case VarEnum.VT_NULL:
                        return null;
                    case VarEnum.VT_VARIANT:
                        break;
                    case VarEnum.VT_DECIMAL:
                        return value.cyVal;
                    case VarEnum.VT_VOID:
                        break;
                    case VarEnum.VT_HRESULT:
                        break;
                    case VarEnum.VT_PTR:
                        break;
                    case VarEnum.VT_SAFEARRAY:
                        break;
                    case VarEnum.VT_CARRAY:
                        break;
                    case VarEnum.VT_USERDEFINED:
                        break;
                    case VarEnum.VT_RECORD:
                        break;
                    case VarEnum.VT_STREAM:
                        break;
                    case VarEnum.VT_STORAGE:
                        break;
                    case VarEnum.VT_STREAMED_OBJECT:
                        break;
                    case VarEnum.VT_STORED_OBJECT:
                        break;
                    case VarEnum.VT_BLOB_OBJECT:
                        break;
                    case VarEnum.VT_CF:
                        break;
                    case VarEnum.VT_CLSID:
                        return Marshal.PtrToStructure(value.pVal, typeof(Guid));
                    case VarEnum.VT_VECTOR:
                        break;
                    case VarEnum.VT_ARRAY:
                        break;
                    case VarEnum.VT_BYREF:
                        break;
                    case VarEnum.VT_I1:
                        return value.cVal;
                    case VarEnum.VT_UI1:
                        return value.bVal;
                    case VarEnum.VT_I2:
                        return value.iVal;
                    case VarEnum.VT_UI2:
                        return value.uiVal;
                    case VarEnum.VT_I4:
                    case VarEnum.VT_INT:
                        return value.intVal;
                    case VarEnum.VT_UI4:
                    case VarEnum.VT_UINT:
                        return value.uintVal;
                    case VarEnum.VT_I8:
                        return value.hVal;
                    case VarEnum.VT_UI8:
                        return value.uhVal;
                    case VarEnum.VT_R4:
                        return value.fltVal;
                    case VarEnum.VT_R8:
                        return value.dblVal;
                    case VarEnum.VT_BOOL:
                        return value.boolVal;
                    case VarEnum.VT_ERROR:
                        return value.scode;
                    case VarEnum.VT_CY:
                        return value.cyVal;
                    case VarEnum.VT_DATE:
                        return value.date;
                    case VarEnum.VT_FILETIME:
                        return DateTime.FromFileTime(value.hVal);
                    case VarEnum.VT_BSTR:
                        return Marshal.PtrToStringBSTR(value.pVal);
                    case VarEnum.VT_BLOB:
                        var blob = value.blob;
                        var blobData = new byte[blob.cbSize];
                        Marshal.Copy(blob.pBlobData, blobData, 0, (int)blob.cbSize);
                        return blobData;
                    case VarEnum.VT_LPSTR:
                        return Marshal.PtrToStringAnsi(value.pVal);
                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(value.pVal);
                    case VarEnum.VT_UNKNOWN:
                        return Marshal.GetObjectForIUnknown(value.pVal);
                    case VarEnum.VT_DISPATCH:
                        return value.pVal;
                        //default:
                        //    throw new NotSupportedException("The type of this variable is not support ('" + variantType + "')");
                }
                return string.Format("unsupported {0}", ((VarEnum)variantType));
                //throw new NotSupportedException("The type of this variable is not support ('" + variantType.ToString() + "')");
            }
        }
    }

    [ComConversionLoss]
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 8)]
    public struct PROPVARIANTVALUE
    {
        [FieldOffset(0)]
        public sbyte cVal;
        [FieldOffset(0)]
        public byte bVal;
        [FieldOffset(0)]
        public short iVal;
        [FieldOffset(0)]
        public ushort uiVal;
        [FieldOffset(0)]
        public int intVal;
        [FieldOffset(0)]
        public uint uintVal;
        [FieldOffset(0)]
        public long hVal;
        [FieldOffset(0)]
        public ulong uhVal;
        [FieldOffset(0)]
        public float fltVal;
        [FieldOffset(0)]
        public double dblVal;
        [FieldOffset(0)]
        public short boolVal;
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.Error)]
        public int scode;
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.Currency)]
        public decimal cyVal;
        [FieldOffset(0)]
        public DateTime date;
        [FieldOffset(0)]
        public tagFILETIME filetime;
        [FieldOffset(0)]
        public tagARRAY array;
        [FieldOffset(0)]
        public tagBLOB blob;
        [ComConversionLoss]
        [FieldOffset(0)]
        public IntPtr pVal;
    }

    [ComConversionLoss]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct tagARRAY
    {
        public uint cElems;
        [ComConversionLoss]
        public IntPtr pElems;
    }

    [ComConversionLoss]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct tagBLOB
    {
        public uint cbSize;
        [ComConversionLoss]
        public IntPtr pBlobData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct tagFILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct tagLARGEINTEGER
    {
        public long QuadPart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct tagULARGEINTEGER
    {
        public ulong QuadPart;
    }
}
