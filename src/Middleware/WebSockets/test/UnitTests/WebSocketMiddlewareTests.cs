// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets.Test;

public class WebSocketMiddlewareTests : LoggedTest
{
    [Fact]
    public async Task Connect_Success()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task NegotiateSubProtocol_Success()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            Assert.Equal("alpha, bravo, charlie", context.Request.Headers["Sec-WebSocket-Protocol"]);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync("Bravo");
        }))
        {
            using (var client = new ClientWebSocket())
            {
                client.Options.AddSubProtocol("alpha");
                client.Options.AddSubProtocol("bravo");
                client.Options.AddSubProtocol("charlie");
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);

                // The Windows version of ClientWebSocket uses the casing from the header (Bravo)
                // However, the Managed version seems match the header against the list generated by
                // the AddSubProtocol calls (case-insensitively) and then use the version from
                // that list as the value for SubProtocol. This is fine, but means we need to ignore case here.
                // We could update our AddSubProtocols above to the same case but I think it's better to
                // ensure this behavior is codified by this test.
                Assert.Equal("Bravo", client.SubProtocol, ignoreCase: true);
            }
        }
    }

    [Fact]
    public async Task SendEmptyData_Success()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[0];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(0, result.Count);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var originalData = new byte[0];
                await client.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task SendShortData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello World");
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[originalData.Length];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(originalData.Length, result.Count);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            Assert.Equal(originalData, serverBuffer);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task SendMediumData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes(new string('a', 130));
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[originalData.Length];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(originalData.Length, result.Count);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            Assert.Equal(originalData, serverBuffer);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task SendLongData_Success()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var originalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[originalData.Length];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

            Assert.Equal(originalData, serverBuffer);

            tcs.SetResult();
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);

            }
            // Wait to close the server otherwise the app could throw if it takes longer than the shutdown timeout
            await tcs.Task;
        }
    }

    [Fact]
    public async Task SendFragmentedData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello World");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[originalData.Length];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.False(result.EndOfMessage);
            Assert.Equal(2, result.Count);
            int totalReceived = result.Count;
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            tcs.SetResult();

            result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
            Assert.False(result.EndOfMessage);
            Assert.Equal(2, result.Count);
            totalReceived += result.Count;
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            tcs.SetResult();

            result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(serverBuffer, totalReceived, serverBuffer.Length - totalReceived), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(7, result.Count);
            totalReceived += result.Count;
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

            Assert.Equal(originalData, serverBuffer);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.SendAsync(new ArraySegment<byte>(originalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await tcs.Task;
                tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await client.SendAsync(new ArraySegment<byte>(originalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
                await tcs.Task;
                tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await client.SendAsync(new ArraySegment<byte>(originalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task ReceiveShortData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello World");
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[originalData.Length];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(originalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(originalData, clientBuffer);
            }
        }
    }

    [Fact]
    public async Task ReceiveMediumData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes(new string('a', 130));
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[originalData.Length];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(originalData.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(originalData, clientBuffer);
            }
        }
    }

    [Fact]
    public async Task ReceiveLongData()
    {
        var originalData = Encoding.UTF8.GetBytes(new string('a', 0x1FFFF));
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.SendAsync(new ArraySegment<byte>(originalData), WebSocketMessageType.Binary, true, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[originalData.Length];
                WebSocketReceiveResult result;
                int receivedCount = 0;
                do
                {
                    result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer, receivedCount, clientBuffer.Length - receivedCount), CancellationToken.None);
                    receivedCount += result.Count;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                }
                while (!result.EndOfMessage);

                Assert.Equal(originalData.Length, receivedCount);
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                Assert.Equal(originalData, clientBuffer);
            }
        }
    }

    [Fact]
    public async Task ReceiveFragmentedData_Success()
    {
        var originalData = Encoding.UTF8.GetBytes("Hello World");
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.SendAsync(new ArraySegment<byte>(originalData, 0, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
            await webSocket.SendAsync(new ArraySegment<byte>(originalData, 2, 2), WebSocketMessageType.Binary, false, CancellationToken.None);
            await webSocket.SendAsync(new ArraySegment<byte>(originalData, 4, 7), WebSocketMessageType.Binary, true, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[originalData.Length];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                int totalReceived = result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await client.ReceiveAsync(
                    new ArraySegment<byte>(clientBuffer, totalReceived, clientBuffer.Length - totalReceived), CancellationToken.None);
                Assert.False(result.EndOfMessage);
                Assert.Equal(2, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                result = await client.ReceiveAsync(
                    new ArraySegment<byte>(clientBuffer, totalReceived, clientBuffer.Length - totalReceived), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(7, result.Count);
                totalReceived += result.Count;
                Assert.Equal(WebSocketMessageType.Binary, result.MessageType);

                Assert.Equal(originalData, clientBuffer);
            }
        }
    }

    [Fact]
    public async Task SendClose_Success()
    {
        string closeDescription = "Test Closed";
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(0, result.Count);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(closeDescription, result.CloseStatusDescription);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                Assert.Equal(WebSocketState.CloseSent, client.State);
            }
        }
    }

    [Fact]
    public async Task ReceiveClose_Success()
    {
        string closeDescription = "Test Closed";
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[1024];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, client.State);
            }
        }
    }

    [Fact]
    public async Task CloseFromOpen_Success()
    {
        string closeDescription = "Test Closed";
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(0, result.Count);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(closeDescription, result.CloseStatusDescription);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

                Assert.Equal(WebSocketState.Closed, client.State);
            }
        }
    }

    [Fact]
    public async Task CloseFromCloseSent_Success()
    {
        string closeDescription = "Test Closed";
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var serverBuffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(0, result.Count);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(closeDescription, result.CloseStatusDescription);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
                Assert.Equal(WebSocketState.CloseSent, client.State);

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);
                Assert.Equal(WebSocketState.Closed, client.State);
            }
        }
    }

    [Fact]
    public async Task CloseFromCloseReceived_Success()
    {
        string closeDescription = "Test Closed";
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeDescription, CancellationToken.None);

            var serverBuffer = new byte[1024];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer), CancellationToken.None);
            Assert.True(result.EndOfMessage);
            Assert.Equal(0, result.Count);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
            Assert.Equal(closeDescription, result.CloseStatusDescription);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
                var clientBuffer = new byte[1024];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(clientBuffer), CancellationToken.None);
                Assert.True(result.EndOfMessage);
                Assert.Equal(0, result.Count);
                Assert.Equal(WebSocketMessageType.Close, result.MessageType);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, result.CloseStatus);
                Assert.Equal(closeDescription, result.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, client.State);

                await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                Assert.Equal(WebSocketState.Closed, client.State);
            }
        }
    }

    [Fact]
    public async Task WebSocket_Abort_Interrupts_Pending_ReceiveAsync()
    {
        WebSocket serverSocket = null;

        // Events that we want to sequence execution across client and server.
        var socketWasAccepted = new ManualResetEventSlim();
        var socketWasAborted = new ManualResetEventSlim();
        var firstReceiveOccured = new ManualResetEventSlim();
        var secondReceiveInitiated = new ManualResetEventSlim();

        Exception receiveException = null;

        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            serverSocket = await context.WebSockets.AcceptWebSocketAsync();
            socketWasAccepted.Set();

            var serverBuffer = new byte[1024];

            try
            {
                while (serverSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
                {
                    if (firstReceiveOccured.IsSet)
                    {
                        var pendingResponse = serverSocket.ReceiveAsync(serverBuffer, default);
                        secondReceiveInitiated.Set();
                        var response = await pendingResponse;
                    }
                    else
                    {
                        var response = await serverSocket.ReceiveAsync(serverBuffer, default);
                        firstReceiveOccured.Set();
                    }
                }
            }
            catch (ConnectionAbortedException ex)
            {
                socketWasAborted.Set();
                receiveException = ex;
            }
            catch (Exception ex)
            {
                // Capture this exception so a test failure can give us more information.
                receiveException = ex;
            }
            finally
            {
                Assert.IsType<ConnectionAbortedException>(receiveException);
            }
        }))
        {
            var clientBuffer = new byte[1024];

            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);

                var socketWasAcceptedDidNotTimeout = socketWasAccepted.Wait(10000);
                Assert.True(socketWasAcceptedDidNotTimeout, "Socket was not accepted within the allotted time.");

                await client.SendAsync(clientBuffer, WebSocketMessageType.Binary, false, default);

                var firstReceiveOccuredDidNotTimeout = firstReceiveOccured.Wait(10000);
                Assert.True(firstReceiveOccuredDidNotTimeout, "First receive did not occur within the allotted time.");

                var secondReceiveInitiatedDidNotTimeout = secondReceiveInitiated.Wait(10000);
                Assert.True(secondReceiveInitiatedDidNotTimeout, "Second receive was not initiated within the allotted time.");

                serverSocket.Abort();

                var socketWasAbortedDidNotTimeout = socketWasAborted.Wait(1000); // Give it a second to process the abort.
                Assert.True(socketWasAbortedDidNotTimeout, "Abort did not occur within the allotted time.");
            }
        }
    }

    [Fact]
    public async Task WebSocket_AllowsCancelling_Pending_ReceiveAsync_When_CancellationTokenProvided()
    {
        WebSocket serverSocket = null;
        CancellationTokenSource cts = new CancellationTokenSource();

        var socketWasAccepted = new ManualResetEventSlim();
        var operationWasCancelled = new ManualResetEventSlim();
        var firstReceiveOccured = new ManualResetEventSlim();

        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            serverSocket = await context.WebSockets.AcceptWebSocketAsync();
            socketWasAccepted.Set();

            var serverBuffer = new byte[1024];

            var finishedWithOperationCancelled = false;

            try
            {
                while (serverSocket.State is WebSocketState.Open or WebSocketState.CloseSent)
                {
                    var response = await serverSocket.ReceiveAsync(serverBuffer, cts.Token);
                    firstReceiveOccured.Set();
                }
            }
            catch (OperationCanceledException)
            {
                operationWasCancelled.Set();
                finishedWithOperationCancelled = true;
            }
            finally
            {
                Assert.True(finishedWithOperationCancelled);
            }
        }))
        {
            var clientBuffer = new byte[1024];

            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);

                var socketWasAcceptedDidNotTimeout = socketWasAccepted.Wait(10000);
                Assert.True(socketWasAcceptedDidNotTimeout, "Socket was not accepted within the allotted time.");

                await client.SendAsync(clientBuffer, WebSocketMessageType.Binary, false, default);

                var firstReceiveOccuredDidNotTimeout = firstReceiveOccured.Wait(10000);
                Assert.True(firstReceiveOccuredDidNotTimeout, "First receive did not occur within the allotted time.");

                cts.Cancel();

                var operationWasCancelledDidNotTimeout = operationWasCancelled.Wait(1000); // Give it a second to process the abort.
                Assert.True(operationWasCancelledDidNotTimeout, "Cancel did not occur within the allotted time.");
            }
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, null)]
    [InlineData(HttpStatusCode.Forbidden, "")]
    [InlineData(HttpStatusCode.Forbidden, "http://e.com")]
    [InlineData(HttpStatusCode.OK, "http://e.com", "http://example.com")]
    [InlineData(HttpStatusCode.OK, "*")]
    [InlineData(HttpStatusCode.OK, "http://e.com", "*")]
    [InlineData(HttpStatusCode.OK, "http://ExAmPLE.cOm")]
    public async Task OriginIsValidatedForWebSocketRequests(HttpStatusCode expectedCode, params string[] origins)
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            return Task.CompletedTask;
        },
        o =>
        {
            if (origins != null)
            {
                foreach (var origin in origins)
                {
                    o.AllowedOrigins.Add(origin);
                }
            }
        }))
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                uri.Scheme = "http";

                // Craft a valid WebSocket Upgrade request
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                {
                    request.Headers.Connection.Clear();
                    request.Headers.Connection.Add("Upgrade");
                    request.Headers.Upgrade.Add(new System.Net.Http.Headers.ProductHeaderValue("websocket"));
                    request.Headers.Add(HeaderNames.SecWebSocketVersion, "13");
                    // SecWebSocketKey required to be 16 bytes
                    request.Headers.Add(HeaderNames.SecWebSocketKey, Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, Base64FormattingOptions.None));

                    request.Headers.Add(HeaderNames.Origin, "http://example.com");

                    var response = await client.SendAsync(request);
                    Assert.Equal(expectedCode, response.StatusCode);
                }
            }
        }
    }

    [Fact]
    public async Task OriginIsNotValidatedForNonWebSocketRequests()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, context =>
        {
            Assert.False(context.WebSockets.IsWebSocketRequest);
            return Task.CompletedTask;
        },
        o => o.AllowedOrigins.Add("http://example.com")))
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                uri.Scheme = "http";

                using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                {
                    request.Headers.Add("Origin", "http://notexample.com");

                    var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
    }

    [Fact]
    public async Task CommonHeadersAreSetToInternedStrings()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Use ReferenceEquals and test against the constants
            Assert.Same(HeaderNames.Upgrade, context.Request.Headers.Connection.ToString());
            Assert.Same(Constants.Headers.UpgradeWebSocket, context.Request.Headers.Upgrade.ToString());
            Assert.Same(Constants.Headers.SupportedVersion, context.Request.Headers.SecWebSocketVersion.ToString());
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
            }
        }
    }

    [Fact]
    public async Task MultipleValueHeadersNotOverridden()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Assert.Equal("Upgrade, keep-alive", context.Request.Headers.Connection.ToString());
            Assert.Equal("websocket, example", context.Request.Headers.Upgrade.ToString());
        }))
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(new Uri($"ws://127.0.0.1:{port}/"));
                uri.Scheme = "http";

                // Craft a valid WebSocket Upgrade request
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString()))
                {
                    request.Headers.Connection.Clear();
                    request.Headers.Connection.Add("Upgrade");
                    request.Headers.Connection.Add("keep-alive");
                    request.Headers.Upgrade.Add(new System.Net.Http.Headers.ProductHeaderValue("websocket"));
                    request.Headers.Upgrade.Add(new System.Net.Http.Headers.ProductHeaderValue("example"));
                    request.Headers.Add(HeaderNames.SecWebSocketVersion, "13");
                    // SecWebSocketKey required to be 16 bytes
                    request.Headers.Add(HeaderNames.SecWebSocketKey, Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, Base64FormattingOptions.None));

                    var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                }
            }
        }
    }

    [Fact]
    public async Task AcceptingWebSocketRequestDisablesTimeout()
    {
        await using (var server = KestrelWebSocketHelpers.CreateServer(LoggerFactory, out var port, async context =>
        {
            context.Features.Set<IHttpRequestTimeoutFeature>(new HttpRequestTimeoutFeature());
            Assert.True(context.WebSockets.IsWebSocketRequest);
            var feature = Assert.IsType<HttpRequestTimeoutFeature>(context.Features.Get<IHttpRequestTimeoutFeature>());
            Assert.True(feature.Enabled);

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            Assert.False(feature.Enabled);
        }))
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), CancellationToken.None);
            }
        }
    }

    internal sealed class HttpRequestTimeoutFeature : IHttpRequestTimeoutFeature
    {
        public bool Enabled { get; private set; } = true;

        public CancellationToken RequestTimeoutToken => new CancellationToken();

        public void DisableTimeout()
        {
            Enabled = false;
        }
    }
}
