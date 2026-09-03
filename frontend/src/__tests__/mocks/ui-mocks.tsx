import type { ReactNode } from 'react';

export const mockConfirmDialog = () => ({
    ConfirmDialog: ({ isOpen, onConfirm }: Readonly<{ isOpen: boolean; onConfirm: () => void }>) =>
        isOpen ? (
            <div data-testid="confirm-dialog">
                <button onClick={onConfirm}>Confirm Delete</button>
            </div>
        ) : null,
});

export const mockDropdownMenu = () => ({
    DropdownMenu: ({ children }: Readonly<{ children: ReactNode }>) => <div data-testid="dropdown">{children}</div>,
    DropdownMenuEllipsisTrigger: () => <button data-testid="dropdown-trigger">...</button>,
    DropdownMenuEllipsisContent: ({ children }: Readonly<{ children: ReactNode }>) => (
        <div data-testid="dropdown-content">{children}</div>
    ),
    DropdownMenuItem: ({ children, onSelect }: Readonly<{ children: ReactNode; onSelect: () => void }>) => (
        <button onClick={onSelect} data-testid={`dropdown-item-${children}`}>
            {children}
        </button>
    ),
    DropdownMenuSub: ({ children }: Readonly<{ children: ReactNode }>) => <div data-testid="dropdown-sub">{children}</div>,
    DropdownMenuSubTrigger: ({ children }: Readonly<{ children: ReactNode }>) => (
        <button data-testid="dropdown-sub-trigger">{children}</button>
    ),
    DropdownMenuPortal: ({ children }: Readonly<{ children: ReactNode }>) => <div data-testid="dropdown-portal">{children}</div>,
    DropdownMenuSubContent: ({ children }: Readonly<{ children: ReactNode }>) => (
        <div data-testid="dropdown-sub-content">{children}</div>
    ),
});
