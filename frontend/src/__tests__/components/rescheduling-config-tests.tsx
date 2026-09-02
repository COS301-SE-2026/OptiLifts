import { ReschedulingConfig } from "@/components/ui/rescheduling-config";
import { customFetch } from "@/lib/custom-fetch";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";

vi.mock("@/lib/custom-fetch", ()=> ({
    customFetch: vi.fn(),
}));

describe("ReschedulingConfig", () => {
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        vi.useRealTimers();
        cleanup();
    });
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("fetches config on open and renders dynamic schedule toggle", async () => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({
                dynamicSchedulerEnabled: true,
                maxWorkoutsPerDay: 2,
                minMuscleRestHours: 48,
                restDays: ["Sunday"],
                cycleWindowLengthDays: 7,
                cycleStartDate: "2026-07-01T00:00:00Z",
            }),
        });

        render(<ReschedulingConfig/>);
        expect(screen.getByText("Loading preferences...")).toBeDefined();

        await waitFor(() => {
            expect(screen.getByText("Dynamic Rescheduling")).toBeDefined();
        });

        const toggle = screen.getByRole("checkbox");
        expect((toggle as HTMLInputElement).checked).toBe(true);
    });

    it("save updated preferences via PUT requests on save btn click", async () => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({
                dynamicSchedulerEnabled: true,
                maxWorkoutsPerDay: 1,
                minMuscleRestHours: 24,
                restDays: ["Sunday"],
                cycleWindowLengthDays: 7,
                cycleStartDate: "2026-07-01T00:00:00Z",
            }),
        }).mockResolvedValueOnce({
            ok: true,
        });

        render(<ReschedulingConfig/>);

        await waitFor(() => {
            expect(screen.getByText("Save Preferences")).toBeDefined();
        });

        const savebtn = screen.getByRole("button", {
            name: /save preferences/i
        });
        fireEvent.click(savebtn);

        await waitFor(()=> {
            expect(mockFetch).toHaveBeenCalledWith("api/users/me/schedule/config", expect.objectContaining({
                method: "PUT",
            }));
        });
    });
});