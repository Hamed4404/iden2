// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { DefaultReconnectDisplay } from './DefaultReconnectDisplay';
import { UserSpecifiedDisplay } from './UserSpecifiedDisplay';
import { LogLevel } from '../Logging/Logger';
import { Blazor } from '../../GlobalExports';
export class DefaultReconnectionHandler {
    constructor(logger, overrideDisplay, reconnectCallback) {
        this._currentReconnectionProcess = null;
        this._logger = logger;
        this._reconnectionDisplay = overrideDisplay;
        this._reconnectCallback = reconnectCallback || Blazor.reconnect;
    }
    onConnectionDown(options, _error) {
        if (!this._reconnectionDisplay) {
            const modal = document.getElementById(options.dialogId);
            this._reconnectionDisplay = modal
                ? new UserSpecifiedDisplay(modal, options.maxRetries, document)
                : new DefaultReconnectDisplay(options.dialogId, options.maxRetries, document, this._logger);
        }
        if (!this._currentReconnectionProcess) {
            this._currentReconnectionProcess = new ReconnectionProcess(options, this._logger, this._reconnectCallback, this._reconnectionDisplay);
        }
    }
    onConnectionUp() {
        if (this._currentReconnectionProcess) {
            this._currentReconnectionProcess.dispose();
            this._currentReconnectionProcess = null;
        }
    }
}
class ReconnectionProcess {
    constructor(options, logger, reconnectCallback, display) {
        this.logger = logger;
        this.reconnectCallback = reconnectCallback;
        this.isDisposed = false;
        this.reconnectDisplay = display;
        this.reconnectDisplay.show();
        this.attemptPeriodicReconnection(options);
    }
    dispose() {
        this.isDisposed = true;
        this.reconnectDisplay.hide();
    }
    async attemptPeriodicReconnection(options) {
        for (let i = 0; i < options.maxRetries; i++) {
            this.reconnectDisplay.update(i + 1);
            const delayDuration = i === 0 && options.retryIntervalMilliseconds > ReconnectionProcess.MaximumFirstRetryInterval
                ? ReconnectionProcess.MaximumFirstRetryInterval
                : options.retryIntervalMilliseconds;
            await this.delay(delayDuration);
            if (this.isDisposed) {
                break;
            }
            try {
                // reconnectCallback will asynchronously return:
                // - true to mean success
                // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
                // - exception to mean we didn't reach the server (this can be sync or async)
                const result = await this.reconnectCallback();
                if (!result) {
                    // If the server responded and refused to reconnect, stop auto-retrying.
                    this.reconnectDisplay.rejected();
                    return;
                }
                return;
            }
            catch (err) {
                // We got an exception so will try again momentarily
                this.logger.log(LogLevel.Error, err);
            }
        }
        this.reconnectDisplay.failed();
    }
    delay(durationMilliseconds) {
        return new Promise(resolve => setTimeout(resolve, durationMilliseconds));
    }
}
ReconnectionProcess.MaximumFirstRetryInterval = 3000;
//# sourceMappingURL=DefaultReconnectionHandler.js.map