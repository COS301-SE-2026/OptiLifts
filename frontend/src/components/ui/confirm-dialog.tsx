import { Button } from './button'

interface ConfirmDialogProps {
    readonly isOpen: boolean
    readonly onClose: () => void
    readonly onConfirm: () => Promise<void> | void
    readonly title?: string
    readonly description?: string
    readonly confirmText?: string
    readonly cancelText?: string
    readonly variant?: 'default' | 'danger'
    readonly isLoading?: boolean
}

export function ConfirmDialog({
    isOpen,
    onClose,
    onConfirm,
    title = 'Are you sure?',
    description = 'This action cannot be undone.',
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    variant = 'default',
    isLoading = false,
}: ConfirmDialogProps) {
    if (!isOpen) return null

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs transition-opacity duration-200 animate-in fade-in">
            <div
                className="w-full max-w-sm rounded-lg border border-border bg-surface p-6 shadow-2xl mx-4 animate-in fade-in zoom-in-95 duration-200"
                role="alertdialog"
                aria-modal="true"
                aria-labelledby="confirm-dialog-title"
                aria-describedby="confirm-dialog-description"
            >
                <h2 id="confirm-dialog-title" className="font-display text-2xl tracking-wide text-foreground">
                    {title}
                </h2>

                <p id="confirm-dialog-description" className="mt-2 text-sm text-muted-foreground leading-relaxed font-sans normal-case">
                    {description}
                </p>

                <div className="mt-6 flex justify-end gap-3">
                    <Button
                        variant="ghost"
                        onClick={onClose}
                        disabled={isLoading}
                        className="text-xs uppercase tracking-wider"
                    >
                        {cancelText}
                    </Button>
                    <Button
                        variant={variant === 'danger' ? 'default' : 'secondary'}
                        disabled={isLoading}
                        className="text-xs uppercase tracking-wider"
                        onClick={async () => {
                            await onConfirm()
                        }}
                    >
                        {isLoading ? 'Processing...' : confirmText}
                    </Button>
                </div>
            </div>
        </div>
    )
}