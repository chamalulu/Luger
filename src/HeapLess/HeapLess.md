# Luger.HeapLess

An idea to implement a (n initially simple) runtime for a heapless programming language.

## Description

By heapless I mean a runtime environment which does not utilize a memory heap, and therefore has no need for
 complex and/or unsafe memory management such as garbage collection or reference counting.

A heapless process would only utilize a stack (per thread) for runtime memory.

(By abstraction of thread to task, as in .NET TPL, there would be one stack per task which threads are
 multiplexed over.)

The whole stack, from bottom frame (thread entrypoint) to top frame (current execution), is read only to the
 currently executing code. This forces data types to have an immutable structure.

E.g. Lists should be implemented as linked lists or trees and can thus be "mutated" by pushing new nodes
 which reference older on the stack.

One problem with this is passing or returning big chunks of data. If allocated by caller, the buffer is
 read-only and callees cannot write to it. If allocated by callee, the buffer blocks the automatic
 deallocation of intermediate stack frames.  
I have a, not completely thought through, plan for how to fix this problem with runtime managed buffers.

Some "syscall" like infrastructure is necessary for inter-thread and inter-process communication.

## Managed buffers

A runtime call is needed to allocate a managed buffer. Depending on memory layout of the runtime buffer can
 be mapped onto the top of stack or reside in a separate data "segment".

The result of the allocation call is a handle to the buffer. The runtime maps such handles to its own
 internal accounting of manged buffers.

The runtime need to keep track of which stacks have unallocated handles. A kind of reference counting, but
 totally exclusive to the runtime with strict safety enforcements like not allowing buffer handles in a
 buffer.

The runtime need to keep track of handle-specific properties like read-write, copy-on-write and read-only.

The runtime need to keep track of buffer-specific properties like address and length.

This leads to a logical structure of opaque handles mapping to access descriptors referencing a buffer
 descriptor which ultimately reference the buffer.

Since user code has no pointer to the buffer, reads and writes must be done by runtime calls.

## POC

I plan to implement a POC on .NET (since that's the platform I'm "locked in"/used to :)).

Initially a runtime interface will be defined on which abstractions of instructions and syscalls will be
 defined. A heapless program is simply a consumer of this interface.

This initial runtime manages an emulated, heap allocated, stack where the program is executed.

Later a custom compiler can be implemented which compiles a heapless program into IL code which respect the
 constraints of the heapless runtime.

The main thread stack size is set by the 'stack reserve' field in the PE header. The POC runtime can use
 Thread.Start method with maxStackSize parameter to specify the stack size of the heapless thread(s).  
(I need to research this more thoroughly.)

In a native compiled environment we would probably have better control over reserved and committed stack
 memory.

## Some runtime optimizations

Just of the top of my head...

### By value argument passing

Since all memory (except top of stack) is read only the runtime can choose to pass some primitive arguments by value when calling functions.

### In register argument passing

As with by value argument passing the runtime can choose to pass primitive arguments in CPU registers when calling functions. This is of course only possible if the argument will not be in scope further down the call chain or will need to be pushed onto the stack or further passed in register.

### Virtual memory

ITC/IPC can utilize virtual memory mapping to facilitate constant time transfer of memory between threads and possibly processes. Since memory is read only no synchronization should be necessary.

