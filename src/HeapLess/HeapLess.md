# Luger.HeapLess

An idea to implement a (n initially simple) runtime for a heapless programming language.

## Description

By heapless I mean a runtime environment which does not utilize a memory heap, and therefore has no need for complex and/or risky memory management such as garbage collection or reference counting.

A heapless process would only utilize a stack (per thread) for runtime memory.
The whole stack, from bottom frame (thread entrypoint) to top frame (current execution), is read only to the currently executing code. This forces data types to have an immutable structure.

E.g. Arrays are always read-only, lists are implemented as linked lists or trees and can thus be "mutated" by pushing new nodes which reference older on the stack.

Some "syscall" like infrastructure is necessary for inter-thread and inter-process communication.

## POC

I plan to implement a POC on .NET (since that's the platform I'm "locked in"/used to :)).

Initially a runtime interface will be definced on which abstractions of instructions and syscalls will be defined. A heapless program is simply a consumer of this interface.

This initial runtime manages an emulated, heap allocated, stack where the program is executed.

Later a custom compiler can be implemented which compiles a heapless program into IL code which respect the constraints of the heapless runtime.

The main thread stack size is set by the 'stack reserve' field in the PE header. The POC runtime can use Thread.Start method with maxStackSize parameter to specify the stack size of the heapless thread(s).

In a native compiled environment we would probably have better control over reserved and committed stack memory.

## Some runtime optimizations

Just of the top of my head...

### By value argument passing

Since all memory (except top of stack) is read only the runtime can choose to pass some primitive arguments by value when calling functions.

### In register argument passing

As with by value argument passing the runtime can choose to pass primitive arguments in CPU registers when calling functions. This is of course only possible if the argument will not be in scope further down the call chain or will need to be pushed onto the stack or further passed in register.

### Virtual memory

ITC/IPC can utilize virtual memory mapping to facilitate near constant time transfer of memory between threads and possibly processes. Since memory is read only no synchronization should be necessary.

