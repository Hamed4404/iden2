// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { PromiseSource } from "./Utils";

export class TestEventSource implements EventSource {
    public CONNECTING: number = 1;
    public OPEN: number = 2;
    public CLOSED: number = 3;
    public onerror!: (evt: Event) => any;
    public onmessage!: (evt: MessageEvent) => any;
    public readyState: number = 0;
    public url: string = "";
    public eventSourceInitDict?: EventSourceInit;
    public withCredentials: boolean = false;

    private _onopen?: (evt: Event) => any;
    public openSet: PromiseSource = new PromiseSource();
    public set onopen(value: (evt: Event) => any) {
        this._onopen = value;
        this.openSet.resolve();
    }

    public get onopen(): (evt: Event) => any {
        return this._onopen!;
    }

    public static eventSourceSet: PromiseSource;
    public static eventSource: TestEventSource;
    public closed: boolean = false;

    constructor(url: string, eventSourceInitDict?: EventSourceInit) {
        this.url = url;
        this.eventSourceInitDict = eventSourceInitDict;

        TestEventSource.eventSource = this;

        if (TestEventSource.eventSourceSet) {
            TestEventSource.eventSourceSet.resolve();
        }
    }

    public close(): void {
        this.closed = true;
    }
    public addEventListener(type: string, listener?: EventListener | EventListenerObject | null, options?: boolean | AddEventListenerOptions): void {
        throw new Error("Method not implemented.");
    }
    public dispatchEvent(evt: Event): boolean {
        throw new Error("Method not implemented.");
    }
    public removeEventListener(type: string, listener?: EventListener | EventListenerObject | null, options?: boolean | EventListenerOptions): void {
        throw new Error("Method not implemented.");
    }

    /* eslint-disable @typescript-eslint/no-empty-function */
    public static callOnOpen(): void {
    }
}

export class TestMessageEvent implements MessageEvent {
    public lastEventId: string = "";
    public composed: boolean = false;
    public composedPath(): EventTarget[];
    public composedPath(): any[] {
        throw new Error("Method not implemented.");
    }
    public data: any;
    public readonly origin!: string;
    // eslint-disable-next-line @typescript-eslint/array-type
    public readonly ports!: ReadonlyArray<MessagePort>;
    public readonly source!: Window;
    public initMessageEvent(type: string, bubbles: boolean, cancelable: boolean, data: any, origin: string, lastEventId: string, source: Window): void {
        throw new Error("Method not implemented.");
    }
    public bubbles!: boolean;
    public cancelBubble!: boolean;
    public cancelable!: boolean;
    public currentTarget!: EventTarget;
    public defaultPrevented!: boolean;
    public eventPhase!: number;
    public isTrusted!: boolean;
    public returnValue!: boolean;
    public scoped!: boolean;
    public srcElement!: Element | null;
    public target!: EventTarget;
    public timeStamp!: number;
    public type!: string;
    public deepPath(): EventTarget[] {
        throw new Error("Method not implemented.");
    }
    public initEvent(type: string, bubbles?: boolean | undefined, cancelable?: boolean | undefined): void {
        throw new Error("Method not implemented.");
    }
    public preventDefault(): void {
        throw new Error("Method not implemented.");
    }
    public stopImmediatePropagation(): void {
        throw new Error("Method not implemented.");
    }
    public stopPropagation(): void {
        throw new Error("Method not implemented.");
    }
    public AT_TARGET!: number;
    public BUBBLING_PHASE!: number;
    public CAPTURING_PHASE!: number;
    public NONE!: number;
}
