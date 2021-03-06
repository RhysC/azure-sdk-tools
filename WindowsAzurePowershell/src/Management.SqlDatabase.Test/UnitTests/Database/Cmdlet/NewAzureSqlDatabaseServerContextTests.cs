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

namespace Microsoft.WindowsAzure.Management.SqlDatabase.Test.UnitTests.Database.Cmdlet
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Database.Cmdlet;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Properties;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services.Common;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Services.Server;
    using Microsoft.WindowsAzure.Management.SqlDatabase.Test.UnitTests.MockServer;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Common;

    [TestClass]
    public class NewAzureSqlDatabaseServerContextTests : TestBase
    {
        [TestCleanup]
        public void CleanupTest()
        {
            DatabaseTestHelper.SaveDefaultSessionCollection();
        }

        [TestMethod]
        public void TestGetManageUrl()
        {
            NewAzureSqlDatabaseServerContext contextCmdlet = new NewAzureSqlDatabaseServerContext();

            // Make sure that server name to Manage Url conversion is working
            contextCmdlet.ServerName = "server0001";
            Assert.AreEqual(
                new Uri("https://server0001.database.windows.net"),
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    NewAzureSqlDatabaseServerContext.ServerNameWithSqlAuthParamSet));

            // Make sure that fully qualified server name name to Manage Url conversion is working
            contextCmdlet.FullyQualifiedServerName = "server0003.database.windows.net";
            Assert.AreEqual(
                new Uri("https://server0003.database.windows.net"),
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    NewAzureSqlDatabaseServerContext.FullyQualifiedServerNameWithSqlAuthParamSet));
            
            // Make sure that Manage Url to Manage Url conversion is working properly
            contextCmdlet.ManageUrl = new Uri("https://server0005.database.windows.net");
            Assert.AreEqual(
                new Uri("https://server0005.database.windows.net"),
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    NewAzureSqlDatabaseServerContext.ManageUrlWithSqlAuthParamSet));


            // Make sure that server name to Manage Url conversion is working
            contextCmdlet.ServerName = "server0001";
            Assert.AreEqual(
                new Uri("https://server0001.database.windows.net"),
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    NewAzureSqlDatabaseServerContext.ServerNameWithCertAuthParamSet));

            // Make sure that fully qualified server name name to Manage Url conversion is working
            contextCmdlet.FullyQualifiedServerName = "server0003.database.windows.net";
            Assert.AreEqual(
                new Uri("https://server0003.database.windows.net"),
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    NewAzureSqlDatabaseServerContext.FullyQualifiedServerNameWithCertAuthParamSet));

            try
            {
                UnitTestHelper.InvokePrivate(
                    contextCmdlet,
                    "GetManageUrl",
                    "InvalidParamterSet");
                Assert.Fail("GetManageUrl with invalid parameter set should not succeed.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(Resources.UnknownParameterSet, ex.Message);
            }
        }

        [TestMethod]
        public void NewAzureSqlDatabaseServerContextWithSqlAuth()
        {
            // Create standard context with both ManageUrl and ServerName overridden
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");
            }

            // Create context with just ManageUrl and a derived servername
            HttpSession testSession = DatabaseTestHelper.DefaultSessionCollection.GetSession(
                "UnitTests.NewAzureSqlDatabaseServerContextWithSqlAuthDerivedName");
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                UnitTestHelper.ImportSqlDatabaseModule(powershell);
                UnitTestHelper.CreateTestCredential(powershell);

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    Collection<PSObject> serverContext;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        serverContext = powershell.InvokeBatchScript(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"$context = New-AzureSqlDatabaseServerContext " +
                                @"-ManageUrl {0} " +
                                @"-Credential $credential",
                                MockHttpServer.DefaultServerPrefixUri.AbsoluteUri),
                            @"$context");
                    }

                    Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
                    powershell.Streams.ClearStreams();

                    PSObject contextPsObject = serverContext.Single();
                    Assert.IsTrue(
                        contextPsObject.BaseObject is ServerDataServiceSqlAuth,
                        "Expecting a ServerDataServiceSqlAuth object");
                }
            }
        }

        /// <summary>
        /// Create a new server context using certificate authentication
        /// </summary>
        [TestMethod]
        public void NewAzureSqlDatabaseServerContextWithCertAuth()
        {
            SubscriptionData subscriptionData = UnitTestHelper.CreateUnitTestSubscription();
            subscriptionData.ServiceEndpoint = MockHttpServer.DefaultHttpsServerPrefixUri.AbsoluteUri;

            NewAzureSqlDatabaseServerContext serverContext = new NewAzureSqlDatabaseServerContext();
            ServerDataServiceCertAuth service = serverContext.GetServerDataServiceByCertAuth(
                "testServer", 
                subscriptionData);

            Assert.IsNotNull(service, "The ServerDataServiceCertAuth object returned from "
                + "NewAzureSqlDatabaseServerContext.GetServerDataServiceByCertAuth is null");
        }

        [TestMethod]
        public void NewAzureSqlDatabaseServerContextWithSqlAuthNegativeCases()
        {
            HttpSession testSession = DatabaseTestHelper.DefaultSessionCollection.GetSession(
                "UnitTests.NewAzureSqlDatabaseServerContextWithSqlAuthNegativeCases");

            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                UnitTestHelper.ImportSqlDatabaseModule(powershell);
                UnitTestHelper.CreateTestCredential(powershell);

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    // Test warning when different $metadata is received.
                    Collection<PSObject> serverContext;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        serverContext = powershell.InvokeBatchScript(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"$context = New-AzureSqlDatabaseServerContext " +
                                @"-ServerName testserver " +
                                @"-ManageUrl {0} " +
                                @"-Credential $credential",
                                MockHttpServer.DefaultServerPrefixUri.AbsoluteUri),
                            @"$context");
                    }

                    Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
                    Assert.AreEqual(1, powershell.Streams.Warning.Count, "Should have warning!");
                    Assert.AreEqual(
                        Resources.WarningModelOutOfDate,
                        powershell.Streams.Warning.First().Message);
                    powershell.Streams.ClearStreams();

                    PSObject contextPsObject = serverContext.Single();
                    Assert.IsTrue(
                        contextPsObject.BaseObject is ServerDataServiceSqlAuth,
                        "Expecting a ServerDataServiceSqlAuth object");

                    // Test error case
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        powershell.InvokeBatchScript(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"$context = New-AzureSqlDatabaseServerContext " +
                                @"-ServerName testserver " +
                                @"-ManageUrl {0} " +
                                @"-Credential $credential",
                                MockHttpServer.DefaultServerPrefixUri.AbsoluteUri),
                            @"$context");
                    }

                    Assert.AreEqual(1, powershell.Streams.Error.Count, "Should have errors!");
                    Assert.AreEqual(2, powershell.Streams.Warning.Count, "Should have warning!");
                    Assert.AreEqual(
                        "Test error message",
                        powershell.Streams.Error.First().Exception.Message);
                    Assert.IsTrue(
                        powershell.Streams.Warning.Any(
                            (w) => w.Message.StartsWith("Client Session Id:")),
                        "Client session Id not written to warning");
                    Assert.IsTrue(
                        powershell.Streams.Warning.Any(
                            (w) => w.Message.StartsWith("Client Request Id:")),
                        "Client request Id not written to warning");
                    powershell.Streams.ClearStreams();
                }
            }
        }

        /// <summary>
        /// Common helper method for other tests to create a context.
        /// </summary>
        /// <param name="contextVariable">The variable name that will hold the new context.</param>
        public static void CreateServerContextSqlAuth(
            System.Management.Automation.PowerShell powershell,
            string contextVariable)
        {
            HttpSession testSession = DatabaseTestHelper.DefaultSessionCollection.GetSession(
                "UnitTest.Common.NewAzureSqlDatabaseServerContextWithSqlAuth");
            DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);
            testSession.RequestValidator =
                new Action<HttpMessage, HttpMessage.Request>(
                (expected, actual) =>
                {
                    Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                    Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                    switch (expected.Index)
                    {
                        // Request 0-2: Create context with both ManageUrl and ServerName overriden
                        case 0:
                            // GetAccessToken call
                            DatabaseTestHelper.ValidateGetAccessTokenRequest(
                                expected.RequestInfo,
                                actual);
                            break;
                        case 1:
                            // Get server call
                            DatabaseTestHelper.ValidateHeadersForODataRequest(
                                expected.RequestInfo,
                                actual);
                            break;
                        case 2:
                            // $metadata call
                            Assert.IsTrue(
                                actual.RequestUri.AbsoluteUri.EndsWith("$metadata"),
                                "Incorrect Uri specified for $metadata");
                            DatabaseTestHelper.ValidateHeadersForServiceRequest(
                                expected.RequestInfo,
                                actual);
                            Assert.AreEqual(
                                expected.RequestInfo.Headers[DataServiceConstants.AccessTokenHeader],
                                actual.Headers[DataServiceConstants.AccessTokenHeader],
                                "AccessToken header does not match");
                            Assert.AreEqual(
                                expected.RequestInfo.Cookies[DataServiceConstants.AccessCookie],
                                actual.Cookies[DataServiceConstants.AccessCookie],
                                "AccessCookie does not match");
                            break;
                        default:
                            Assert.Fail("No more requests expected.");
                            break;
                    }
                });

            UnitTestHelper.ImportSqlDatabaseModule(powershell);
            UnitTestHelper.CreateTestCredential(
                powershell,
                testSession.SessionProperties["Username"],
                testSession.SessionProperties["Password"]);

            Collection<PSObject> serverContext;
            using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
            {
                using (new MockHttpServer(
                    exceptionManager,
                    MockHttpServer.DefaultServerPrefixUri,
                    testSession))
                {
                    serverContext = powershell.InvokeBatchScript(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"{1} = New-AzureSqlDatabaseServerContext " +
                            @"-ServerName {2} " +
                            @"-ManageUrl {0} " +
                            @"-Credential $credential",
                            MockHttpServer.DefaultServerPrefixUri.AbsoluteUri,
                            contextVariable,
                            testSession.SessionProperties["Servername"]),
                        contextVariable);
                }
            }

            Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
            Assert.AreEqual(0, powershell.Streams.Warning.Count, "Warnings during run!");
            powershell.Streams.ClearStreams();

            PSObject contextPsObject = serverContext.Single();
            Assert.IsTrue(
                contextPsObject.BaseObject is ServerDataServiceSqlAuth,
                "Expecting a ServerDataServiceSqlAuth object");
        }
    }
}
