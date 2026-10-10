#ifndef RING0_KERNEL_GUARD_H
#define RING0_KERNEL_GUARD_H

#include <windows.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

// Ring-0 Kernel Anti-Tamper & Security Audit Interface
BOOL AuditKernelRing0Security(void);
BOOL VerifySystemTokenIntegrity(HANDLE hProcessToken);
DWORD FlushWin32DnsResolverCache(void);

#ifdef __cplusplus
}
#endif

#endif // RING0_KERNEL_GUARD_H
