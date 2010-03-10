﻿#region Copyright (C) 2008-2009 Simon Allaeys

/*
    Copyright (C) 2008-2009 Simon Allaeys
 
    This file is part of AppStract

    AppStract is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AppStract is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with AppStract.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using AppStract.Core.Data.Databases;
using AppStract.Core.Virtualization.Registry;
using AppStract.Utilities.Interop;
using Microsoft.Win32;
using Microsoft.Win32.Interop;
using ValueType = AppStract.Core.Virtualization.Registry.ValueType;

namespace AppStract.Server.Registry.Data
{
  /// <summary>
  /// Buffers keys that are read from the host's registry.
  /// Open keys stay buffered until <see cref="CloseKey"/> is called.
  /// </summary>
  public sealed class TransparentRegistry : RegistryBase
  {

    #region Constructors

    public TransparentRegistry(IndexGenerator indexGenerator)
      : base(indexGenerator)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Closes a key.
    /// </summary>
    /// <param name="hKey">Key to close.</param>
    public void CloseKey(uint hKey)
    {
      // Let the base delete the key from its internal dictionary.
      base.DeleteKey(hKey);
    }

    #endregion

    #region Overridden Methods

    public override bool OpenKey(string keyFullPath, out uint hKey)
    {
      // Always create a new VirtualRegistryKey, no matter if if one already exists for keyName.
      // Why? There is no counter for the number of users of each handle.
      // => if one user closes the handle, other users won't be able to use it anymore.
      if (!RegistryHelper.KeyExists(keyFullPath))
      {
        hKey = 0;
        return false;
      }
      hKey = BufferKey(keyFullPath);
      return true;
    }

    public override NativeResultCode CreateKey(string keyFullPath, out uint hKey, out RegCreationDisposition creationDisposition)
    {
      // Create the key in the real registry.
      var registryKey = RegistryHelper.CreateRegistryKey(keyFullPath, out creationDisposition);
      if (registryKey == null)
      {
        hKey = 0;
        return NativeResultCode.AccessDenied;
      }
      registryKey.Close();
      hKey = BufferKey(keyFullPath);
      return NativeResultCode.Succes;
    }

    public override NativeResultCode DeleteKey(uint hKey)
    {
      // Overriden, first delete the real key.
      if (HiveHelper.IsHiveHandle(hKey))
        return NativeResultCode.AccessDenied;
      string keyName;
      if (!IsKnownKey(hKey, out keyName))
        return NativeResultCode.InvalidHandle;
      int index = keyName.LastIndexOf(@"\");
      string subKeyName = keyName.Substring(index + 1);
      keyName = keyName.Substring(0, index);
      RegistryKey regKey = RegistryHelper.OpenRegistryKey(keyName, true);
      try
      {
        if (regKey != null)
          regKey.DeleteSubKeyTree(subKeyName);
      }
      catch (ArgumentException)
      {
        // Key is not found in real registry, call base to delete it from the buffer.
        base.DeleteKey(hKey);
        return NativeResultCode.NotFound;
      }
      catch
      {
        return NativeResultCode.AccessDenied;
      }
      // Real key is deleted, now delete the virtual one.
      return base.DeleteKey(hKey);
    }

    public override NativeResultCode QueryValue(uint hKey, string valueName, out VirtualRegistryValue value)
    {
      value = new VirtualRegistryValue(valueName, null, ValueType.INVALID);
      string keyPath;
      if (!IsKnownKey(hKey, out keyPath))
        return NativeResultCode.InvalidHandle;
      try
      {
        ValueType valueType;
        var data = RegistryHelper.QueryRegistryValue(keyPath, valueName, out valueType);
        if (data == null)
          return NativeResultCode.FileNotFound;
        value = new VirtualRegistryValue(valueName, MarshallingHelpers.ToByteArray(data), valueType);
        return NativeResultCode.Succes;
      }
      catch
      {
        return NativeResultCode.AccessDenied;
      }
    }

    public override NativeResultCode SetValue(uint hKey, VirtualRegistryValue value)
    {
      string keyPath;
      if (!IsKnownKey(hKey, out keyPath))
        return NativeResultCode.InvalidHandle;
      try
      {
        // Bug: Will the registry contain a correct value here?
        Microsoft.Win32.Registry.SetValue(keyPath, value.Name, value.Data, value.Type.AsValueKind());
      }
      catch
      {
        return NativeResultCode.AccessDenied;
      }
      return NativeResultCode.Succes;
    }

    public override NativeResultCode DeleteValue(uint hKey, string valueName)
    {
      string keyPath;
      if (!IsKnownKey(hKey, out keyPath))
        return NativeResultCode.InvalidHandle;
      try
      {
        var regKey = RegistryHelper.OpenRegistryKey(keyPath, true);
        if (regKey == null)
          return NativeResultCode.FileNotFound;
        regKey.DeleteValue(valueName, true);
        regKey.Close();
        return NativeResultCode.Succes;
      }
      catch (ArgumentException)
      {
        return NativeResultCode.FileNotFound;
      }
      catch
      {
        return NativeResultCode.AccessDenied;
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Buffers a <see cref="VirtualRegistryKey"/> for <paramref name="keyName"/> and returns the assigned handle.
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    private uint BufferKey(string keyName)
    {
      var key = ConstructRegistryKey(keyName);
      WriteKey(key, true);
      return key.Handle;
    }

    #endregion

  }
}
