import { ScheduleSettingsPopup } from "@/components/ui/schedule-settings-popup";
import { customFetch } from "@/lib/custom-fetch";
import { cleanup, render, waitFor, screen, fireEvent } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";

vi.mock("@/lib/custom-fetch", ()=> ({
    customFetch: vi.fn(),
}));

vi.mock("@/components/ui/rescheduling-config", () => ({
    ReschedulingConfig: () => <div data-testid="rescheduling-config">Rescheduling Config Component</div>,
}));

describe("ScheduleSettingsPopup", () => {
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        vi.useRealTimers();
        cleanup();
    });
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("returns null when isOpen is false", () =>{
        const {container} = render(<ScheduleSettingsPopup isOpen={false} onClose={vi.fn()}/>);
        expect(container.firstChild).toBeNull();
    });

    it("fetches google calendar settings when opened", async () =>{
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({
                isConnected: true,
                syncEnabled: true,
            }),
        });
        render(<ScheduleSettingsPopup isOpen={true} onClose={vi.fn()}/>);

        await waitFor(()=> {
            expect(screen.getByText("Schedule Settings")).toBeDefined();
            expect(screen.getByTestId("rescheduling-config")).toBeDefined();
        });
    });

    it("closed when close btn clicked", async() => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({
                isConnected: false,
                syncEnabled: false,
            }),
        });

        const handleClose = vi.fn();
        render(<ScheduleSettingsPopup isOpen={true} onClose={handleClose}/>);

        await waitFor(()=> {
            expect(screen.getByText("Schedule Settings")).toBeDefined();
        });

        const closebtn = screen.getByRole("button", {
            name: "Close modal"
        });
        fireEvent.click(closebtn);
        expect(handleClose).toHaveBeenCalledTimes(1);
    })
})