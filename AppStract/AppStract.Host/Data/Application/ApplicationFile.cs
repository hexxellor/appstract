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
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using AppStract.Utilities.Helpers;
using AppStract.Utilities.Extensions;

namespace AppStract.Host.Data.Application
{
  /// <summary>
  /// The <see cref="ApplicationFile"/> class describes any file used by the AppStract application.
  /// </summary>
  [Serializable]
  public sealed class ApplicationFile : ISerializable, IXmlSerializable
  {

    #region Variables

    private FileType _type;
    private string _file;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the <see cref="FileType"/> matching <see cref="FileName"/>.
    /// </summary>
    public FileType Type
    {
      get { return _type; }
    }

    /// <summary>
    /// Gets or sets the <see cref="FileName"/> of the described file.
    /// </summary>
    public string FileName
    {
      get { return _file; }
      set
      {
        _type = GetFileType(value);
        _file = value;
      }
    }

    #endregion

    #region Constructors

    public ApplicationFile() { }

    public ApplicationFile(string file)
    {
      FileName = file;
    }

    private ApplicationFile(SerializationInfo info, StreamingContext context)
    {
      ParserHelper.TryParseEnum(info.GetString("Type"), out _type);
      _file = info.GetString("FileName");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns whether the curretn <see cref="ApplicationFile"/> exists on the local media.
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
      return File.Exists(FileName);
    }

    /// <summary>
    /// Gets the library type for the current file.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// An <see cref="InvalidOperationException"/> is thrown if <see cref="Type"/> does not match
    /// <see cref="FileType.Executable"/> nor <see cref="FileType.Library"/>.
    /// </exception>
    /// <returns>The <see cref="LibraryType"/> for the current file.</returns>
    public LibraryType GetLibraryType()
    {
      if (_type != FileType.Library && _type != FileType.Executable)
        throw new InvalidOperationException("Unable to determine libary type on a file of type " + _type);
      if (!File.Exists(_file))
        return LibraryType.Undetermined;
      return AssemblyHelper.IsManagedAssembly(_file)
               ? LibraryType.Managed
               : LibraryType.Native;
    }

    /// <summary>
    /// Returns a string representation of the current <see cref="ApplicationFile"/>.
    /// </summary>
    /// <returns>A string formatted like "[<see cref="Type"/>] <see cref="FileName"/>"</returns>
    public override string ToString()
    {
      return "[" + Type + "] " + FileName; 
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns the <see cref="FileType"/> for the filename specified.
    /// </summary>
    /// <param name="filename">The file to determine the <see cref="FileType"/> of.</param>
    /// <returns>The <see cref="FileType"/> of the <paramref name="filename"/> specified.</returns>
    private static FileType GetFileType(string filename)
    {
      filename = filename.ToLowerInvariant();
      if (filename == "" || filename.IsComposedOf(new[] {'.', '\\'})
        || filename.EndsWith("" + Path.DirectorySeparatorChar) || Directory.Exists(filename))
        return FileType.Directory;
      if (filename.EndsWith(".db3"))
        return FileType.Database;
      if (filename.EndsWith(".exe"))
        return FileType.Executable;
      if (filename.EndsWith(".dll"))
        return FileType.Library;
      // Else
      return FileType.File;
    }

    #endregion

    #region ISerializable Members

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Type", Enum.GetName(typeof(FileType), _type));
      info.AddValue("FileName", _file);
    }

    #endregion

    #region IXmlSerializable Members

    public global::System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(XmlReader reader)
    {
      if (!ParserHelper.TryParseEnum(reader.GetAttribute("Type"), out _type))
        _type = GetFileType(_file);
      reader.Read();
      _file = reader.ReadElementString("FileName");
      reader.Read();
    }

    public void WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("Type", Enum.GetName(typeof(FileType), _type));
      writer.WriteElementString("FileName", _file);
    }

    #endregion

  }
}
