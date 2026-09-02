import { ReschedulePreviewModal, type RescheduledItem } from "@/components/ui/reschedule-preview-modal";
import { cleanup, render, screen, fireEvent } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

describe("ReschedulePreviewModal", () => {
    afterEach(() => {
        cleanup();
    });

    const mockProposedItems: RescheduledItem[] = [
        {
            entryId: "entry-1",
            workoutId: "w-1",
            workoutName: "Chest & Triceps",
            originalScheduledAt: "2026-07-13T09:00:00Z",
            newScheduledAt: "2026-07-14T09:00:00Z",
            action: "Rescheduled",
        }
    ];

    it("renders nothing when isOpen is false", () => {
        const { container } = render(
            <ReschedulePreviewModal isOpen={false} onClose={vi.fn()}
                proposedItems= { mockProposedItems } onConfirm={vi.fn()} 
                isConfirming={false} />
        );
        expect(container.firstChild).toBeNull();
    });

    it("renders proposed items and date compairion when open", () =>{
        render(
            <ReschedulePreviewModal isOpen={true} onClose={vi.fn()}
                proposedItems= { mockProposedItems } onConfirm={vi.fn()} 
                isConfirming={false} />
        );
        expect(screen.getByText("Proposed Schedule Comparison")).toBeDefined();
        expect(screen.getByText("Chest & Triceps")).toBeDefined();
        expect(screen.getByText("Accept Proposed Schedule")).toBeDefined();
    });

    it("calls onconfirm when confirm button is clicked", () =>{
        const handleConfirm = vi.fn().mockResolvedValue(undefined);
        render(
            <ReschedulePreviewModal isOpen={true} onClose={vi.fn()}
                proposedItems= { mockProposedItems } onConfirm={handleConfirm} 
                isConfirming={false} />
        );
        
        const confirmBtn = screen.getByRole("button", {
            name: /accept proposed schedule/i
        });
        fireEvent.click(confirmBtn);
        expect(handleConfirm).toHaveBeenCalledTimes(1);
    });

    it("closes modal on escape key press", () =>{
        const handleClose = vi.fn();
        render(
            <ReschedulePreviewModal isOpen={true} onClose={handleClose}
                proposedItems= { mockProposedItems } onConfirm={vi.fn()} 
                isConfirming={false} />
        );
        
        fireEvent.keyDown(window, {
            key: "Escape"
        });
        expect(handleClose).toHaveBeenCalledTimes(1);
    });
})
