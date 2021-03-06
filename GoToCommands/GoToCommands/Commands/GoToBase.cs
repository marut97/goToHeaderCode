﻿using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using GoToCommands.Lib;
using EnvDTE80;
using System.Collections.Generic;

namespace GoToCommands.Commands
{
    internal sealed class GoToBase
    {
        public const int _commandId = 4133;

        public static readonly Guid _commandSet = new Guid("1eececa1-e0da-4689-bb36-1cfbef669757");

        private readonly AsyncPackage _package;

		private static DTE _dte;

		private static String _derivedClassName;

		private GoToBase(AsyncPackage package, OleMenuCommandService commandService)
		{
			_package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));


			var cmdId = new CommandID(_commandSet, _commandId);
			var command = new OleMenuCommand(Execute, cmdId);

			command.BeforeQueryStatus += ButtonStatus;

			commandService.AddCommand(command);
		}

		public static GoToBase Instance
		{
			get;
			private set;
		}

		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get { return _package; }
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			_dte = await package.GetServiceAsync(typeof(DTE)) as DTE;
			Instance = new GoToBase(package, commandService);
		}

		private void ButtonStatus(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte == null || !(sender is OleMenuCommand button))
				return;

			button.Visible = false;

			if ((_dte.ActiveDocument.ProjectItem == null))
				return;

			CodeFile.set(_dte.ActiveDocument);
			button.Visible = CodeFile.HasBaseClass;
			_derivedClassName = CodeFile.HasBaseClass ? CodeFile.BaseClassName : "";
		}

		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var codeModel = _dte.ActiveDocument.ProjectItem.ContainingProject.CodeModel;

			foreach (EnvDTE.CodeElement element in codeModel.CodeElements)
			{
				if (!Utilities.IsHeader(element.ProjectItem.Name))
					continue;

				var codeClass = getClass(element);
				if (codeClass != null)
				{
					_dte.ExecuteCommand("File.OpenFile", element.ProjectItem.FileNames[0]);
					return;
				}

			}
		}

		private CodeClass getClass(CodeElement codeElement)
		{
			if (codeElement is CodeClass codeClass)
			{
				if (codeClass.Name.Contains(_derivedClassName))
					return codeClass;
			}
			else if (codeElement is CodeNamespace)
			{
				foreach (CodeElement element in codeElement.Children)
				{
					var classModel = getClass(element);
					if (classModel != null)
						return classModel;
				}
			}
			return null;
		}
	}
}
