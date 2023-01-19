import { MonoObject, MonoString, MonoArray } from 'dotnet';
import { WebAssemblyResourceLoader } from './WebAssemblyResourceLoader';
export interface Platform {
    start(resourceLoader: WebAssemblyResourceLoader): Promise<void>;
    callEntryPoint(assemblyName: string): Promise<unknown>;
    toUint8Array(array: System_Array<unknown>): Uint8Array;
    getArrayLength(array: System_Array<unknown>): number;
    getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr;
    getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer;
    readInt16Field(baseAddress: Pointer, fieldOffset?: number): number;
    readInt32Field(baseAddress: Pointer, fieldOffset?: number): number;
    readUint64Field(baseAddress: Pointer, fieldOffset?: number): number;
    readFloatField(baseAddress: Pointer, fieldOffset?: number): number;
    readObjectField<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T;
    readStringField(baseAddress: Pointer, fieldOffset?: number, readBoolValueAsString?: boolean): string | null;
    readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T;
    beginHeapLock(): HeapLock;
    invokeWhenHeapUnlocked(callback: Function): void;
}
export interface HeapLock {
    release(): any;
}
export interface MethodHandle {
    MethodHandle__DO_NOT_IMPLEMENT: unknown;
}
export type System_Object = MonoObject;
export interface System_Boolean {
    System_Boolean__DO_NOT_IMPLEMENT: unknown;
}
export interface System_Byte {
    System_Byte__DO_NOT_IMPLEMENT: unknown;
}
export interface System_Int {
    System_Int__DO_NOT_IMPLEMENT: unknown;
}
export interface System_String extends System_Object, MonoString {
}
export interface System_Array<T> extends System_Object, MonoArray {
}
export interface Pointer {
    Pointer__DO_NOT_IMPLEMENT: unknown;
}
