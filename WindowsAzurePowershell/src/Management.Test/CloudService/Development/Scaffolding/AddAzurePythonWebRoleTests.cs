﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Management.Test.CloudService.Development.Scaffolding
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.CloudService.Development.Scaffolding;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AddAzurePythonWebRoleTests : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private AddAzureDjangoWebRoleCommand addPythonWebCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
            mockCommandRuntime = new MockCommandRuntime();
        }

        [TestMethod]
        public void AddAzurePythonWebRoleProcess()
        {
            var pyInstall = AddAzureDjangoWebRoleCommand.FindPythonInterpreterPath();
            if (pyInstall == null)
            {
                Assert.Inconclusive("Python is not installed on this machine and therefore the Python tests cannot be run");
                return;
            }

            string stdOut, stdErr;
            ProcessHelper.StartAndWaitForProcess(
                    new ProcessStartInfo(
                        Path.Combine(pyInstall, "python.exe"),
                        string.Format("-m django.bin.django-admin")
                    ),
                    out stdOut,
                    out stdErr
            );

            if (stdOut.IndexOf("django-admin.py") == -1)
            {
                Assert.Inconclusive("Django is not installed on this machine and therefore the Python tests cannot be run");
                return;
            }

            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                addPythonWebCmdlet = new AddAzureDjangoWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime };
                addPythonWebCmdlet.CommandRuntime = mockCommandRuntime;
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreatePython, rootPath, roleName);
                mockCommandRuntime.ResetPipelines();

                addPythonWebCmdlet.ExecuteCmdlet();

                AzureAssert.ScaffoldingExists(Path.Combine(rootPath, roleName), Path.Combine(Resources.PythonScaffolding, Resources.WebRole));
                Assert.AreEqual<string>(roleName, ((PSObject)mockCommandRuntime.OutputPipeline[0]).GetVariableValue<string>(Parameters.RoleName));
                Assert.AreEqual<string>(expectedVerboseMessage, mockCommandRuntime.VerboseStream[0]);
                Assert.IsTrue(Directory.Exists(Path.Combine(rootPath, roleName, roleName)));
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, roleName, roleName, "manage.py")));
                Assert.IsTrue(Directory.Exists(Path.Combine(rootPath, roleName, roleName, roleName)));
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, roleName, roleName, roleName, "__init__.py")));
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, roleName, roleName, roleName, "settings.py")));
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, roleName, roleName, roleName, "urls.py")));
                Assert.IsTrue(File.Exists(Path.Combine(rootPath, roleName, roleName, roleName, "wsgi.py")));
            }
        }

        [TestMethod]
        public void AddAzurePythonWebRoleWillRecreateDeploymentSettings()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                addPythonWebCmdlet = new AddAzureDjangoWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime };
                addPythonWebCmdlet.CommandRuntime = mockCommandRuntime;
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreatePython, rootPath, roleName);
                string settingsFilePath = Path.Combine(rootPath, Resources.SettingsFileName);
                File.Delete(settingsFilePath);
                Assert.IsFalse(File.Exists(settingsFilePath));

                addPythonWebCmdlet.ExecuteCmdlet();

                AzureAssert.ScaffoldingExists(Path.Combine(rootPath, roleName), Path.Combine(Resources.PythonScaffolding, Resources.WebRole));
                Assert.AreEqual<string>(roleName, ((PSObject)mockCommandRuntime.OutputPipeline[0]).GetVariableValue<string>(Parameters.RoleName));
                Assert.AreEqual<string>(expectedVerboseMessage, mockCommandRuntime.VerboseStream[0]);
                Assert.IsTrue(File.Exists(settingsFilePath));
            }
        }
    }
}