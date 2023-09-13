// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView;

class Program
{
    public class BasicBlazorHybridTest
    {
        private string _latestControlDivValue;

        public void Run()
        {
            Console.WriteLine($"Running in test mode!");

            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"Current assembly: {typeof(Program).Assembly.Location}");
            var thisProgramDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            Console.WriteLine($"Old PATH: {Environment.GetEnvironmentVariable("PATH")}");
            Environment.SetEnvironmentVariable("PATH", Path.Combine(thisProgramDir, "runtimes", RuntimeInformation.RuntimeIdentifier, "native") + ";" + Environment.GetEnvironmentVariable("PATH"));
            Console.WriteLine($"New PATH: {Environment.GetEnvironmentVariable("PATH")}");

            var thisAppFiles = Directory.GetFiles(thisProgramDir, "*", SearchOption.AllDirectories).ToArray();
            Console.WriteLine($"Found {thisAppFiles.Length} files in this app:");
            foreach (var file in thisAppFiles)
            {
                Console.WriteLine($"\t{file}");
            }

            var hostPage = "wwwroot/webviewtesthost.html";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<HttpClient>();

            Console.WriteLine($"Creating BlazorWindow...");
            BlazorWindow mainWindow = null;
            try
            {
                mainWindow = new BlazorWindow(
                    title: "Hello, world!",
                    hostPage: hostPage,
                    services: serviceCollection.BuildServiceProvider(),
                    pathBase: "/subdir"); // The content in BasicTestApp assumes this
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.GetType().FullName} while creating window: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine($"Hooking exception handler...");
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                Console.Write(
                    "Fatal exception" + Environment.NewLine +
                    error.ExceptionObject.ToString() + Environment.NewLine);
            };

            Console.WriteLine($"Setting up root components...");
            mainWindow.RootComponents.Add<Pages.TestPage>("root");

            Console.WriteLine($"Running window...");

            const string NewControlDivValueMessage = "wvt:NewControlDivValue";
            var isWebViewReady = false;

            Console.WriteLine($"RegisterWebMessageReceivedHandler...");
            mainWindow.PhotinoWindow.RegisterWebMessageReceivedHandler((s, msg) =>
            {
                if (!msg.StartsWith("__bwv:", StringComparison.Ordinal))
                {
                    if (msg == "wvt:Started")
                    {
                        isWebViewReady = true;
                    }
                    else if (msg.StartsWith(NewControlDivValueMessage, StringComparison.Ordinal))
                    {
                        _latestControlDivValue = msg.Substring(NewControlDivValueMessage.Length + 1);
                    }
                }
            });
            var testPassed = false;

            mainWindow.PhotinoWindow.WindowCreated += (s, e) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // 1. Wait for WebView ready
                        Console.WriteLine($"Waiting for WebView ready...");
                        var isWebViewReadyRetriesLeft = 20;
                        while (!isWebViewReady)
                        {
                            Console.WriteLine($"WebView not ready yet, waiting 1sec...");
                            await Task.Delay(1000);
                            isWebViewReadyRetriesLeft--;
                            if (isWebViewReadyRetriesLeft == 0)
                            {
                                Console.WriteLine($"WebView never became ready, failing the test...");
                                return;
                            }
                        }
                        Console.WriteLine($"WebView is ready!");

                        // 2. Check TestPage starting state
                        if (!await WaitForControlDiv(mainWindow.PhotinoWindow, controlValueToWaitFor: "0"))
                        {
                            return;
                        }

                        // 3. Click a button
                        mainWindow.PhotinoWindow.SendWebMessage($"wvt:ClickButton:incrementButton");

                        // 4. Check TestPage is updated after button click
                        if (!await WaitForControlDiv(mainWindow.PhotinoWindow, controlValueToWaitFor: "1"))
                        {
                            return;
                        }

                        // 5. If we get here, it all worked!
                        Console.WriteLine($"All tests passed!");
                        testPassed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EXCEPTION DURING TEST: " + ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        throw;
                    }
                    finally
                    {
                        // No matter what happens, close the Photino Window
                        mainWindow.PhotinoWindow.Close();
                    }
                });
            };

            try
            {
                mainWindow.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while running window: {ex}");
            }

            Console.WriteLine($"Test passed? {testPassed}");
        }

        const int MaxWaitTimes = 30;
        const int WaitTimeInMS = 250;

        public async Task<bool> WaitForControlDiv(PhotinoWindow photinoWindow, string controlValueToWaitFor)
        {

            for (var i = 0; i < MaxWaitTimes; i++)
            {
                // Tell WebView to report the current controlDiv value (this is inside the loop because
                // it's possible for this to execute before the WebView has finished processing previous
                // C#-generated events, such as WebView button clicks).
                Console.WriteLine($"Asking WebView for current controlDiv value...");
                photinoWindow.SendWebMessage($"wvt:GetControlDivValue");

                // And wait for the value to appear
                if (_latestControlDivValue == controlValueToWaitFor)
                {
                    Console.WriteLine($"WebView reported the expected controlDiv value of {controlValueToWaitFor}!");
                    return true;
                }
                Console.WriteLine($"Waiting for controlDiv to have value '{controlValueToWaitFor}', but it's still '{_latestControlDivValue}', so waiting {WaitTimeInMS}ms.");
                await Task.Delay(WaitTimeInMS);
            }

            Console.WriteLine($"Waited {MaxWaitTimes * WaitTimeInMS}ms but couldn't get controlDiv to have value '{controlValueToWaitFor}' (last value is '{_latestControlDivValue}').");
            return false;
        }
    }

    // Yes, this is a Program.Main() inside of a test project! This project is a regular xUnit.net test project, but
    // some of the WebViewManagerTests also launch this project as a regular executable to launch UI tests. To achieve
    // this, the CSPROJ has the <StartupObject> property set to indicate that _this_ is the Program.Main() to use
    // when launching as an executable.
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            var basicBlazorHybridTest = new BasicBlazorHybridTest();
            basicBlazorHybridTest.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while running {typeof(BasicBlazorHybridTest).FullName}: {ex}");
        }
    }
}
