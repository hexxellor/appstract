﻿#region Copyright (C) 2009-2010 Simon Allaeys

/*
    Copyright (C) 2009-2010 Simon Allaeys
 
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
using System.Collections.Generic;
using AppStract.Engine.Configuration;
using AppStract.Engine.Virtualization.Registry.Data;
using AppStract.Utilities.Data;
using Microsoft.Win32;

namespace AppStract.Engine.Virtualization.Registry
{
  /// <summary>
  /// This class functions as a switch for <see cref="VirtualRegistry"/> and <see cref="TransparentRegistry"/>.
  /// The switching itself is based on various terms and conditions.
  /// </summary>
  public class RegistrySwitch
  {

    #region Variables

    /// <summary>
    /// The virtual registry.
    /// </summary>
    private readonly RegistryBase _virtualRegistry;
    /// <summary>
    /// Contains the open keys leading to hives that are not portable.
    /// </summary>
    private readonly RegistryBase _transparentRegistry;
    /// <summary>
    /// The collection of engine rules to apply during the target registry decision process.
    /// </summary>
    private readonly RegistryRuleCollection _engineRules;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="RegistrySwitch"/>.
    /// </summary>
    /// <param name="indexGenerator">The <see cref="IndexGenerator"/> to use for generating virtual key handles.</param>
    /// <param name="knownKeys">A list of all known virtual registry keys.</param>
    /// <param name="ruleCollection">The collection of engine rules to consider when deciding on a target registry.</param>
    public RegistrySwitch(IndexGenerator indexGenerator, IDictionary<uint, VirtualRegistryKey> knownKeys, RegistryRuleCollection ruleCollection)
    {
      _transparentRegistry = new TransparentRegistry(indexGenerator);
      _virtualRegistry = new VirtualRegistry(indexGenerator, knownKeys);
      _engineRules = ruleCollection;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the registry that must be used to handle te specified <see cref="RegistryRequest"/> with.
    /// </summary>
    /// <param name="request">The <see cref="RegistryRequest"/> to get the handling registry for.</param>
    /// <returns></returns>
    public RegistryBase GetRegistryFor(RegistryRequest request)
    {
      return GetRegistryFor(request, true);
    }

    /// <summary>
    /// Returns the registry that must be used to handle te specified <see cref="RegistryRequest"/> with.
    /// </summary>
    /// <param name="request">The <see cref="RegistryRequest"/> to get the handling registry for.</param>
    /// <param name="recoverHandle">Indicates whether or not a possible unknown <see cref="RegistryRequest.Handle"/> should be recovered and virtualized.</param>
    /// <returns></returns>
    public RegistryBase GetRegistryFor(RegistryRequest request, bool recoverHandle)
    {
      if (request == null)
        throw new ArgumentNullException("request");
      RegistryBase result = null;
      if (_virtualRegistry.IsKnownKey(request))
        result = _virtualRegistry;
      else if (_transparentRegistry.IsKnownKey(request))
        result = _transparentRegistry;
      else if (HiveHelper.IsHiveHandle(request.Handle))
      {
        request.KeyFullPath = HiveHelper.GetHive(request.Handle).AsRegistryHiveName();
        result = GetDefaultRegistryFor(request);
      }
      else if (recoverHandle)
        // Unknown handle, and allowed to be recovered and virtualized.
        result = TryRecoverUnknownHandle(request);
      else
        EngineCore.Log.Error("Unknown registry key handle => {0}", request.Handle);
      request.VirtualizationType = GetVirtualizationType(request.KeyFullPath);
      return result;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns the required virtualization type to use on a key.
    /// </summary>
    /// <param name="keyFullPath">The key's full path.</param>
    /// <returns>The <see cref="VirtualizationType"/>, indicating how the key should be accessed.</returns>
    private VirtualizationType GetVirtualizationType(string keyFullPath)
    {
      if (string.IsNullOrEmpty(keyFullPath))
        return VirtualizationType.Virtual;
      VirtualizationType accessMechanism;
      return _engineRules.HasRule(keyFullPath, out accessMechanism)
               ? accessMechanism
               : GetFallBackVirtualizationType(keyFullPath);
    }

    /// <summary>
    /// Returns the default virtualization type to use on a key.
    /// </summary>
    /// <param name="keyFullPath">The key's full path.</param>
    /// <returns>The <see cref="VirtualizationType"/>, indicating how the key should be accessed.</returns>
    private static VirtualizationType GetFallBackVirtualizationType(string keyFullPath)
    {
      EngineCore.Log.Error("Falling back to default rules, no rule specified for \"" + keyFullPath + "\"");
      var hive = HiveHelper.GetHive(keyFullPath);
      if (hive == RegistryHive.Users
          || hive == RegistryHive.CurrentUser)
        return VirtualizationType.VirtualWithFallback;
      if (hive == RegistryHive.CurrentConfig
          || hive == RegistryHive.LocalMachine
          || hive == RegistryHive.ClassesRoot)
        return VirtualizationType.TransparentRead;
      if (hive == RegistryHive.PerformanceData
          || hive == RegistryHive.DynData)
        return VirtualizationType.Transparent;
      throw new ApplicationException("Can't determine required action for unknown subkeys of  \"" + hive + "\"");
    }

    /// <summary>
    /// Returns the <see cref="RegistryBase"/> able to process requests for the given <paramref name="request"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private RegistryBase GetDefaultRegistryFor(RegistryRequest request)
    {
      request.VirtualizationType = GetVirtualizationType(request.KeyFullPath);
      return request.VirtualizationType == VirtualizationType.Transparent
               ? _transparentRegistry
               : _virtualRegistry;
    }

    /// <summary>
    /// Tries to recover from an unknown handle.
    /// </summary>
    /// <param name="request">The request for the unknown key handle to try to virtualize.</param>
    /// <returns></returns>
    private RegistryBase TryRecoverUnknownHandle(RegistryRequest request)
    {
      request.KeyFullPath = HostRegistry.GetKeyNameByHandle(request.Handle);
      if (request.KeyFullPath == null)
      {
        EngineCore.Log.Error("Unknown registry key handle => {0}", request.Handle);
        return null;
      }
      EngineCore.Log.Warning("Recovering from unknown registry key handle => {0} => {1}", request.Handle,
                            request.KeyFullPath);
      // Teach target about the recovered key handle.
      var recoveredHandle = request.Handle;
      var target = GetDefaultRegistryFor(request);
      Exception error = null;
      if (target.OpenKey(request) == NativeResultCode.Success)
      {
        try
        {
          target.AddAlias(request.Handle, recoveredHandle);
          HostRegistry.CloseKey(recoveredHandle);
          return target;
        }
        catch (ApplicationException e)
        {
          error = e;
        }
      }
      EngineCore.Log.Error("Unable to recover from unknown registry key handle => {0}",
                          error, request.Handle, request.KeyFullPath);
      return null;
    }

    #endregion

  }
}
