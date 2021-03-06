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
using AppStract.Engine.Data.Connection;

namespace AppStract.Host.Virtualization.Connection
{
  /// <summary>
  /// Enables access to an instance of <see cref="IProcessSynchronizer"/> across application domain boundaries.
  /// </summary>
  public sealed class ProcessSynchronizerInterface : MarshalByRefObject
  {

    /// <summary>
    /// The object providing means of synchronization between guest and host process.
    /// </summary>
    public static IProcessSynchronizer SProcessSynchronizer;

    /// <summary>
    /// Gets or sets <see cref="SProcessSynchronizer"/>, which is the object providing means of synchronization between guest and host process.
    /// </summary>
    public IProcessSynchronizer ProcessSynchronizer
    {
      get { return SProcessSynchronizer; }
      set { SProcessSynchronizer = value; }
    }

  }
}
