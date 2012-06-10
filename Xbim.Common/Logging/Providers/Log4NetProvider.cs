#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Common
// Filename:    Log4NetProvider.cs
// (See accompanying copyright.rtf)

#endregion

using System;
using log4net.Config;
using System.Reflection;
using System.IO;
using Xbim.Common.Helpers;

namespace Xbim.Common.Logging.Providers
{

	/// <summary>
	/// Provides advanced logging capabilities using log4net through the <see cref="Log4NetLogger"/> Logger.
	/// </summary>
	/// <remarks>See http://logging.apache.org/log4net/release/manual/introduction.html for more on log4net logging.</remarks>
	internal class Log4NetProvider : ILoggingProvider
	{

		#region ILoggingProvider Members

		/// <summary>
		/// Configures the log4Net environment for first use.
		/// </summary>
		public void Configure()
		{
			Initialise();
			// Set up some default properties we can use to provide consistent log
			// file naming conventions. 
			log4net.GlobalContext.Properties["LogName"] = Path.Combine(LogPath, LogFileName);
			log4net.GlobalContext.Properties["ApplicationName"] = ApplicationName;
			XmlConfigurator.Configure();
		}

		/// <summary>
		/// Gets the <see cref="ILogger"/> applicable for this <see cref="T:System.Type"/>.
		/// </summary>
		/// <param name="callingType">The type.</param>
		/// <remarks>Logging consumers provider a Type to this call so that the Logging Provider
		/// can customise the logger dynamically for the Type. More advanced logging systems, such 
		/// as log4Net can use this to provide different logging levels and outputs for different
		/// parts of the application.</remarks>
		/// <returns>An <see cref="ILogger"/> for this Type.</returns>
		public ILogger GetLogger(Type callingType)
		{
			return new Log4NetLogger(callingType);
		}
		#endregion

		/// <summary>
		/// Gets the log path.
		/// </summary>
		/// <value>The log path.</value>
		public string LogPath
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the log file.
		/// </summary>
		/// <value>The name of the log file.</value>
		public string LogFileName
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// <value>The name of the application.</value>
		public string ApplicationName
		{
			get;
			private set;
		}

		private void Initialise()
		{
			Assembly mainAssembly = GetAssembly();

			String company = GetCompany(mainAssembly);
			String product = GetProductName(mainAssembly);
			String path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			path = Path.Combine(path, company);
			path = Path.Combine(path, product);

			if(!Directory.Exists(path))
			{
				try
				{
					Directory.CreateDirectory(path);
				}
				catch (SystemException) { }
			}

			LogPath = path;

			ApplicationName = mainAssembly.GetName().Name;
			LogFileName = ApplicationName;
		}

		private Assembly GetAssembly()
		{
			// This will work for classic .NET Executables...
			Assembly mainAssembly = Assembly.GetEntryAssembly();

			// When we are hosted under an un-managed process we have to work harder to get a path. 
			// e.g. MMC.exe or Excel.exe
			// This also works for apps hosted under IIS etc, but results can be unexpected,
			// since the entry method is often in the App_Code.dll which has limited meta data.
			if (mainAssembly == null)
			{
				try
				{
					System.Diagnostics.StackTrace stack =  new System.Diagnostics.StackTrace();
					// HACK: 6 stackframes up is typically the caller into the ILogger.
					mainAssembly = stack.GetFrame(6).GetMethod().DeclaringType.Assembly;
				}
				catch
				{ }
			}
			if (mainAssembly == null)
			{
				// Default to this assembly.
				mainAssembly = Assembly.GetExecutingAssembly();
			}

			return mainAssembly;
		}

		private string GetCompany(Assembly assembly)
		{
			String companyName = String.Empty;
			AssemblyCompanyAttribute companyAttr = AttributeHelper.GetAttribute<AssemblyCompanyAttribute>(assembly, true);

			if (companyAttr != null)
			{
				companyName = companyAttr.Company;
			}
			return companyName;
		}

		private string GetProductName(Assembly assembly)
		{
			String productName = String.Empty;
            AssemblyProductAttribute productAttr = AttributeHelper.GetAttribute<AssemblyProductAttribute>(assembly, true);

			if (productAttr != null)
			{
				productName = productAttr.Product;
			}
			return productName;
		}

		
	}
}
