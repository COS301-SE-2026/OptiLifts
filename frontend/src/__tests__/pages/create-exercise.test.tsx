import { CreateExercise } from '@/components/ui/create-exercise';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';

vi.mock('@/context/auth-context', () => ({
  useAuth: vi.fn(),
}));

vi.mock('@/lib/custom-fetch', () => ({
  customFetch: vi.fn(),
}));

describe('CreateExercise Component', () => {
  const mockAuth = useAuth as unknown as Mock;
  const mockFetch = customFetch as unknown as Mock;

  beforeEach(() => {
    vi.clearAllMocks();
    mockAuth.mockReturnValue({
      user: { id: 'user-123', email: 'test@example.com' },
      isAuthenticated: true,
    });
  });

  afterEach(() => {
    cleanup();
  });

  it('renders correctly when open', () => {
    render(
      <CreateExercise
        isOpen={true}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByText('Create Custom Exercise')).toBeDefined();
    expect(screen.getByPlaceholderText('e.g. Seated Cable Row')).toBeDefined();
  });

  it('displays duplicate name error message returned from API', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({ error: "An exercise with the name 'Bench Press' already exists." }),
    });

    render(
      <CreateExercise
        isOpen={true}
        onCancel={vi.fn()}
        initialValues={{
          name: 'Bench Press',
          primaryMuscle: 'Chest',
          secondaryMuscles: [],
          exerciseType: 'WeightReps',
          equipment: 'Barbell',
        }}
      />
    );

    const saveButton = screen.getByRole('button', { name: /save exercise/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      const alert = screen.getByRole('alert');
      expect(alert).not.toBeNull();
      expect(alert.textContent).toContain("An exercise with the name 'Bench Press' already exists.");
    });
  });

  it('clears error message when user modifies the name', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({ error: "An exercise with the name 'Bench Press' already exists." }),
    });

    render(
      <CreateExercise
        isOpen={true}
        onCancel={vi.fn()}
        initialValues={{
          name: 'Bench Press',
          primaryMuscle: 'Chest',
          secondaryMuscles: [],
          exerciseType: 'WeightReps',
          equipment: 'Barbell',
        }}
      />
    );

    const saveButton = screen.getByRole('button', { name: /save exercise/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(screen.getByRole('alert')).not.toBeNull();
    });

    const nameInput = screen.getByPlaceholderText('e.g. Seated Cable Row');
    fireEvent.change(nameInput, { target: { value: 'Bench Press (Incline)' } });

    expect(screen.queryByRole('alert')).toBeNull();
  });
});
