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

using AppStract.Core.Data.Application;

namespace AppStract.Manager.ApplicationConfiguration
{
  /// <summary>
  /// Contains a method to bind an instance of <see cref="ApplicationData"/>.
  /// </summary>
  public interface IApplicationConfigurationPage
  {

    /// <summary>
    /// Associates the specified <see cref="ApplicationData"/> to the current <see cref="IApplicationConfigurationPage"/>.
    /// </summary>
    /// <param name="dataSource"></param>
    void BindDataSource(ApplicationData dataSource);

  }
}