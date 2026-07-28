import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import App from '../src/App.jsx';

describe('Todo App', () => {
  beforeEach(() => {
    global.fetch = vi.fn();
  });

  it('renders the app title', async () => {
    global.fetch.mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) });
    render(<App />);
    expect(screen.getByText('📋 Todo App')).toBeInTheDocument();
  });

  it('shows error when title is empty', async () => {
    global.fetch.mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) });
    render(<App />);
    fireEvent.click(screen.getByText('Add Todo'));
    expect(screen.getByRole('alert')).toHaveTextContent('Title is required');
  });

  it('displays todos from API', async () => {
    const mockTodos = [
      { id: 1, title: 'Test Todo', description: 'A test', isCompleted: false }
    ];
    global.fetch.mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockTodos) });
    render(<App />);
    await waitFor(() => expect(screen.getByText('Test Todo')).toBeInTheDocument());
  });

  it('calls toggle endpoint when checkbox clicked', async () => {
    const mockTodos = [
      { id: 1, title: 'Toggle Me', description: '', isCompleted: false }
    ];
    global.fetch
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockTodos) })
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) });

    render(<App />);
    await waitFor(() => expect(screen.getByText('Toggle Me')).toBeInTheDocument());
    fireEvent.click(screen.getByLabelText('Toggle Toggle Me'));
    expect(global.fetch).toHaveBeenCalledWith('/api/todo/1/toggle', { method: 'PATCH' });
  });
});
